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
    internal sealed class DebugInfoManager
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
        /// The GenerationContext that owns this DebugInfoManager
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
        }

        internal BitcodeModule CreateModuleWithCompileUnit()
        {
            if (this.DebugFlag)
            {
                // Find an entry point in order to find the source file path
                bool foundEntryAttribute = false;
                string sourcePath = "";

               ImmutableDictionary<QsQualifiedName, QsCallable> globalCallables = this.sharedState.GetGlobalCallables();
               foreach (QsCallable callable in globalCallables.Values)
                {
                    if (foundEntryAttribute)
                    {
                        break;
                    }

                    foreach (QsDeclarationAttribute atr in callable.Attributes)
                    {
                        string atrName = atr.TypeId.IsValue ? atr.TypeId.Item.Name : "";
                        if (atrName == AttributeNames.EntryPoint)
                        {
                            sourcePath = callable.Source.CodeFile;
                            foundEntryAttribute = true;
                            break;
                        }
                    }
                }

                if (!foundEntryAttribute)
                {
                    throw new Exception("No entry point found in the source code");
                }

                string moduleID = Path.GetFileName(sourcePath);
                string producerVersionIdent = "Qsharp-Compiler"; // RyanTODO: this should eventually include the version and be some constant in some file with names like this
                string compilationFlags = ""; // RyanTODO
                bool optimized = false; // RyanTODO
                uint runtimeVersion = 0; // RyanTODO

                // Change the extension for to .c because of the language/extension issue
                string cSourcePath = Path.ChangeExtension(sourcePath, ".c");

                return this.Context.CreateBitcodeModule(
                    moduleID,
                    SourceLanguage.C99, // For now, we are using the C interface. Ideally this would be a user defined language for Q#
                    cSourcePath, // Note that to debug the source file, you'll have to copy the content of the .qs file into a .c file with the same name
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
    }
}
