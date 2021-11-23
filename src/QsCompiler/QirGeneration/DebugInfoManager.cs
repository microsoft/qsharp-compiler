// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
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
// TODO: remove SECTIONS before merging to main
// SECTION: Config values
        private static class Config
        {
            /// <summary>
            /// Whether or not to emit debug information during QIR generation
            /// </summary>
            public static bool DebugSymbolsEnabled { get; set; } = false;

            /// <summary>
            /// Dwarf version we are using for the debug info in the QIR generation
            /// </summary>
            public static uint DwarfVersion { get; } = 4;

            /// <summary>
            /// Title of the CodeView module flag for debug info
            /// </summary>
            public static string CodeviewName { get; } = "CodeView";

            /// <summary>
            /// CodeView version we are using for the debug info in the QIR generation
            /// </summary>
            public static uint CodeviewVersion { get; } = 1;

            /// <summary>
            /// The source language information for Dwarf.
            /// For now, we are using the C interface. Ideally this would be a user defined language for Q#.
            /// </summary>
            public static SourceLanguage QSharpLanguage { get; } = SourceLanguage.C99;

            /// <summary>
            /// Returns a string representing the producer information for the QIR
            /// </summary>
            public static string GetQirProducerIdent()
            {
                AssemblyName compilationInfo = CompilationLoader.GetQSharpCompilerAssemblyName();
                AssemblyName qirGenerationInfo = Assembly.GetExecutingAssembly().GetName();
                return compilationInfo.Name + " with " + qirGenerationInfo.Name + " V " + qirGenerationInfo.Version;
            }
        }

// SECTION: Exposed member variables

        /// <summary>
        /// Contains the location information for the statement nodes we are currently parsing
        /// </summary>
        internal Stack<QsLocation?> StatementLocationStack { get; }

        /// <summary>
        /// Contains the location information for the Expression we are inside
        /// </summary>
        internal Stack<DataTypes.Range?> ExpressionRangeStack { get; }

        /// <summary>
        /// Contains the location information for the namespace element we are inside
        /// </summary>
        internal QsLocation? CurrentNamespaceElementLocation { get; set; }

// SECTION: Private member variables

        /// <summary>
        /// The GenerationContext that owns this DebugInfoManager
        /// </summary>
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Contains the DIBuilders included in the module related to this DebugInfoManager.
        /// The key is the source file path of the DebugInfoBuilder's compile unit
        /// </summary>
        private readonly Dictionary<string, DebugInfoBuilder> dIBuilders;

// SECTION: Exposed functions

        /// <summary>
        /// Constructs a DebugInfoManater with access to the <see cref="GenerationContext"/> as the shared state.
        /// </summary>
        internal DebugInfoManager(GenerationContext generationContext, bool debugSymbolsEnabled)
        {
            this.sharedState = generationContext;
            Config.DebugSymbolsEnabled = debugSymbolsEnabled;
            this.StatementLocationStack = new Stack<QsLocation?>();
            this.ExpressionRangeStack = new Stack<DataTypes.Range?>();
            this.dIBuilders = new Dictionary<string, DebugInfoBuilder>();
        }

        /// <summary>
        /// If <see cref="Config.DebugSymbolsEnabled"/> is set to true, creates and adds top level debug info to the module.
        /// Note: because this is called from within the constructor of the GenerationContext,
        /// we cannot access this.Module or anything else that uses <see cref="sharedState"/>.
        /// </summary>
        /// <param name="owningModule">The <see cref="BitcodeModule"/> that this DebugInfoManager is related to</param>
        internal void AddTopLevelDebugInfo(BitcodeModule owningModule)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            // Add Module identity and Module Flags
            owningModule.AddProducerIdentMetadata(Config.GetQirProducerIdent());
            owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DwarfVersionValue, Config.DwarfVersion);
            owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DebugVersionValue, BitcodeModule.DebugMetadataVersion);
            owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, Config.CodeviewName, Config.CodeviewVersion); // TODO: We seem to need this flag and not Dwarf in order to debug on Windows. Need to look into why LLDB is using CodeView on Windows

            // TODO: could be useful to have target-specific module flags at some point
            // Examples: AddModuleFlag(ModuleFlagBehavior.Error, "PIC Level", 2); (ModuleFlagBehavior.Error, "wchar_size", 4); (ModuleFlagBehavior.Error, "min_enum_size", 4)
        }

        /// <summary>
        /// Creates a moduleID which is a list of all of the source files with debug info
        /// and makes the necessary calls to finalize the <see cref="DebugInfoBuilder"/>s.
        /// </summary>
        internal void FinalizeDebugInfo()
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            string moduleID = "";
            foreach (KeyValuePair<string, DebugInfoBuilder> entry in this.dIBuilders)
            {
                entry.Value.Finish(); // Must be called for every DIBuilder after all QIR generation

                string sourcePath = entry.Key;
                if (!string.IsNullOrEmpty(moduleID))
                {
                    moduleID += ", ";
                }

                moduleID += Path.GetFileName(sourcePath);
            }

            // TODO: set module ID. Decide how to actually create the moduleID (above way could be very long)
        }

        /// <summary>
        /// If <see cref="Config.DebugSymbolsEnabled"/> is set to true, emits a location allowing for a breakpoint when debugging. Expects a 0-based <see cref="Position"/>
        /// relative to the namespace and statement stack of parent <see cref="QsStatement"/>s,
        /// which is converted to a 1-based <see cref="DebugPosition"/>.
        /// </summary>
        /// <param name="relativePosition">The 0-based <see cref="Position"/> at which to emit the location
        /// relative to the namespace and stack of parent <see cref="QsStatement"/>s.</param>
        internal void EmitLocationWithOffset(Position? relativePosition)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            DISubProgram? subProgram = this.sharedState.CurrentFunction?.DISubProgram;
            QsLocation? namespaceLocation = this.sharedState.DIManager.CurrentNamespaceElementLocation;

            if (namespaceLocation != null && relativePosition != null && subProgram != null)
            {
                Position absolutePosition = namespaceLocation.Offset + this.TotalOffsetFromStatements() + relativePosition;
                this.EmitLocation(absolutePosition, subProgram);
            }
        }

        /// <summary>
        /// If <see cref="Config.DebugSymbolsEnabled"/> is set to true, emits the current location allowing for a breakpoint when debugging.
        /// </summary>
        internal void EmitLocation()
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            this.EmitLocationWithOffset(Position.Zero);
        }

        /// <summary>
        /// If the debug flag is set to true, this creates the debug information for a local variable.
        /// </summary>
        /// <param name="name">The name of the local variable.</param>
        /// <param name="value">The value of the variable.</param>
        internal void CreateLocalVariable(string name, IValue value)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            // Get the DebugInfoBuilder for this variable
            DISubProgram? subProgram = this.sharedState.CurrentFunction?.DISubProgram;
            string? sourcePath = subProgram?.File?.Path;
            if (string.IsNullOrEmpty(sourcePath))
            {
                return;
            }

            DebugInfoBuilder dIBuilder = this.GetOrCreateDIBuilder(sourcePath);

            // Get the type information for the variable
            ResolvedType? resType = value.QSharpType;
            DIType? dIType = resType == null ? null : this.DebugTypeFromQsharpType(resType, dIBuilder)?.DIType;

            // Get the location information for the variable declaration
            QsLocation? namespaceLocation = this.sharedState.DIManager.CurrentNamespaceElementLocation;
            Position namespaceOffset = namespaceLocation?.Offset ?? Position.Zero;
            Position absolutePosition = namespaceOffset + this.TotalOffsetFromStatements() + this.TotalOffsetFromExpressions();

            if (subProgram != null && dIType != null)
            {
                // LLVM Native API has a bug and will crash if we pass in a null dIType
                // for either CreateLocalVariable or InsertDeclare (https://bugs.llvm.org/show_bug.cgi?id=52459)

                // Create the debug info for the local variable
                DILocalVariable dIVar = dIBuilder.CreateLocalVariable(
                    subProgram,
                    name,
                    dIBuilder.CompileUnit?.File,
                    ConvertToDebugPosition(absolutePosition),
                    dIType,
                    alwaysPreserve: true,
                    DebugInfoFlags.None);

                DILocation dILocation = new DILocation(
                    this.Context,
                    ConvertToDebugPosition(absolutePosition),
                    subProgram);

                if (this.sharedState.CurrentBlock != null)
                {
                    // Create the debug info for the local variable declaration
                    dIBuilder.InsertDeclare(
                        storage: value.Value,
                        varInfo: dIVar,
                        location: dILocation,
                        insertAtEnd: this.sharedState.CurrentBlock);
                }
            }
        }

        /// <summary>
        /// Creates a global function. If the debug flag is set to true, includes debug information
        /// with the global function.
        /// </summary>
        /// <param name="spec">The <see cref="QsSpecialization"/> for the function.</param>
        /// <param name="mangledName">The mangled name for the function.</param>
        /// <param name="signature">The signature for the function.</param>
        /// <returns>The <see cref="IrFunction"/> corresponding to the given Q# type.</returns>
        internal IrFunction CreateGlobalFunction(QsSpecialization spec, string mangledName, IFunctionType signature)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return this.CreateFunctionNoDebug(mangledName, signature);
            }

            if (spec.Kind == QsSpecializationKind.QsBody)
            {
                // Set up the DIBuilder
                string sourcePath = spec.Source.CodeFile;
                DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
                DIFile debugFile = curDIBuilder.CreateFile(sourcePath);

                // Set up debug info for the function
                IDebugType<ITypeRef, DIType>? retDebugType;
                IDebugType<ITypeRef, DIType>?[] argDebugTypes;
                retDebugType = this.DebugTypeFromQsharpType(spec.Signature.ReturnType, curDIBuilder);
                argDebugTypes = spec.Signature.ArgumentType.Resolution is ResolvedTypeKind.TupleType ts ?
                    ts.Item.Select(t => this.DebugTypeFromQsharpType(t, curDIBuilder)).ToArray() :
                    new IDebugType<ITypeRef, DIType>?[] { this.DebugTypeFromQsharpType(spec.Signature.ArgumentType, curDIBuilder) };
                string shortName = spec.Parent.Name; // We want to show the user the short name in the debug info instead of the mangled name

                // Get the location info
                QsNullable<QsLocation> debugLocation = spec.Location;
                if (debugLocation.IsNull)
                {
                    return this.CreateFunctionNoDebug(mangledName, signature);
                }

                Position absolutePosition = debugLocation.Item.Offset; // The position stored in a QsSpecialization is absolute, not relative to the ancestor namespace and statements

                if (retDebugType != null && !argDebugTypes.Contains(null))
                {
                    // Create the function with debug info
                    DebugFunctionType debugSignature = new DebugFunctionType(signature, curDIBuilder, DebugInfoFlags.None, retDebugType, argDebugTypes!);
                    IrFunction func = this.Module.CreateFunction(
                        scope: curDIBuilder.CompileUnit,
                        name: shortName,
                        mangledName: mangledName,
                        file: debugFile,
                        linePosition: ConvertToDebugPosition(absolutePosition),
                        signature: debugSignature,
                        isLocalToUnit: true, // We're using the compile unit from the source file this was declared in
                        isDefinition: true, // if this isn't set to true, it results in temporary metadata nodes that don't get resolved.
                        scopeLinePosition: ConvertToDebugPosition(absolutePosition), // TODO: Could make more exact bc of formatting and white space (see lastParamLocation in Kaleidescope tutorial)
                        debugFlags: DebugInfoFlags.None,
                        isOptimized: false,
                        curDIBuilder);

                    return func;
                }
                else
                {
                    return this.CreateFunctionNoDebug(mangledName, signature);
                }
            }
            else
            {
                return this.CreateFunctionNoDebug(mangledName, signature);
            }
        }

        /// <summary>
        /// Creates the debug info for a function argument.
        /// </summary>
        /// <param name="spec">The function's <see cref="QsSpecialization"/></param>
        /// <param name="name">The name of the function's argument.</param>
        /// <param name="value">The value of the function's argument.</param>
        /// <param name="argNo">The index of the argument (expected to be 1-indexed).</param>
        internal void CreateFunctionArgument(QsSpecialization spec, string name, IValue value, int argNo)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            // Get the DIBuilder
            string sourcePath = spec.Source.CodeFile;
            DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
            DIFile debugFile = curDIBuilder.CreateFile(sourcePath);

            // Get the debug location of the function
            QsNullable<QsLocation> debugLocation = spec.Location;
            if (debugLocation.IsNull)
            {
                throw new ArgumentException("Expected a specialiazation with a non-null location");
            }

            // TODO: make this more exact by calculating absolute column position as well
            DebugPosition debugPosition = DebugPosition.FromZeroBasedLine(debugLocation.Item.Offset.Line); // The position stored in a QsSpecialization is absolute, not relative to the ancestor namespace and statements

            // Get necessary debug information
            DIType? dIType = this.DebugTypeFromQsharpType(value.QSharpType, curDIBuilder)?.DIType;
            DISubProgram? subProgram = this.sharedState.CurrentFunction?.DISubProgram;
            DILocalVariable dIVariable = curDIBuilder.CreateArgument(
                subProgram,
                name,
                debugFile,
                debugPosition,
                dIType,
                alwaysPreserve: true,
                DebugInfoFlags.None,
                (ushort)argNo); // Arg numbers are 1-indexed

            if (this.sharedState.CurrentBlock != null && dIType != null && subProgram != null)
            {
                DILocation dILocation = new DILocation(
                    this.Context,
                    debugPosition,
                    subProgram);

                // Create the debug information for a variable declaration
                curDIBuilder.InsertDeclare(
                    storage: value.Value,
                    varInfo: dIVariable,
                    location: dILocation,
                    insertAtEnd: this.sharedState.CurrentBlock);
            }
        }

// SECTION: Private helper functions

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
                di = this.Module.CreateDIBuilder();
                di.CreateCompileUnit(
                    Config.QSharpLanguage,
                    sourcePath,
                    Config.GetQirProducerIdent(),
                    null);
                this.dIBuilders.Add(sourcePath, di);
                return di;
            }
        }

        /// <summary>
        /// If <see cref="Config.DebugSymbolsEnabled"/> is set to true, creates a debug type for a particular Q# type.
        /// The debug type connects a debug info type with its corresponding LLVM Native type.
        /// Note: If the debug type is not implemented, ideally we would return a <see cref="DebugType"/> with a valid LLVM Native type
        /// and a null DIType. However, this causes an error in the QIR emission because of a bug in the native LLVM API, so we return null.
        /// </summary>
        /// <param name="resolvedType">The Q# type to create a DebugType from.</param>
        /// <param name="dIBuilder">The <see cref="DebugInfoBuilder"/> to use to create the debug type.</param>
        /// <returns>The <see cref="DebugType"/> corresponding to the given Q# type. Returns null if the debug type is not implemented.</returns>
        private IDebugType<ITypeRef, DIType>? DebugTypeFromQsharpType(ResolvedType resolvedType, DebugInfoBuilder dIBuilder)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return null;
            }

            // TODO: implement other variable types in TypeTransformation including callables
            return this.sharedState.Types.Transform.DebugTypeFromQsharpType(resolvedType, dIBuilder);
        }

        /// <summary>
        /// Creates a function without debug information. This should be used when the debug information
        /// being created is invalid.
        /// </summary>
        /// <param name="mangledName">The mangled name for the function.</param>
        /// <param name="signature">The signature for the function.</param>
        /// <returns>The created <see cref="IrFunction"/> with no debug info.</returns>
        private IrFunction CreateFunctionNoDebug(string mangledName, IFunctionType signature) => this.Module.CreateFunction(mangledName, signature);

// SECTION: Private location-related helper functions

        /// <summary>
        /// If <see cref="Config.DebugSymbolsEnabled"/> is set to true, emits a location allowing for a breakpoint when debugging. Expects a 0-based <see cref="Position"/>
        /// which is converted to a 1-based <see cref="DebugPosition"/>.
        /// </summary>
        /// <param name="absolutePosition">The 0-based <see cref="Position"/> at which to emit the location.</param>
        /// <param name="localScope">The <see cref="DILocalScope"/>at this location. If the argument is null,
        /// this will not emit a location.</param>
        private void EmitLocation(Position absolutePosition, DILocalScope? localScope)
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            if (localScope != null)
            {
                this.CurrentInstrBuilder.SetDebugLocation(ConvertToDebugPosition(absolutePosition), localScope);
            }
        }

        /// <summary>
        /// Converts from a 0-based <see cref="Position"/> to a 1-based <see cref="DebugPosition"/>.
        /// </summary>
        /// <param name="position">The 0-based <see cref="Position"/></param>
        /// <return>The 1-based <see cref="DebugPosition"/></return>
        private static DebugPosition ConvertToDebugPosition(Position position)
        {
            return DebugPosition.FromZeroBased(position.Line, position.Column);
        }

        /// <summary>
        /// Sums up the offsets from the stack of statement locations <see cref="StatementLocationStack"/>.
        /// </summary>
        /// <returns>The offset that the current stack of statements yields as a <see cref="Position"/>.</returns>
        private Position TotalOffsetFromStatements()
        {
            Position offset = Position.Zero;

            foreach (QsLocation? loc in this.StatementLocationStack)
            {
                if (loc != null)
                {
                    offset += loc.Offset;
                    return offset;
                }
            }

            return offset;
        }

        /// <summary>
        /// Sums up the offsets from the stack of expression ranges <see cref="ExpressionRangeStack"/>.
        /// </summary>
        /// <returns>The offset that the current stack of expression yields as a <see cref="Position"/>.</returns>
        private Position TotalOffsetFromExpressions()
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

// SECTION: Private helpers to access content from the shared state

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
    }
}
