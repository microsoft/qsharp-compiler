// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.DebugInfo;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// This class holds shared utility routines for generating QIR debug information.
    /// It contains a reference to the GenerationContext that owns it because it needs
    /// access to the same shared state represented by the GenerationContext.
    /// </summary>
    internal sealed class DebugInfoManager
    {
        /// <summary>
        /// Dwarf version we are using for the debug info in the QIR generation
        /// </summary>
        private static readonly uint DwarfVersion = 4;

        /// <summary>
        /// Title of the CodeView module flag for debug info
        /// </summary>
        private static readonly string CodeviewName = "CodeView";

        /// <summary>
        /// CodeView version we are using for the debug info in the QIR generation
        /// </summary>
        private static readonly uint CodeviewVersion = 1;

        /// <summary>
        /// The source language information for Dwarf.
        /// For now, we are using the C interface. Ideally this would be a user defined language for Q#.
        /// </summary>
        private static readonly SourceLanguage QSharpLanguage = SourceLanguage.C99;

        /// <summary>
        /// Returns a string representing the producer information for the QIR
        /// </summary>
        private static string GetQirProducerIdent()
        {
            AssemblyName compilationInfo = CompilationLoader.GetQSharpCompilerAssemblyName();
            AssemblyName qirGenerationInfo = Assembly.GetExecutingAssembly().GetName();
            return compilationInfo.Name + " with " + qirGenerationInfo.Name + " V " + qirGenerationInfo.Version;
        }

        /// <summary>
        /// Whether or not to emit debug information during QIR generation
        /// </summary>
        public bool DebugFlag { get; } = true;

        /// <summary>
        /// Contains the location information for the syntax tree node we are currently parsing and its parents
        /// Currently this is only used for statement nodes, but will be used for other types of nodes in the future
        /// </summary>
        internal Stack<QsNullable<QsLocation>> LocationStack { get; }

        /// <summary>
        /// The GenerationContext that owns this DebugInfoManager
        /// </summary>
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Access to the GenerationContext's Module's DIBuilder
        /// </summary>
        internal DebugInfoBuilder DIBuilder => this.sharedState.Module.DIBuilder;

        /// <summary>
        /// Access to the GenerationContext's Context
        /// </summary>
        private Context Context => this.sharedState.Context;

        internal DebugInfoManager(GenerationContext generationContext)
        {
            this.sharedState = generationContext;
            this.LocationStack = new Stack<QsNullable<QsLocation>>();
        }

        /// <summary>
        /// Gets the Dwarf version we are using for the debug info in the QIR generation
        /// </summary>
        private uint GetDwarfVersion() => DwarfVersion;

        /// <summary>
        /// Gets the CodeView version we are using for the debug info in the QIR generation
        /// </summary>
        private string GetCodeViewName() => CodeviewName;

        /// <summary>
        /// Gets the title for the CodeView module flag for debug info
        /// </summary>
        private uint GetCodeViewVersion() => CodeviewVersion;

        /// <summary>
        /// Makes the necessary call to finalize the DIBuiler.
        /// </summary>
        internal void FinalizeDebugInfo()
        {
            this.DIBuilder.Finish(); // must be called after all QIR generation
        }

        /// <summary>
        /// If DebugFlag is set to false, simply creates a module for the owning GenerationContext, attaches it to its Context, and returns it.
        /// If DebugFlag is set to true, creates a module with a compile unit and top level debug info, attaches it, and returns it.
        /// Note: because this is called from within the constructor of the GenerationContext,
        /// we cannot access this.Module or anything else that uses this.sharedState
        /// </summary>
        internal BitcodeModule CreateModuleWithCompileUnit(ImmutableArray<QsQualifiedName> entryPoints)
        {
            if (this.DebugFlag)
            {
                // Get the source file path from an entry point. For now this only handles modules with one source file.
                string sourcePath = "";
                if (!entryPoints.IsEmpty)
                {
                    if (this.sharedState.TryGetGlobalCallable(entryPoints[0], out QsCallable? entry))
                    {
                        sourcePath = entry.Source.CodeFile;
                    }
                    else
                    {
                        throw new Exception("Entry point not found");
                    }
                }
                else
                {
                    throw new Exception("No entry point found in the source code");
                }

                string moduleID = Path.GetFileName(sourcePath);

                // Change the extension for to .c because of the language/extension issue
                string cSourcePath = Path.ChangeExtension(sourcePath, ".c");

                // TODO: If we need compilation flags (an optional argument to CreateBitcodeModule) in the future we will need
                // to figure out how to get the compile options in the CompilationLoader.Configuration
                // and turn them into a string (although the compilation flags don't seem to be emitted to the IR anyways)

                BitcodeModule newModule = this.Context.CreateBitcodeModule(
                    moduleID,
                    QSharpLanguage, // For now, we are using the C interface. Ideally this would be a user defined language for Q#
                    cSourcePath, // Note that to debug the source file, you'll have to copy the content of the .qs file into a .c file with the same name
                    GetQirProducerIdent());

                // Add Module identity and Module Flags
                newModule.AddProducerIdentMetadata(GetQirProducerIdent());

                // TODO: ModuleFlagBehavior.Warning is emitting a 1 (indicating error) instead of 2
                newModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DwarfVersionValue, this.GetDwarfVersion());
                newModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DebugVersionValue, BitcodeModule.DebugMetadataVersion);
                newModule.AddModuleFlag(ModuleFlagBehavior.Warning, this.GetCodeViewName(), this.GetCodeViewVersion()); // TODO: We seem to need this flag and not Dwarf in order to debug on Windows. Need to look into why LLDB is using CodeView on Windows
                AddTargetSpecificModuleFlags();

                return newModule;
            }
            else
            {
                return this.Context.CreateBitcodeModule();
            }

            void AddTargetSpecificModuleFlags()
            {
                // TODO: could be useful to have target-specific module flags at some point
                // Examples: AddModuleFlag(ModuleFlagBehavior.Error, "PIC Level", 2); (ModuleFlagBehavior.Error, "wchar_size", 4); (ModuleFlagBehavior.Error, "min_enum_size", 4)
                // Have access to newModule here
                return;
            }
        }

                // private void EmitLocation(IAstNode? node) //TODO write this correctly
        // {
        //     // Get current scope
        //     DILocalScope? scope = null;
        //     if(LexicalBlocks.Count > 0)
        //     {
        //         scope = LexicalBlocks.Peek();
        //     } // RyanNote: This DISubProgram looks important for debug information
        //     else if(InstructionBuilder.InsertFunction != null && InstructionBuilder.InsertFunction.DISubProgram != null)
        //     {
        //         scope = InstructionBuilder.InsertFunction.DISubProgram;
        //     }

        //     DILocation? loc = null;
        //     if(scope != null)
        //     {
        //         loc = new DILocation(this.Context)
        //                             , ( uint )( node?.Location.StartLine ?? 0 )
        //                             , ( uint )( node?.Location.StartColumn ?? 0 )
        //                             , scope
        //                             );
        //     }

        //     InstructionBuilder.SetDebugLocation( loc );
        // }

        internal IrFunction CreateLocalFunction(QsSpecialization spec, string name, IFunctionType signature, bool isDefinition, ITypeRef retType, ITypeRef[] argTypes)
        {
            if (this.DebugFlag && spec.Kind == QsSpecializationKind.QsBody)
            {
                DIFile debugFile = this.DIBuilder.CreateFile(this.DICompileUnit.File?.FileName, this.DICompileUnit.File?.Directory);
                QsNullable<QsLocation> debugLoc = spec.Location; // RyanNote: here's where we get the location.
                uint line;

                if (debugLoc.IsNull)
                {
                    throw new ArgumentException("Expected a specialiazation with a non-null location");
                    return this.Module.CreateFunction(name, signature);
                }

                // create the debugSignature
                var voidType = DebugType.Create<ITypeRef, DIType>(this.Module.Context.VoidType, null); // RyanNote: pass retType in for first arg, not sure for second
                IDebugType<ITypeRef, DIType>[] voidTypeArr = { voidType, voidType };
                DebugInfoFlags debugFlags = DebugInfoFlags.None; // RyanTODO: Might want flags here. Also might want to define our own. Also can we have multiple?
                DebugFunctionType debugSignature = new DebugFunctionType(signature, this.Module, debugFlags, voidType, voidTypeArr); // RyanTODO: the voidType stuff is for sure wrong, but just want to compile rn

    // public DebugFunctionType( // here's what I need to construct a DebugFunctionType
    //         IFunctionType llvmType,
    //         BitcodeModule module,
    //         DebugInfoFlags debugFlags,
    //         IDebugType<ITypeRef, DIType> retType,
    //         params IDebugType<ITypeRef, DIType>[] argTypes)

    // this.Context.GetFunctionType(returnTypeRef, argTypeRefs); // from context.cs in QIR
    //     var signature = DebugFunctionType( Module.DIBuilder, DoubleType, prototype.Parameters.Select( _ => DoubleType ) ); // from code generation in Kaleidoscope (this function doesn't exist here)

                return this.Module.CreateFunction(
                    scope: this.DICompileUnit,
                    name: name,
                    mangledName: null,
                    file: debugFile,
                    line: (uint) debugLoc.Item.Offset.Line,
                    signature: debugSignature,
                    isLocalToUnit: true,
                    isDefinition: isDefinition,
                    scopeLine: (uint) debugLoc.Item.Offset.Line, // RyanTODO: Need to be more exact bc of formatting (see lastParamLocation in Kaleidescope tutorial)
                    debugFlags: debugFlags,
                    isOptimized: false); // RyanQuestion: is this always the case?
            }
            else
            {
                return this.Module.CreateFunction(name, signature);
            }
        }

        #endregion

    }
}
