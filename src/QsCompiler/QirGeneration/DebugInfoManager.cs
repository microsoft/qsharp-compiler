using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QIR;
using Ubiquity.NET.Llvm.DebugInfo;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Values;
using Ubiquity.NET.Llvm.Types;
using System.IO;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// This class holds shared utility routines for generating QIR debug information.
    /// It contains a reference to the GenerationContext that owns it because it needs
    /// access to the same shared state represented by the GenerationContext.
    /// </summary>
    internal sealed class DebugInfoManager // RyanQuestion: Do I need to inherit from IDisposable?
    {
        /// <summary>
        /// Whether or not to emit debug information during QIR generation
        /// </summary>
        public bool DebugFlag { get; internal set; } = true;

        /// <summary>
        /// Contains the location information for the syntax tree node we are currently parsing and its parents
        /// </summary>
        internal Stack<QsLocation> LocationStack { get; }

        /// <summary>
        /// The context that owns this DebugInfoManager
        /// </summary>
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Access to the GenerationContext's Module
        /// </summary>
        internal BitcodeModule Module
        {
            get
            {
                if (this.sharedState.Module == null)
                {
                    throw new NullReferenceException("Cannot access Module because it is null");
                }

                return this.sharedState.Module;
            }
        }

        /// <summary>
        /// Access to the GenerationContext's Module's DICompileUnit
        /// </summary>
        internal DICompileUnit DICompileUnit
        {
            get
            {
                if (this.Module.DICompileUnit == null)
                {
                    throw new NullReferenceException("Cannot access DICompileUnit because it is null");
                }

                return this.Module.DICompileUnit;
            }
        }

        /// <summary>
        /// Access to the GenerationContext's Module's DIBuilder
        /// </summary>
        internal DebugInfoBuilder DIBuilder
        {
            get
            {
                if (this.Module.DIBuilder == null)
                {
                    throw new NullReferenceException("Cannot access DIBuilder because it is null");
                }

                return this.Module.DIBuilder;
            }
        }

        /// <summary>
        /// Access to the GenerationContext's Context
        /// </summary>
        internal Context Context
        {
            get
            {
                if (this.sharedState.Context == null)
                {
                    throw new NullReferenceException("Cannot access Context because it is null");
                }

                return this.sharedState.Context;
            }
        }

        internal DebugInfoManager(GenerationContext generationContext)
        {
            this.sharedState = generationContext;
            this.LocationStack = new Stack<QsLocation>();
            // perhaps safer to set up to call setupmoduleandcompileunit in here (and just set the compile unit through the context) but seems like bad practice cause I'm setting someone else's member variables
        }

        internal BitcodeModule CreateModuleWithCompileUnit()
        { // RyanNote: a compilation  unit is bound to a BitcodeModule, even though, technically the types are owned by a Context
            if (this.DebugFlag)
            {
                // Find an entry point in order to find the source file path
                bool foundAttribute = false;
                string sourcePath = "";

               ImmutableDictionary<QsQualifiedName, QsCallable> globalCallables = this.sharedState.GetGlobalCallables();
               foreach (QsCallable callable in globalCallables.Values)
                {
                    if (foundAttribute)
                    {
                        break;
                    }

                    foreach (QsDeclarationAttribute atr in callable.Attributes)
                    {
                        string atrName = atr.TypeId.IsValue ? atr.TypeId.Item.Name : "";
                        if (atrName == AttributeNames.EntryPoint)
                        {
                            sourcePath = callable.Source.CodeFile;
                            foundAttribute = true;
                            break;
                        }
                    }
                }

                if (!foundAttribute)
                {
                    sourcePath = ""; // RyanTODO: throw exception here
                }

                string moduleID = Path.GetFileName(sourcePath!);
                string producerVersionIdent = "Qsharp-Compiler"; // this should eventually include the version and be some constant in some file with names like this
                string compilationFlags = "";
                bool optimized = false;
                uint runtimeVersion = 0;

                // change the extension for to .c because of the language/extension issue
                string cSourcePath = Path.ChangeExtension(sourcePath, ".c");
                // RyanTODO: copy qs source file into a c file

                return this.Context.CreateBitcodeModule(
                    moduleID,

                    // ideally this would be a user defined language for Q#
                    SourceLanguage.C99,
                    // SourceLanguage.QSharp,

                    // ideally this would be the original source file path with a .qs extension
                    cSourcePath,
                    // sourcePath,
                    producerVersionIdent,
                    optimized,
                    compilationFlags,
                    runtimeVersion);
            }
            else
            {
                return this.Context.CreateBitcodeModule();
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

    }

}
