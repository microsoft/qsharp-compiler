// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.DebugInfo;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

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
        /// Contains the location information for the statement nodes we are currently parsing
        /// </summary>
        internal Stack<QsNullable<QsLocation>> StatementLocationStack { get; }

        /// <summary>
        /// Contains the location information for the namespace element we are inside
        /// </summary>
        internal QsLocation? CurrentNamespaceElementLocation { get; set; }

        /// <summary>
        /// Contains the location information for the Expression we are inside
        /// </summary>
        internal Stack<DataTypes.Range?> ExpressionRangeStack { get; }

        /// <summary>
        /// The GenerationContext that owns this DebugInfoManager
        /// </summary>
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Contains the DIBuilders included in the module related to this DebugInfoManager.
        /// The key is the source file path of the DebugInfoBuilder's compile unit
        /// </summary>
        private readonly Dictionary<string, DebugInfoBuilder> dIBuilders;

        /// <summary>
        /// Access to the GenerationContext's Context
        /// </summary>
        private Context Context => this.sharedState.Context;

        /// <summary>
        /// Access to the GenerationContext's Module
        /// </summary>
        private BitcodeModule Module => this.sharedState.Module;

        /// <summary>
        /// Access to the GenerationContext's Context's InstructionBuilder
        /// </summary>
        private InstructionBuilder CurrentInstrBuilder => this.sharedState.CurrentBuilder;

        internal DebugInfoManager(GenerationContext generationContext)
        {
            this.sharedState = generationContext;
            this.StatementLocationStack = new Stack<QsNullable<QsLocation>>();
            this.ExpressionRangeStack = new Stack<DataTypes.Range?>();
            this.dIBuilders = new Dictionary<string, DebugInfoBuilder>();
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
        /// Sums up the offsets from the stack of statement locations <see cref="StatementLocationStack"/>.
        /// </summary>
        internal Position TotalOffsetFromStatements()
        {
            Position offset = Position.Zero;

            foreach (QsNullable<QsLocation> loc in this.StatementLocationStack)
            {
                if (loc.IsValue)
                {
                    offset += loc.Item.Offset;
                }

            }
            return offset;
        }

        /// <summary>
        /// Sums up the offsets from the stack of expression ranges <see cref="ExpressionRangeStack"/>.
        /// </summary>
        internal Position TotalOffsetFromExpressions() // TODO: should it be added up like this?
        {
            Position offset = Position.Zero;

            foreach (DataTypes.Range? range in this.ExpressionRangeStack)
            {
                if (range != null)
                {
                    offset += range.Start;
                }

            }
            return offset;
        }



        /// <summary>
        /// Creates a moduleID which lists all source files with debug info
        /// And makes the necessary calls to finalize the DIBuilders
        /// </summary>
        internal void FinalizeDebugInfo()
        {
            if (this.DebugFlag)
            {
                string moduleID = "";
                foreach (KeyValuePair<string, DebugInfoBuilder> entry in this.dIBuilders)
                {
                    entry.Value.Finish(); // must be called for every DIBuilder after all QIR generation

                    string sourcePath = entry.Key;
                    if (!string.IsNullOrEmpty(moduleID))
                    {
                        moduleID += ", ";
                    }

                    moduleID += Path.GetFileName(sourcePath);
                }

                // TODO: set module ID. Decide how to actually create the moduleID (above way could be very long)
            }
        }

        /// <summary>
        /// Gets the DebugInfoBuilder with a CompileUnit associated with this source file if it has already been created,
        /// Creates a DebugInfoBuilder with a CompileUnit associated with this source file otherwise.
        /// </summary>
        /// <param name="sourcePath">The source file's path for this compile unit</param>
        /// <returns>The compile unit related to this source file</returns>
        private DebugInfoBuilder GetOrCreateDIBuilder(string sourcePath)
        {
            DebugInfoBuilder di;
            if (this.dIBuilders.TryGetValue(sourcePath, out di))
            {
                return di;
            }
            else
            {
                // Change the extension for to .c because of the language/extension issue
                string cSourcePath = Path.ChangeExtension(sourcePath, ".c");

                // TODO: If we need compilation flags (an optional argument to CreateBitcodeModule) in the future we will need
                // to figure out how to get the compile options in the CompilationLoader.Configuration
                // and turn them into a string (although the compilation flags don't seem to be emitted to the IR anyways)

                di = this.Module.CreateDIBuilder();
                di.CreateCompileUnit(
                    QSharpLanguage, // Note that to debug the source file, you'll have to copy the content of the .qs file into a .c file with the same name
                    cSourcePath,
                    GetQirProducerIdent(),
                    null);
                this.dIBuilders.Add(sourcePath, di);
                return di;
            }
        }

        /// <summary>
        /// If DebugFlag is set to true, creates Adds top level debug info to the module,
        /// Note: because this is called from within the constructor of the GenerationContext,
        /// we cannot access this.Module or anything else that uses this.sharedState
        /// </summary>
        internal void AddTopLevelDebugInfo(BitcodeModule owningModule, ImmutableArray<QsQualifiedName> entryPoints)
        {
            if (this.DebugFlag)
            {
                // Add Module identity and Module Flags
                owningModule.AddProducerIdentMetadata(GetQirProducerIdent());

                // TODO: ModuleFlagBehavior.Warning is emitting a 1 (indicating error) instead of 2
                owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DwarfVersionValue, this.GetDwarfVersion());
                owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DebugVersionValue, BitcodeModule.DebugMetadataVersion);
                owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, this.GetCodeViewName(), this.GetCodeViewVersion()); // TODO: We seem to need this flag and not Dwarf in order to debug on Windows. Need to look into why LLDB is using CodeView on Windows
                AddTargetSpecificModuleFlags();

                void AddTargetSpecificModuleFlags()
            {
                // TODO: could be useful to have target-specific module flags at some point
                // Examples: AddModuleFlag(ModuleFlagBehavior.Error, "PIC Level", 2); (ModuleFlagBehavior.Error, "wchar_size", 4); (ModuleFlagBehavior.Error, "min_enum_size", 4)
                return;
            }

                // // For now this is here to demonstrate a compilation unit being added, but eventually this will be called whenever a new file is encountered and not here.
                // // Get the source file path from an entry point.
                // string sourcePath = "";
                // if (!entryPoints.IsEmpty)
                // {
                //     if (this.sharedState.TryGetGlobalCallable(entryPoints[0], out QsCallable? entry))
                //     {
                //         sourcePath = entry.Source.CodeFile;
                //     }
                //     else
                //     {
                //         throw new Exception("Entry point not found");
                //     }
                // }
                // else
                // {
                //     throw new Exception("No entry point found in the source code");
                // }

                // string cSourcePath = Path.ChangeExtension(sourcePath, ".c");
                // DebugInfoBuilder di = owningModule.CreateDIBuilder();
                // di.CreateCompileUnit(
                //     QSharpLanguage, // Note that to debug the source file, you'll have to copy the content of the .qs file into a .c file with the same name
                //     cSourcePath,
                //     GetQirProducerIdent(),
                //     null);
            }
        }

        private IDebugType<ITypeRef, DIType> GetDebugTypeFor(ResolvedType resolvedType, DebugInfoBuilder dIBuilder) // TODO: use a type transformation to do this
        {
            if (resolvedType.Resolution.Equals(QsTypeKind.Int))
            {
                return new DebugBasicType(this.sharedState.Types.Int, dIBuilder, TypeNames.Int, DiTypeKind.Signed);
            }
            else if (resolvedType.Resolution.Equals(QsTypeKind.String))
            {
                // dIBuilder.CreatePointerType
                // this is causing an exception bc llvmtype pased in isn't one of the expected for a basic type
                return new DebugBasicType(this.sharedState.Types.String, dIBuilder, TypeNames.String, DiTypeKind.Signed);
            }
            else if (resolvedType.Resolution.Equals(QsTypeKind.UnitType))
            {
                return DebugType.Create<ITypeRef, DIType>(this.Context.VoidType, null);
            }
            else
            {
                throw new Exception("Only handling a couple things for testing rn");
            }
        }

    // enum QsTypeKind: useful for basic types
        // UnitType,
        // Int,
        // BigInt,
        // Double,
        // Bool,
        // String,
        // Qubit,
        // Result,
        // Pauli,
        // Range,
        // ArrayType,
        // TupleType,
        // UserDefinedType,
        // TypeParameter,
        // Operation,
        // Function,
        // MissingType,
        // InvalidType,

// return DIBasicType::get(VMContext, dwarf::DW_TAG_unspecified_type, Name); look at dwarfenumeration tags to get more complicted like this for user defined types

        internal void CreateLocalVariable(string name, IValue value) // TODO: should I be doing something with this local variable so it gets updated properly???
        {
            if (this.DebugFlag)
            {
                DISubProgram? subProgram = this.sharedState.CurrentFunction?.DISubProgram;
                string? sourcePath = subProgram?.File?.Path;
                if (string.IsNullOrEmpty(sourcePath))
                {
                    return;
                }

                DebugInfoBuilder diBuilder = this.GetOrCreateDIBuilder(sourcePath);
                ResolvedType? resType = value.QSharpType;

                DIType? dIType;
                try
                {
                    dIType = resType == null ? null : this.GetDebugTypeFor(resType, diBuilder).DIType;
                }
                catch (Exception)
                {
                    dIType = null;
                }

                Position absolutePosition = this.TotalOffsetFromStatements() + this.TotalOffsetFromExpressions();
                if (subProgram != null)
                {
                    DILocalVariable dIVar = diBuilder.CreateLocalVariable(
                    subProgram,
                    name,
                    diBuilder.CompileUnit?.File,
                    (uint)absolutePosition.Line,
                    dIType,
                    alwaysPreserve: true,
                    DebugInfoFlags.None);

                    DILocation dILoc = new DILocation(
                        this.Context,
                        (uint)absolutePosition.Line,
                        (uint)absolutePosition.Column,
                        subProgram);

                    var nativeType = value.Value.NativeType;

                    if (this.sharedState.CurrentBlock != null && dIType != null)
                    {
                        diBuilder.InsertDeclare(
                            storage: value.Value,
                            varInfo: dIVar,
                            location: dILoc,
                            insertAtEnd: this.sharedState.CurrentBlock);
                    }
                }

                // Only need to emit debug information if the variable is from a source file RyanTODO: move this to the manager registerVariable so we have access to stack
                // if (fromLocalID != null)
                // {
                //     DILocalScope scope = null; // RyanTODO: where do I get this
                //     this.SharedState.DIManager.emitLocation(scope);
                // }
            }
        }
        internal IrFunction CreateLocalFunction(QsSpecialization spec, string name, IFunctionType signature, bool isDefinition, ITypeRef retType, ITypeRef[] argTypes)
        {
            if (this.DebugFlag && spec.Kind == QsSpecializationKind.QsBody)
            {
                // get the debug location of the function
                QsNullable<QsLocation> debugLoc = spec.Location; // RyanNote: here's where we get the location.
                if (debugLoc.IsNull)
                {
                    throw new ArgumentException("Expected a specialiazation with a non-null location");
                }
                uint line = (uint)debugLoc.Item.Offset.Line;
                uint col = (uint)debugLoc.Item.Offset.Column;

                // set up the DIBuilder
                string sourcePath = spec.Source.CodeFile;
                DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
                DIFile debugFile = curDIBuilder.CreateFile(Path.ChangeExtension(sourcePath, ".c")); // TODO: this will get changed after language/extension issue figured out

                // create the debugSignature
                // IDebugType<ITypeRef, DIType> voidType = DebugType.Create<ITypeRef, DIType>(this.Module.Context.VoidType, null); // RyanNote: pass retType in for first arg, DIType for second which is null just cause void doesn't have debug info
                // IDebugType<ITypeRef, DIType> retDebugType = voidType;
                IDebugType<ITypeRef, DIType> retDebugType;
                IDebugType<ITypeRef, DIType>[] argDebugTypes;
                try
                {
                    retDebugType = this.GetDebugTypeFor(spec.Signature.ReturnType, curDIBuilder);

                    argDebugTypes = spec.Signature.ArgumentType.Resolution is ResolvedTypeKind.TupleType ts ? ts.Item.Select(t => this.GetDebugTypeFor(t, curDIBuilder)).ToArray() :
                        new IDebugType<ITypeRef, DIType>[] { this.GetDebugTypeFor(spec.Signature.ArgumentType, curDIBuilder) };
                }
                catch (Exception)
                {
                    Console.WriteLine("exception in converting to debugtype");
                    return this.Module.CreateFunction(name, signature);
                }

                DebugInfoFlags debugFlags = DebugInfoFlags.None; // RyanTODO: Might want flags here. Also might want to define our own. Also can we have multiple?
                // DebugFunctionType debugSignature = new DebugFunctionType(signature, curDIBuilder, debugFlags, voidType, voidTypeArr); // RyanTODO: the voidType stuff is for sure wrong, but just want to compile rn
                // DebugFunctionType debugSignature = this.Context.CreateFunctionType(curDIBuilder, retDebugType, argDebugTypes);
                // CreateFunctionType( DebugInfoBuilder diBuilder
                //                                    , IDebugType<ITypeRef, DIType> retType
                //                                    , params IDebugType<ITypeRef, DIType>[ ] argTypes
                //                                    )
               //  signature: context.CreateFunctionType(module.DIBuilder, i32)
                DebugFunctionType debugSignature = new DebugFunctionType(signature, curDIBuilder, debugFlags, retDebugType, argDebugTypes);


// DebugType.Create( fooPtr, constFoo ) from LLVM.NET
// var fooBody = new[ ] //from LLVM.NET
//     {
//         new DebugMemberInfo( 0, "a", diFile, 3, i32 ),
//         new DebugMemberInfo( 1, "b", diFile, 4, f32 ),
//         new DebugMemberInfo( 2, "c", diFile, 5, i32Array_0_32 ),
//     };

// var fooType = new DebugStructType( module, "struct.foo", module.DICompileUnit, "foo", diFile, 1, DebugInfoFlags.None, fooBody );


// example creation of debug types from LLVM.NET
    // DebugType.Create( llvmType.ValidateNotNull( nameof( llvmType ) ).ElementType, elementType )
    // var i32 = new DebugBasicType( module.Context.Int32Type, module, "int", DiTypeKind.Signed );
    //         var f32 = new DebugBasicType( module.Context.FloatType, module, "float", DiTypeKind.Float );
    // var doubleType = new DebugBasicType( llvmContext.DoubleType, module, "double", DiTypeKind.Float );

    // CGDebugInfo.cpp in clang has examples in RVV_TYPE
    // uint64_t Size = CGM.getContext().getTypeSize(BT);
//   return DBuilder.createBasicType(BTName, Size, Encoding);

    // public DebugFunctionType( // here's what I need to construct a DebugFunctionType
    //         IFunctionType llvmType,
    //         BitcodeModule module,
    //         DebugInfoFlags debugFlags,
    //         IDebugType<ITypeRef, DIType> retType,
    //         params IDebugType<ITypeRef, DIType>[] argTypes)

    // this.Context.GetFunctionType(returnTypeRef, argTypeRefs); // from context.cs in QIR


                IrFunction func = this.Module.CreateFunction(
                    scope: curDIBuilder.GetCompileUnit(),
                    name: name,
                    mangledName: null, // RyanTODO: figure out what mangling we might need
                    file: debugFile,
                    line: line,
                    signature: debugSignature,
                    isLocalToUnit: true, // we're using the compile unit from the source file this was declared in
                    // isDefinition: isDefinition,
                    isDefinition: true,
                    scopeLine: line, // RyanTODO: Need to be more exact bc of formatting (see lastParamLocation in Kaleidescope tutorial)
                    debugFlags: debugFlags,
                    isOptimized: false, // RyanQuestion: is this always the case?
                    curDIBuilder);

                this.EmitLocation(line, col, func.DISubProgram, curDIBuilder); // do I need this when we create the function?
                return func;
            }
            else
            {
                return this.Module.CreateFunction(name, signature);
            }
        }

        internal void EmitLocation(uint line, uint col, DILocalScope? localScope, DebugInfoBuilder? di = null)
        {
            if (this.DebugFlag)
            {
                // The way we store line/col is 0-indexed but InstructionBuilder is expecting 1-indexed.
                line += 1;
                col += 1;

                if (localScope == null)
                {
                    // scope = di.GetCompileUnit(); // Would be nice functionality, but bindings not set up for DIScope rather than DILocalScope rn
                    throw new ArgumentException("Cannot set a debug location with a null scope."); // TODO: remove, doesn't need to throw an exception this is just for testing
                }

                this.CurrentInstrBuilder.SetDebugLocation(line, col, localScope);
            }
        }

        internal void EmitLocation(Position? relativePosition)
        {
            if (this.DebugFlag)
            {
                DISubProgram? sp = this.sharedState.CurrentFunction?.DISubProgram;
                QsLocation? namespaceLoc = this.sharedState.DIManager.CurrentNamespaceElementLocation;
                if (namespaceLoc != null && relativePosition != null && sp != null)
                {
                    Position absolutePosition = namespaceLoc.Offset + this.TotalOffsetFromStatements() + relativePosition;
                    this.EmitLocation((uint)absolutePosition.Line, (uint)absolutePosition.Column, sp);
                }
            }
        }
    }
}
