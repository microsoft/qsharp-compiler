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
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// This class holds shared utility routines for generating QIR debug information.
    /// It contains a reference to the GenerationContext that owns it because it needs
    /// access to the same shared state represented by the GenerationContext.
    /// </summary>
    internal sealed class DebugInfoManager
    {
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
            public static string QirProducerIdentifier =>
                $"{CompilationLoader.QSharpCompilerAssemblyName.Name} with {DebugInfoProducerAssemblyName.Name} V {DebugInfoProducerAssemblyName.Version}";
        }

        private static AssemblyName DebugInfoProducerAssemblyName => Assembly.GetExecutingAssembly().GetName();

        /// <summary><see cref="DebugPosition"/> represents a line/col position in source code and is 1-based.</summary>
        /// <summary>If a column number is not provided, 1 is used.</summary>
        private class DebugPosition
        {
            public uint Line { get; set; }

            public uint Column { get; set; }

            private DebugPosition(uint line, uint col)
            {
                this.Line = line;
                this.Column = col;
            }

            public static DebugPosition FromZeroBased(uint line, uint col)
            {
                return new DebugPosition(line + 1, col + 1);
            }

            public static DebugPosition FromZeroBased(int line, int col)
            {
                return FromZeroBased((uint)line, (uint)col);
            }

            public static DebugPosition FromZeroBasedLine(uint line)
            {
                return new DebugPosition(line + 1, 1);
            }

            public static DebugPosition FromZeroBasedLine(int line)
            {
                return FromZeroBasedLine((uint)line);
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

        public static string DebugTypeNotSupportedMessage => "This debug type is not yet supported";

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
        /// Constructs a DebugInfoManager with access to the <see cref="GenerationContext"/> as the shared state.
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
            owningModule.AddProducerIdentMetadata(Config.QirProducerIdentifier);
            owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DwarfVersionValue, Config.DwarfVersion);
            owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DebugVersionValue, BitcodeModule.DebugMetadataVersion);
            owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, Config.CodeviewName, Config.CodeviewVersion);
        }

        /// <summary>
        /// Makes the necessary calls to finalize the <see cref="DebugInfoBuilder"/>s.
        /// </summary>
        internal void FinalizeDebugInfo()
        {
            if (!Config.DebugSymbolsEnabled)
            {
                return;
            }

            foreach (KeyValuePair<string, DebugInfoBuilder> entry in this.dIBuilders)
            {
                entry.Value.Finish(); // must be called for every DIBuilder after all QIR generation
            }
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
        internal void CreateLocalVariable(string name, IValue value, bool isMutableBinding)
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
            QsLocation? namespaceLocation = this.CurrentNamespaceElementLocation;
            Position namespaceOffset = namespaceLocation?.Offset ?? Position.Zero;
            Position absolutePosition = namespaceOffset + this.TotalOffsetFromStatements() + this.TotalOffsetFromExpressions();

            // LLVM Native API has a bug and will crash if we pass in a null dIType
            // for either CreateLocalVariable or InsertDeclare (https://bugs.llvm.org/show_bug.cgi?id=52459)
            if (subProgram != null && dIType != null)
            {
                if (dIType.Name.Equals(DebugTypeNotSupportedMessage))
                {
                    name += " [value display for this type is not currently supported]";
                }

                DebugPosition debugLocation = ConvertToDebugPosition(absolutePosition);

                // create the debug info for the local variable
                DILocalVariable dIVar = dIBuilder.CreateLocalVariable(
                    subProgram,
                    name,
                    dIBuilder.CompileUnit?.File,
                    debugLocation.Line,
                    dIType,
                    alwaysPreserve: true,
                    DebugInfoFlags.None);

                DILocation dILocation = new DILocation(
                    this.Context,
                    debugLocation.Line,
                    debugLocation.Column,
                    subProgram);

                if (this.sharedState.CurrentBlock != null)
                {
                    // The variable is represented as a pointer if it's mutable, or its value if it's immutable.
                    var variable = isMutableBinding ? ((PointerValue)value).Pointer : value.Value;
                    dIBuilder.InsertValue(
                        value: variable,
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
                // set up the DIBuilder
                string sourcePath = spec.Source.CodeFile;
                DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
                DIFile debugFile = curDIBuilder.CreateFile(sourcePath);

                // set up debug info for the function
                IDebugType<ITypeRef, DIType>? retDebugType;
                IDebugType<ITypeRef, DIType>?[] argDebugTypes;
                retDebugType = this.DebugTypeFromQsharpType(spec.Signature.ReturnType, curDIBuilder);
                argDebugTypes = spec.Signature.ArgumentType.Resolution is ResolvedTypeKind.TupleType ts ?
                    ts.Item.Select(t => this.DebugTypeFromQsharpType(t, curDIBuilder)).ToArray() :
                    new IDebugType<ITypeRef, DIType>?[] { this.DebugTypeFromQsharpType(spec.Signature.ArgumentType, curDIBuilder) };
                string shortName = spec.Parent.Name;

                // Get the location info
                QsNullable<QsLocation> debugLocation = spec.Location;
                if (debugLocation.IsNull)
                {
                    return this.CreateFunctionNoDebug(mangledName, signature);
                }

                Position absolutePosition = debugLocation.Item.Offset; // the position stored in a QsSpecialization is absolute, not relative to the ancestor namespace and statements

                 // checking the debug types for null ensures we have valid debug info
                if (retDebugType != null && !argDebugTypes.Contains(null))
                {
                    DebugPosition debugPosition = ConvertToDebugPosition(absolutePosition);

                    // create the function with debug info
                    DebugFunctionType debugSignature = new DebugFunctionType(signature, curDIBuilder, DebugInfoFlags.None, retDebugType, argDebugTypes!);
                    IrFunction func = this.Module.CreateFunction(
                        scope: curDIBuilder.CompileUnit,
                        name: shortName,
                        mangledName: mangledName,
                        file: debugFile,
                        line: debugPosition.Line,
                        signature: debugSignature,
                        isLocalToUnit: true, // we're using the compile unit from the source file this was declared in
                        isDefinition: true, // if this isn't set to true, it results in temporary metadata nodes that don't get resolved.
                        scopeLine: debugPosition.Line,
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

            // get the DIBuilder
            string sourcePath = spec.Source.CodeFile;
            DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
            DIFile debugFile = curDIBuilder.CreateFile(sourcePath);

            // get the debug location of the function
            QsNullable<QsLocation> debugLocation = spec.Location;
            if (debugLocation.IsNull)
            {
                throw new ArgumentException("Expected a specialiazation with a non-null location");
            }

            // the position stored in a QsSpecialization is absolute, not relative to the ancestor namespace and statements
            DebugPosition debugPosition = DebugPosition.FromZeroBasedLine(debugLocation.Item.Offset.Line);

            // get necessary debug information
            DIType? dIType = this.DebugTypeFromQsharpType(value.QSharpType, curDIBuilder)?.DIType;
            DISubProgram? subProgram = this.sharedState.CurrentFunction?.DISubProgram;
            DILocalVariable dIVariable = curDIBuilder.CreateArgument(
                subProgram,
                name,
                debugFile,
                debugPosition.Line,
                dIType,
                alwaysPreserve: true,
                DebugInfoFlags.None,
                (ushort)argNo); // arg numbers are 1-indexed

            if (this.sharedState.CurrentBlock != null && dIType != null && subProgram != null)
            {
                DILocation dILocation = new DILocation(
                    this.Context,
                    debugPosition.Line,
                    debugPosition.Column,
                    subProgram);

                // all arguments are passed by value
                Value variable = value.Value;
                curDIBuilder.InsertValue(
                    value: variable,
                    varInfo: dIVariable,
                    location: dILocation,
                    insertAtEnd: this.sharedState.CurrentBlock);
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
                di = this.Module.CreateDIBuilder();
                di.CreateCompileUnit(
                    Config.QSharpLanguage,
                    sourcePath,
                    Config.QirProducerIdentifier,
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

            DebugPosition debugPosition = ConvertToDebugPosition(absolutePosition);
            if (localScope != null)
            {
                this.CurrentInstrBuilder.SetDebugLocation(debugPosition.Line, debugPosition.Column, localScope);
            }
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
