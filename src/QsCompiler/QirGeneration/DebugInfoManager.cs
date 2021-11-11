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
// SECTION: Config values

        /// <summary>
        /// Whether or not to emit debug information during QIR generation
        /// </summary>
        public bool DebugFlag { get; } = false;

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
        internal DebugInfoManager(GenerationContext generationContext)
        {
            this.sharedState = generationContext;
            this.StatementLocationStack = new Stack<QsLocation?>();
            this.ExpressionRangeStack = new Stack<DataTypes.Range?>();
            this.dIBuilders = new Dictionary<string, DebugInfoBuilder>();
        }

        /// <summary>
        /// If DebugFlag is set to true, creates Adds top level debug info to the module,
        /// Note: because this is called from within the constructor of the GenerationContext,
        /// we cannot access this.Module or anything else that uses this.sharedState
        /// </summary>
        /// <param name="owningModule">The <see cref="BitcodeModule"/> that this DebugInfoManager is related to</param>
        internal void AddTopLevelDebugInfo(BitcodeModule owningModule)
        {
            if (this.DebugFlag)
            {
                // Add Module identity and Module Flags
                owningModule.AddProducerIdentMetadata(GetQirProducerIdent());
                owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DwarfVersionValue, this.GetDwarfVersion());
                owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, BitcodeModule.DebugVersionValue, BitcodeModule.DebugMetadataVersion);
                owningModule.AddModuleFlag(ModuleFlagBehavior.Warning, this.GetCodeViewName(), this.GetCodeViewVersion()); // TODO: We seem to need this flag and not Dwarf in order to debug on Windows. Need to look into why LLDB is using CodeView on Windows

                // TODO: could be useful to have target-specific module flags at some point
                // Examples: AddModuleFlag(ModuleFlagBehavior.Error, "PIC Level", 2); (ModuleFlagBehavior.Error, "wchar_size", 4); (ModuleFlagBehavior.Error, "min_enum_size", 4)
            }
        }

        /// <summary>
        /// Creates a moduleID which is a list of all of the source files with debug info
        /// and makes the necessary calls to finalize the <see cref="DebugInfoBuilder"/>s.
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
        /// If <see cref="DebugFlag"/> is set, emits a location allowing for a breakpoint when debugging. Expects a 0-based <see cref="Position"/>
        /// relative to the namespace and stack of parent <see cref="QsStatement"/>s,
        /// which is converted to a 1-based <see cref="DebugPosition"/>. The position
        /// </summary>
        /// <param name="relativePosition">The 0-based <see cref="Position"/> at which to emit the location,
        /// relative to the namespace and stack of parent <see cref="QsStatement"/>s.</param>
        internal void EmitLocation(Position? relativePosition)
        {
            if (this.DebugFlag)
            {
                DISubProgram? sp = this.sharedState.CurrentFunction?.DISubProgram;
                QsLocation? namespaceLoc = this.sharedState.DIManager.CurrentNamespaceElementLocation;

                if (namespaceLoc != null && relativePosition != null && sp != null)
                {
                    Position absolutePosition = namespaceLoc.Offset + this.TotalOffsetFromStatements() + relativePosition;
                    this.EmitLocation(absolutePosition, sp);
                }
            }
        }

        /// <summary>
        /// If the debug flag is set to true, this creates the debug information for a local variable.
        /// </summary>
        /// <param name="name">The name of the local variable.</param>
        /// <param name="value">The value of the variable.</param>
        internal void CreateLocalVariable(string name, IValue value)
        {
            if (this.DebugFlag)
            {
                // get the DebugInfoBuilder for this variable
                DISubProgram? subProgram = this.sharedState.CurrentFunction?.DISubProgram;
                string? sourcePath = subProgram?.File?.Path;
                if (string.IsNullOrEmpty(sourcePath))
                {
                    return;
                }

                DebugInfoBuilder diBuilder = this.GetOrCreateDIBuilder(sourcePath);

                // get the type information for the variable
                ResolvedType? resType = value.QSharpType;
                DIType? dIType = resType == null ? null : this.GetDebugTypeFor(resType, diBuilder)?.DIType;

                // get the location information for the variable declaration
                QsLocation? namespaceLoc = this.sharedState.DIManager.CurrentNamespaceElementLocation;
                Position namespaceOffset = namespaceLoc?.Offset ?? Position.Zero;
                Position absolutePosition = namespaceOffset + this.TotalOffsetFromStatements() + this.TotalOffsetFromExpressions();

                if (subProgram != null && dIType != null)
                {
                    // LLVM Native API has a bug and will crash if we pass in a null dIType for either CreateLocalVariable or InsertDeclare

                    // create the debug info for the local variable
                    DILocalVariable dIVar = diBuilder.CreateLocalVariable(
                        subProgram,
                        name,
                        diBuilder.CompileUnit?.File,
                        ConvertToDebugPosition(absolutePosition),
                        dIType,
                        alwaysPreserve: true,
                        DebugInfoFlags.None);

                    DILocation dILoc = new DILocation(
                        this.Context,
                        ConvertToDebugPosition(absolutePosition),
                        subProgram);

                    if (this.sharedState.CurrentBlock != null)
                    {
                        // create the debug info for the local variable declaration
                        diBuilder.InsertDeclare(
                            storage: value.Value,
                            varInfo: dIVar,
                            location: dILoc,
                            insertAtEnd: this.sharedState.CurrentBlock);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a global function. If the debug flag is set, includes debug information
        /// with the global function.
        /// </summary>
        /// <param name="spec">The <see cref="QsSpecialization"/> for the function.</param>
        /// <param name="mangledName">The mangled name for the function.</param>
        /// <param name="signature">The signature for the function.</param>
        /// <param name="isDefinition">Whether this is a function definition (rather than a declaration).</param>
        /// <returns>The <see cref="IrFunction"/> corresponding to the given Q# type.</returns>
        internal IrFunction CreateGlobalFunction(QsSpecialization spec, string mangledName, IFunctionType signature, bool isDefinition)
        {
            if (this.DebugFlag && spec.Kind == QsSpecializationKind.QsBody)
            {
                // set up the DIBuilder
                string sourcePath = spec.Source.CodeFile;
                DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
                DIFile debugFile = curDIBuilder.CreateFile(sourcePath);

                // set up debug info for the function
                IDebugType<ITypeRef, DIType>? retDebugType;
                IDebugType<ITypeRef, DIType>?[] argDebugTypes;
                retDebugType = this.GetDebugTypeFor(spec.Signature.ReturnType, curDIBuilder);
                argDebugTypes = spec.Signature.ArgumentType.Resolution is ResolvedTypeKind.TupleType ts ? ts.Item.Select(t => this.GetDebugTypeFor(t, curDIBuilder)).ToArray() :
                    new IDebugType<ITypeRef, DIType>?[] { this.GetDebugTypeFor(spec.Signature.ArgumentType, curDIBuilder) };
                string shortName = spec.Parent.Name; // We want to show the user the short name in the debug info instead of the mangled name

                // get the location info
                QsNullable<QsLocation> debugLoc = spec.Location;
                if (debugLoc.IsNull)
                {
                    return this.CreateFunctionNoDebug(mangledName, signature);
                }

                Position absolutePosition = debugLoc.Item.Offset; // Because spec is its own namespace, the position is already an absolute position

                if (retDebugType != null && !argDebugTypes.Contains(null))
                {
                    // create the function with debug info
                    DebugFunctionType debugSignature = new DebugFunctionType(signature, curDIBuilder, DebugInfoFlags.None, retDebugType, argDebugTypes!);
                    IrFunction func = this.Module.CreateFunction(
                        scope: curDIBuilder.CompileUnit,
                        name: shortName,
                        mangledName: mangledName,
                        file: debugFile,
                        linePosition: ConvertToDebugPosition(absolutePosition),
                        signature: debugSignature,
                        isLocalToUnit: true, // we're using the compile unit from the source file this was declared in
                        isDefinition: isDefinition,
                        scopeLinePosition: ConvertToDebugPosition(absolutePosition), // TODO: Could make more exact bc of formatting and white space (see lastParamLocation in Kaleidescope tutorial)
                        debugFlags: DebugInfoFlags.None,
                        isOptimized: false,
                        curDIBuilder);

                    this.EmitLocation(absolutePosition, func.DISubProgram);
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
            if (this.DebugFlag)
            {
                // get the DIBuilder
                string sourcePath = spec.Source.CodeFile;
                DebugInfoBuilder curDIBuilder = this.GetOrCreateDIBuilder(sourcePath);
                DIFile debugFile = curDIBuilder.CreateFile(sourcePath);

                // get the debug location of the function
                QsNullable<QsLocation> debugLoc = spec.Location;
                if (debugLoc.IsNull)
                {
                    throw new ArgumentException("Expected a specialiazation with a non-null location");
                }

                // TODO: make this more exact by calculating absolute column position as well
                DebugPosition debugPos = DebugPosition.FromZeroBasedLine(debugLoc.Item.Offset.Line); // Because spec is its own namespace, this is an absolute position

                // get necessary debug information
                DIType? dIType = this.GetDebugTypeFor(value.QSharpType, curDIBuilder)?.DIType;
                DISubProgram? sp = this.sharedState.CurrentFunction?.DISubProgram;
                DILocalVariable dIVar = curDIBuilder.CreateArgument(
                    sp,
                    name,
                    debugFile,
                    debugPos,
                    dIType,
                    alwaysPreserve: true,
                    DebugInfoFlags.None,
                    (ushort)argNo); // arg numbers are 1-indexed

                if (this.sharedState.CurrentBlock != null && dIType != null && sp != null)
                {
                    DILocation dILoc = new DILocation(
                        this.Context,
                        debugPos,
                        sp);

                    // create the debug information for a variable declaration
                    curDIBuilder.InsertDeclare(
                        storage: value.Value,
                        varInfo: dIVar,
                        location: dILoc,
                        insertAtEnd: this.sharedState.CurrentBlock);
                }
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
                    QSharpLanguage,
                    sourcePath,
                    GetQirProducerIdent(),
                    null);
                this.dIBuilders.Add(sourcePath, di);
                return di;
            }
        }

        /// <summary>
        /// Creates a debug type for a particular Q# type. The debug type connects a debug info type with its
        /// corresponding LLVM Native type.
        /// </summary>
        /// <param name="resolvedType">The Q# type to create a DebugType from.</param>
        /// <param name="dIBuilder">The <see cref="DebugInfoBuilder"/> to use to create the debug type.</param>
        /// <returns>The Debug Type corresponding to the given Q# type. Returns null if the debug type is not implemented.</returns>
        private IDebugType<ITypeRef, DIType>? GetDebugTypeFor(ResolvedType resolvedType, DebugInfoBuilder dIBuilder)
        {
            // TODO: include other variable types including callables
            if (resolvedType.Resolution.IsInt)
            {
                return new DebugBasicType(this.sharedState.NativeLlvmTypes.Int, dIBuilder, TypeNames.Int, DiTypeKind.Signed);
            }
            else if (resolvedType.Resolution.IsUnitType)
            {
                return DebugType.Create<ITypeRef, DIType>(this.sharedState.NativeLlvmTypes.Void, null);
            }
            else
            {
                // Note: Ideally we would return a DebugType with a valid LLVM Native type and a null DIType. However, this causes an error
                // in the QIR emission because of a bug in the native LLVM API.
                return null;
            }
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
        /// If <see cref="DebugFlag"/> is set, emits a location allowing for a breakpoint when debugging. Expects a 0-based <see cref="Position"/>
        /// which is converted to a 1-based <see cref="DebugPosition"/>.
        /// </summary>
        /// <param name="absolutePosition">The 0-based <see cref="Position"/> at which to emit the location.</param>
        /// <param name="localScope">The <see cref="DILocalScope"/>at this location. If the argument is null,
        /// this will not emit a location.</param>
        private void EmitLocation(Position absolutePosition, DILocalScope? localScope)
        {
            if (this.DebugFlag && localScope != null)
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

// SECTION: Private getters

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
