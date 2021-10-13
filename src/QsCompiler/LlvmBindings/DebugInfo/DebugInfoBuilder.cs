// -----------------------------------------------------------------------
// <copyright file="DebugInfoBuilder.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Defines the amount of debug information to emit</summary>
    public enum DwarfEmissionKind
    {
        /// <summary>No debug information</summary>
        None = 0,

        /// <summary>Full Debug information</summary>
        Full,

        /// <summary>Emit line tables only</summary>
        LineTablesOnly,
    }

    /// <summary>Describes the kind of macro declaration</summary>
    [SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Simple 1:1 mapping to native names and values, there is no 0 value")]
    public enum MacroKind
    {
        /// <summary>Macro definition</summary>
        Define = LLVMDWARFMacinfoRecordType.LLVMDWARFMacinfoRecordTypeDefine,

        /// <summary>Undefine a macro</summary>
        Undefine = LLVMDWARFMacinfoRecordType.LLVMDWARFMacinfoRecordTypeMacro,

        /* These are not supported in the LLVM native code yet, so no point in exposing them at this time
            /// <summary>Start of file macro</summary>
            StartFile = LLVMDWARFMacinfoRecordType.LLVMDWARFMacinfoRecordTypeStartFile,

            /// <summary>End of file macro</summary>
            EndFile = LLVMDWARFMacinfoRecordType.LLVMDWARFMacinfoRecordTypeEndFile,

            /// <summary>Vendor specific extension type</summary>
            VendorExt = LLVMDWARFMacinfoRecordType.LLVMDWARFMacinfoRecordTypeVendorExt
        */
    }

    /// <summary>DebugInfoBuilder is a factory class for creating DebugInformation for an LLVM <see cref="BitcodeModule"/></summary>
    /// <remarks>
    /// Many Debug information metadata nodes are created with unresolved references to additional
    /// metadata. To ensure such metadata is resolved applications should call the <see cref="Finish()"/>
    /// method to resolve and finalize the metadata. After this point only fully resolved nodes may
    /// be added to ensure that the data remains valid.
    /// </remarks>
    /// <seealso href="xref:llvm_sourceleveldebugging">LLVM Source Level Debugging</seealso>
    public sealed class DebugInfoBuilder
    {
        /// <summary>Gets the module that owns this builder</summary>
        public BitcodeModule OwningModule { get; }

        /// <summary>Creates a new <see cref="DICompileUnit"/></summary>
        /// <param name="language"><see cref="SourceLanguage"/> for the compilation unit</param>
        /// <param name="sourceFilePath">Full path to the source file of this compilation unit</param>
        /// <param name="producer">Name of the application processing the compilation unit</param>
        /// <param name="optimized">Flag to indicate if the code in this compilation unit is optimized</param>
        /// <param name="compilationFlags">Additional tool specific flags</param>
        /// <param name="runtimeVersion">Runtime version</param>
        /// <returns><see cref="DICompileUnit"/></returns>
        public DICompileUnit CreateCompileUnit(
            SourceLanguage language,
            string sourceFilePath,
            string? producer,
            bool optimized,
            string? compilationFlags,
            uint runtimeVersion)
        {
            return this.CreateCompileUnit(
                language,
                Path.GetFileName(sourceFilePath),
                Path.GetDirectoryName(sourceFilePath) ?? Environment.CurrentDirectory,
                producer,
                optimized,
                compilationFlags,
                runtimeVersion);
        }

        /// <summary>Creates a new <see cref="DICompileUnit"/></summary>
        /// <param name="language"><see cref="SourceLanguage"/> for the compilation unit</param>
        /// <param name="fileName">Name of the source file of this compilation unit (without any path)</param>
        /// <param name="fileDirectory">Path of the directory containing the file</param>
        /// <param name="producer">Name of the application processing the compilation unit</param>
        /// <param name="optimized">Flag to indicate if the code in this compilation unit is optimized</param>
        /// <param name="compilationFlags">Additional tool specific flags</param>
        /// <param name="runtimeVersion">Runtime version</param>
        /// <returns><see cref="DICompileUnit"/></returns>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "DICompileUnit", Justification = "It is spelled correctly 8^)")]
        public DICompileUnit CreateCompileUnit(
            SourceLanguage language,
            string fileName,
            string fileDirectory,
            string? producer,
            bool optimized,
            string? compilationFlags,
            uint runtimeVersion)
        {
            if (producer == null)
            {
                producer = string.Empty;
            }

            if (compilationFlags == null)
            {
                compilationFlags = string.Empty;
            }

            if (this.OwningModule.DICompileUnit != null)
            {
                throw new InvalidOperationException();
            }

            var file = this.CreateFile(fileName, fileDirectory);
            var handle = this.BuilderHandle.CreateCompileUnit(
                (LLVMDWARFSourceLanguage)language,
                file.MetadataHandle,
                producer,
                optimized ? 1 : 0,
                compilationFlags,
                runtimeVersion,
                string.Empty,
                LLVMDWARFEmissionKind.LLVMDWARFEmissionFull,
                0,
                0,
                0,
                string.Empty,
                string.Empty);

            this.OwningModule.DICompileUnit = MDNode.FromHandle<DICompileUnit>(handle)!;
            return this.OwningModule.DICompileUnit;
        }

        /// <summary>Creates a debugging information temporary entry for a macro file</summary>
        /// <param name="parent">Macro file parent, if any</param>
        /// <param name="line">Source line where the macro file is included</param>
        /// <param name="file">File information for the file containing the macro</param>
        /// <returns>Newly created <see cref="DIMacroFile"/></returns>
        /// <remarks>
        /// The list of macro node direct children is calculated by the use of the <see cref="CreateMacro"/>
        /// functions parentFile parameter.
        /// </remarks>
        public DIMacroFile CreateTempMacroFile(DIMacroFile? parent, uint line, DIFile? file)
        {
            var handle = this.BuilderHandle.CreateTempMacroFile(
                parent?.MetadataHandle ?? default,
                line,
                file?.MetadataHandle ?? default);

            return MDNode.FromHandle<DIMacroFile>(handle)!;
        }

        /// <summary>Create a macro</summary>
        /// <param name="parentFile">Parent file containing the macro</param>
        /// <param name="line">Source line number where the macro is defined</param>
        /// <param name="kind">Kind of macro</param>
        /// <param name="name">Name of the macro</param>
        /// <param name="value">Value of the macro (use String.Empty for <see cref="MacroKind.Undefine"/>)</param>
        /// <returns>Newly created macro node</returns>
        public DIMacro CreateMacro(DIMacroFile? parentFile, uint line, MacroKind kind, string name, string value)
        {
            switch (kind)
            {
            case MacroKind.Define:
            case MacroKind.Undefine:
                break;
            default:
                throw new NotSupportedException("LLVM currently only supports MacroKind.Define and MacroKind.Undefine");
            }

            var handle = this.BuilderHandle.CreateMacro(
                parentFile?.MetadataHandle ?? default,
                line,
                (LLVMDWARFMacinfoRecordType)kind,
                name,
                value);

            return MDNode.FromHandle<DIMacro>(handle)!;
        }

        /// <summary>Creates a <see cref="DINamespace"/></summary>
        /// <param name="scope">Containing scope for the namespace or null if the namespace is a global one</param>
        /// <param name="name">Name of the namespace</param>
        /// <param name="exportSymbols">export symbols</param>
        /// <returns>Debug namespace</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DINamespace CreateNamespace(DIScope? scope, string name, bool exportSymbols)
        {
            var handle = this.BuilderHandle.CreateNameSpace(scope?.MetadataHandle ?? default, name, exportSymbols);
            return MDNode.FromHandle<DINamespace>(handle)!;
        }

        /// <summary>Creates a <see cref="DIFile"/></summary>
        /// <param name="path">Path of the file (may be <see langword="null"/> or empty)</param>
        /// <returns>
        /// <see cref="DIFile"/> or <see langword="null"/> if <paramref name="path"/>
        /// is <see langword="null"/> empty, or all whitespace
        /// </returns>
        public DIFile CreateFile(string? path)
        {
            string? fileName = path is null ? null : Path.GetFileName(path);
            string? directory = path is null ? null : Path.GetDirectoryName(path);
            return this.CreateFile(fileName, directory);
        }

        /// <summary>Creates a <see cref="DIFile"/></summary>
        /// <param name="fileName">Name of the file (may be <see langword="null"/> or empty)</param>
        /// <param name="directory">Path of the directory containing the file (may be <see langword="null"/> or empty)</param>
        /// <returns>
        /// <see cref="DIFile"/> created
        /// </returns>
        public DIFile CreateFile(string? fileName, string? directory)
        {
            var handle = this.BuilderHandle.CreateFile(fileName, directory);
            return MDNode.FromHandle<DIFile>(handle)!;
        }

        /// <summary>Creates a new <see cref="DILexicalBlock"/></summary>
        /// <param name="scope"><see cref="DIScope"/> for the block</param>
        /// <param name="file"><see cref="DIFile"/> containing the block</param>
        /// <param name="line">Starting line number for the block</param>
        /// <param name="column">Starting column for the block</param>
        /// <returns>
        /// <see cref="DILexicalBlock"/> created from the parameters
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DILexicalBlock CreateLexicalBlock(DIScope? scope, DIFile? file, uint line, uint column)
        {
            var handle = this.BuilderHandle.CreateLexicalBlock(
                scope?.MetadataHandle ?? default,
                file?.MetadataHandle ?? default,
                line,
                column);

            return MDNode.FromHandle<DILexicalBlock>(handle)!;
        }

        /// <summary>Creates a <see cref="DILexicalBlockFile"/></summary>
        /// <param name="scope"><see cref="DIScope"/> for the block</param>
        /// <param name="file"><see cref="DIFile"/></param>
        /// <param name="discriminator">Discriminator to disambiguate lexical blocks with the same file info</param>
        /// <returns>
        /// <see cref="DILexicalBlockFile"/> constructed from the parameters
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DILexicalBlockFile CreateLexicalBlockFile(DIScope? scope, DIFile? file, uint discriminator)
        {
            var handle = this.BuilderHandle.CreateLexicalBlockFile(
                scope?.MetadataHandle ?? default,
                file?.MetadataHandle ?? default,
                discriminator);

            return MDNode.FromHandle<DILexicalBlockFile>(handle)!;
        }

        /// <summary>Factory method to create a <see cref="DISubProgram"/> with debug information</summary>
        /// <param name="scope"><see cref="DIScope"/> for the function</param>
        /// <param name="name">Name of the function as it appears in the source language</param>
        /// <param name="mangledName">Linkage (mangled) name of the function</param>
        /// <param name="file"><see cref="DIFile"/> containing the function</param>
        /// <param name="line">starting line of the function definition</param>
        /// <param name="signatureType"><see cref="DISubroutineType"/> for the function's signature type</param>
        /// <param name="isLocalToUnit">Flag to indicate if this function is local to the compilation unit or available externally</param>
        /// <param name="isDefinition">Flag to indicate if this is a definition or a declaration only</param>
        /// <param name="scopeLine">starting line of the first scope of the function's body</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this function</param>
        /// <param name="isOptimized">Flag to indicate if this function is optimized</param>
        /// <param name="function">Underlying LLVM <see cref="IrFunction"/> to attach debug info to</param>
        /// <returns><see cref="DISubProgram"/> created based on the input parameters</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DISubProgram CreateFunction(
            DIScope? scope,
            string name,
            string mangledName,
            DIFile? file,
            uint line,
            DISubroutineType? signatureType,
            bool isLocalToUnit,
            bool isDefinition,
            uint scopeLine,
            DebugInfoFlags debugFlags,
            bool isOptimized,
            IrFunction function)
        {
            // force whitespace strings to empty
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(mangledName))
            {
                mangledName = string.Empty;
            }

            var handle = this.BuilderHandle.CreateFunction(
                scope?.MetadataHandle ?? default,
                name,
                mangledName,
                file?.MetadataHandle ?? default,
                line,
                signatureType?.MetadataHandle ?? default,
                isLocalToUnit ? 1 : 0,
                isDefinition ? 1 : 0,
                scopeLine,
                (LLVMDIFlags)debugFlags,
                isOptimized ? 1 : 0);

            var retVal = MDNode.FromHandle<DISubProgram>(handle)!;
            function.DISubProgram = retVal;
            return retVal;
        }

        /// <summary>Creates a <see cref="DILocalVariable"/> for a given scope</summary>
        /// <param name="scope">Scope the variable belongs to</param>
        /// <param name="name">Name of the variable</param>
        /// <param name="file">File where the variable is declared</param>
        /// <param name="line">Line where the variable is declared</param>
        /// <param name="type">Type of the variable</param>
        /// <param name="alwaysPreserve">Flag to indicate if this variable's debug information should always be preserved</param>
        /// <param name="debugFlags">Flags for the variable</param>
        /// <param name="alignInBits">Variable alignment (in Bits)</param>
        /// <returns><see cref="DILocalVariable"/></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DILocalVariable CreateLocalVariable(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            DIType? type,
            bool alwaysPreserve,
            DebugInfoFlags debugFlags,
            uint alignInBits = 0)
        {
            var handle = this.BuilderHandle.CreateAutoVariable(
                scope?.MetadataHandle ?? default,
                name,
                file?.MetadataHandle ?? default,
                line,
                type?.MetadataHandle ?? default,
                alwaysPreserve,
                (LLVMDIFlags)debugFlags,
                alignInBits);

            return MDNode.FromHandle<DILocalVariable>(handle)!;
        }

        /// <summary>Creates an argument for a function as a <see cref="DILocalVariable"/></summary>
        /// <param name="scope">Scope for the argument</param>
        /// <param name="name">Name of the argument</param>
        /// <param name="file"><see cref="DIFile"/> containing the function this argument is declared in</param>
        /// <param name="line">Line number fort his argument</param>
        /// <param name="type">Debug type for this argument</param>
        /// <param name="alwaysPreserve">Flag to indicate if this argument is always preserved for debug view even if optimization would remove it</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this argument</param>
        /// <param name="argNo">One based argument index on the method (e.g the first argument is 1 not 0 )</param>
        /// <returns><see cref="DILocalVariable"/> representing the function argument</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DILocalVariable CreateArgument(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            DIType? type,
            bool alwaysPreserve,
            DebugInfoFlags debugFlags,
            ushort argNo)
        {
            var handle = this.BuilderHandle.CreateParameterVariable(
                scope?.MetadataHandle ?? default,
                name,
                argNo,
                file?.MetadataHandle ?? default,
                line,
                type?.MetadataHandle ?? default,
                alwaysPreserve,
                (LLVMDIFlags)debugFlags);

            return MDNode.FromHandle<DILocalVariable>(handle)!;
        }

        /// <summary>Construct debug information for a basic type (a.k.a. primitive type)</summary>
        /// <param name="name">Name of the type</param>
        /// <param name="bitSize">Bit size for the type</param>
        /// <param name="encoding"><see cref="DiTypeKind"/> encoding for the type</param>
        /// <param name="diFlags"><see cref="DebugInfoFlags"/> for the type</param>
        /// <returns>Basic type debugging information</returns>
        public DIBasicType CreateBasicType(string name, ulong bitSize, DiTypeKind encoding, DebugInfoFlags diFlags = DebugInfoFlags.None)
        {
            var handle = this.BuilderHandle.CreateBasicType(name, bitSize, (uint)encoding, (LLVMDIFlags)diFlags);
            return MDNode.FromHandle<DIBasicType>(handle)!;
        }

        /// <summary>Creates a pointer type with debug information</summary>
        /// <param name="pointeeType">base type of the pointer (<see langword="null"/> => void)</param>
        /// <param name="name">Name of the type</param>
        /// <param name="bitSize">Bit size of the type</param>
        /// <param name="bitAlign">But alignment of the type</param>
        /// <param name="addressSpace">Address space for the pointer</param>
        /// <returns>Pointer type</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DIDerivedType CreatePointerType(DIType? pointeeType, string? name, ulong bitSize, uint bitAlign = 0, uint addressSpace = 0)
        {
            var handle = this.BuilderHandle.CreatePointerType(
                pointeeType?.MetadataHandle ?? default,
                bitSize,
                bitAlign,
                addressSpace,
                name);

            return MDNode.FromHandle<DIDerivedType>(handle)!;
        }

        /// <summary>Creates a qualified type</summary>
        /// <param name="baseType">Base type to add the qualifier to</param>
        /// <param name="tag">Qualifier to apply</param>
        /// <returns>Qualified type</returns>
        /// <exception cref="ArgumentException"><paramref name="tag"/> is <see cref="QualifiedTypeTag.None"/></exception>
        /// <exception cref="ArgumentNullException"><paramref name="baseType"/> is <see langword="null"/></exception>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DIDerivedType CreateQualifiedType(DIType? baseType, QualifiedTypeTag tag)
        {
            var handle = this.BuilderHandle.CreateQualifiedType((uint)tag, baseType?.MetadataHandle ?? default);
            return MDNode.FromHandle<DIDerivedType>(handle)!;
        }

        /// <summary>Create a debug metadata array of debug types</summary>
        /// <param name="types">Types to include in the array</param>
        /// <returns>Array containing the types</returns>
        public DITypeArray CreateTypeArray(params DIType?[] types) => this.CreateTypeArray((IEnumerable<DIType?>)types);

        /// <summary>Create a debug metadata array of debug types</summary>
        /// <param name="types">Types to include in the array</param>
        /// <returns>Array containing the types</returns>
        public DITypeArray CreateTypeArray(IEnumerable<DIType?> types)
        {
            var handles = types.Select(t => t?.MetadataHandle ?? default).ToArray();
            var handle = this.BuilderHandle.GetOrCreateTypeArray(handles, handles.LongLength);
            return new DITypeArray(MDNode.FromHandle<MDTuple>(handle));
        }

        /// <summary>Creates a <see cref="DISubroutineType"/> to provide debug information for a function/procedure signature</summary>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this signature</param>
        /// <param name="types">Parameter types</param>
        /// <returns><see cref="DISubroutineType"/></returns>
        public DISubroutineType CreateSubroutineType(DebugInfoFlags debugFlags, params DIType?[] types)
        {
            return this.CreateSubroutineType(debugFlags, (IEnumerable<DIType?>)types);
        }

        /// <summary>Creates a <see cref="DISubroutineType"/> to provide debug information for a function/procedure signature</summary>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this signature</param>
        /// <param name="types">Parameter types</param>
        /// <returns><see cref="DISubroutineType"/></returns>
        public DISubroutineType CreateSubroutineType(DebugInfoFlags debugFlags, IEnumerable<DIType?> types)
        {
            var handles = types.Select(t => t?.MetadataHandle ?? default).ToArray();
            var handle = this.BuilderHandle.CreateSubroutineType(default, handles, (LLVMDIFlags)debugFlags);
            return MDNode.FromHandle<DISubroutineType>(handle)!;
        }

        /// <summary>Creates a <see cref="DISubroutineType"/> to provide debug information for a function/procedure signature</summary>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this signature</param>
        /// <returns><see cref="DISubroutineType"/></returns>
        public DISubroutineType CreateSubroutineType(DebugInfoFlags debugFlags)
        {
            return this.CreateSubroutineType(debugFlags, Array.Empty<DIType>());
        }

        /// <summary>Creates a <see cref="DISubroutineType"/> to provide debug information for a function/procedure signature</summary>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this signature</param>
        /// <param name="returnType">Return type of the signature</param>
        /// <param name="types">Parameters for the function</param>
        /// <returns><see cref="DISubroutineType"/></returns>
        public DISubroutineType CreateSubroutineType(DebugInfoFlags debugFlags, DIType? returnType, IEnumerable<DIType?> types)
        {
            return this.CreateSubroutineType(debugFlags, returnType != null ? types.Prepend(returnType) : types);
        }

        /// <summary>Creates debug description of a structure type</summary>
        /// <param name="scope">Scope containing the structure</param>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="file">File containing the type</param>
        /// <param name="line">Line of the start of the type</param>
        /// <param name="bitSize">Size of the type in bits</param>
        /// <param name="bitAlign">Bit alignment of the type</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for the structure</param>
        /// <param name="derivedFrom"><see cref="DIType"/> this type is derived from, if any</param>
        /// <param name="elements">Node array describing the elements of the structure</param>
        /// <returns><see cref="DICompositeType"/></returns>
        public DICompositeType CreateStructType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            ulong bitSize,
            uint bitAlign,
            DebugInfoFlags debugFlags,
            DIType? derivedFrom,
            params DINode[] elements)
        {
            return this.CreateStructType(scope, name, file, line, bitSize, bitAlign, debugFlags, derivedFrom, (IEnumerable<DINode>)elements);
        }

        /// <summary>Creates debug description of a structure type</summary>
        /// <param name="scope">Scope containing the structure</param>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="file">File containing the type</param>
        /// <param name="line">Line of the start of the type</param>
        /// <param name="bitSize">Size of the type in bits</param>
        /// <param name="bitAlign">Bit alignment of the type</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for the structure</param>
        /// <param name="derivedFrom"><see cref="DIType"/> this type is derived from, if any</param>
        /// <param name="elements">Node array describing the elements of the structure</param>
        /// <param name="runTimeLang">runtime language for the type</param>
        /// <param name="vTableHolder">VTable holder for the type</param>
        /// <param name="uniqueId">Unique ID for the type</param>
        /// <returns><see cref="DICompositeType"/></returns>
        public DICompositeType CreateStructType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            ulong bitSize,
            uint bitAlign,
            DebugInfoFlags debugFlags,
            DIType? derivedFrom,
            IEnumerable<DINode> elements,
            uint runTimeLang = 0,
            DIType? vTableHolder = null,
            string uniqueId = "")
        {
            var elementHandles = elements.Select(e => e.MetadataHandle).ToArray();
            var handle = this.BuilderHandle.CreateStructType(
                scope?.MetadataHandle ?? default,
                name,
                file?.MetadataHandle ?? default,
                line,
                bitSize,
                bitAlign,
                (LLVMDIFlags)debugFlags,
                derivedFrom?.MetadataHandle ?? default,
                elementHandles,
                (uint)elementHandles.Length,
                runTimeLang,
                vTableHolder?.MetadataHandle ?? default,
                uniqueId ?? string.Empty);

            return MDNode.FromHandle<DICompositeType>(handle)!;
        }

        /// <summary>Creates debug description of a union type</summary>
        /// <param name="scope">Scope containing the union</param>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="file">File containing the union</param>
        /// <param name="line">Line of the start of the union</param>
        /// <param name="bitSize">Size of the union in bits</param>
        /// <param name="bitAlign">Bit alignment of the union</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for the union</param>
        /// <param name="elements">Node array describing the elements of the union</param>
        /// <returns><see cref="DICompositeType"/></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DICompositeType CreateUnionType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            ulong bitSize,
            uint bitAlign,
            DebugInfoFlags debugFlags,
            DINodeArray elements)
        {
            return this.CreateUnionType(scope, name, file, line, bitSize, bitAlign, debugFlags, (IEnumerable<DINode>)elements);
        }

        /// <summary>Creates debug description of a union type</summary>
        /// <param name="scope">Scope containing the union</param>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="file">File containing the union</param>
        /// <param name="line">Line of the start of the union</param>
        /// <param name="bitSize">Size of the union in bits</param>
        /// <param name="bitAlign">Bit alignment of the union</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for the union</param>
        /// <param name="elements">Node array describing the elements of the union</param>
        /// <returns><see cref="DICompositeType"/></returns>
        public DICompositeType CreateUnionType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            ulong bitSize,
            uint bitAlign,
            DebugInfoFlags debugFlags,
            params DINode[] elements)
        {
            return this.CreateUnionType(scope, name, file, line, bitSize, bitAlign, debugFlags, (IEnumerable<DINode>)elements);
        }

        /// <summary>Creates debug description of a union type</summary>
        /// <param name="scope">Scope containing the union</param>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="file">File containing the union</param>
        /// <param name="line">Line of the start of the union</param>
        /// <param name="bitSize">Size of the union in bits</param>
        /// <param name="bitAlign">Bit alignment of the union</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for the union</param>
        /// <param name="elements">Node array describing the elements of the union</param>
        /// <param name="runTimeLang">Objective-C runtime version [Default=0]</param>
        /// <param name="uniqueId">A unique identifier for the type</param>
        /// <returns><see cref="DICompositeType"/></returns>
        public DICompositeType CreateUnionType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            ulong bitSize,
            uint bitAlign,
            DebugInfoFlags debugFlags,
            IEnumerable<DINode> elements,
            uint runTimeLang = 0,
            string uniqueId = "")
        {
            var elementHandles = elements.Select(e => e.MetadataHandle).ToArray();
            var handle = this.BuilderHandle.CreateUnionType(
                                                      scope?.MetadataHandle ?? default,
                                                      name,
                                                      file?.MetadataHandle ?? default,
                                                      line,
                                                      bitSize,
                                                      bitAlign,
                                                      (LLVMDIFlags)debugFlags,
                                                      elementHandles,
                                                      (uint)elementHandles.Length,
                                                      runTimeLang,
                                                      uniqueId ?? string.Empty);

            return MDNode.FromHandle<DICompositeType>(handle)!;
        }

        /// <summary>Creates a <see cref="DIDerivedType"/> for a member of a type</summary>
        /// <param name="scope">Scope containing the member type</param>
        /// <param name="name">Name of the member type</param>
        /// <param name="file">File containing the member type</param>
        /// <param name="line">Line of the start of the member type</param>
        /// <param name="bitSize">Size of the member type in bits</param>
        /// <param name="bitAlign">Bit alignment of the member</param>
        /// <param name="bitOffset">Bit offset of the member</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for the type</param>
        /// <param name="type">LLVM native type for the member type</param>
        /// <returns><see cref="DICompositeType"/></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DIDerivedType CreateMemberType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint line,
            ulong bitSize,
            uint bitAlign,
            ulong bitOffset,
            DebugInfoFlags debugFlags,
            DIType? type)
        {
            var handle = this.BuilderHandle.CreateMemberType(
                                                        scope?.MetadataHandle ?? default,
                                                        name,
                                                        file?.MetadataHandle ?? default,
                                                        line,
                                                        bitSize,
                                                        bitAlign,
                                                        bitOffset,
                                                        (LLVMDIFlags)debugFlags,
                                                        type?.MetadataHandle ?? default);
            return MDNode.FromHandle<DIDerivedType>(handle)!;
        }

        /// <summary>Creates debug information for an array type</summary>
        /// <param name="bitSize">Size, in bits for the type</param>
        /// <param name="bitAlign">Alignment in bits for the type</param>
        /// <param name="elementType">Type of elements in the array</param>
        /// <param name="subscripts">Dimensions for the array</param>
        /// <returns><see cref="DICompositeType"/> for the array</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DICompositeType CreateArrayType(ulong bitSize, uint bitAlign, DIType elementType, DINodeArray subscripts)
        {
            return this.CreateArrayType(bitSize, bitAlign, elementType, (IEnumerable<DINode>)subscripts);
        }

        /// <summary>Creates debug information for an array type</summary>
        /// <param name="bitSize">Size, in bits for the type</param>
        /// <param name="bitAlign">Alignment in bits for the type</param>
        /// <param name="elementType">Type of elements in the array</param>
        /// <param name="subscripts">Dimensions for the array</param>
        /// <returns><see cref="DICompositeType"/> for the array</returns>
        public DICompositeType CreateArrayType(ulong bitSize, uint bitAlign, DIType elementType, params DINode[] subscripts)
        {
            return this.CreateArrayType(bitSize, bitAlign, elementType, (IEnumerable<DINode>)subscripts);
        }

        /// <summary>Creates debug information for an array type</summary>
        /// <param name="bitSize">Size, in bits for the type</param>
        /// <param name="bitAlign">Alignment in bits for the type</param>
        /// <param name="elementType">Type of elements in the array</param>
        /// <param name="subscripts">Dimensions for the array</param>
        /// <returns><see cref="DICompositeType"/> for the array</returns>
        public DICompositeType CreateArrayType(ulong bitSize, uint bitAlign, DIType elementType, IEnumerable<DINode> subscripts)
        {
            var subScriptHandles = subscripts.Select(s => s.MetadataHandle).ToArray();
            var handle = this.BuilderHandle.CreateArrayType(bitSize, bitAlign, elementType.MetadataHandle, subScriptHandles, (uint)subScriptHandles.Length);
            return MDNode.FromHandle<DICompositeType>(handle)!;
        }

        /// <summary>Creates debug information for a vector type</summary>
        /// <param name="bitSize">Size, in bits for the type</param>
        /// <param name="bitAlign">Alignment in bits for the type</param>
        /// <param name="elementType">Type of elements in the Vector</param>
        /// <param name="subscripts">Dimensions for the Vector</param>
        /// <returns><see cref="DICompositeType"/> for the Vector</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DICompositeType CreateVectorType(ulong bitSize, uint bitAlign, DIType elementType, DINodeArray subscripts)
        {
            return this.CreateVectorType(bitSize, bitAlign, elementType, (IEnumerable<DINode>)subscripts);
        }

        /// <summary>Creates debug information for a vector type</summary>
        /// <param name="bitSize">Size, in bits for the type</param>
        /// <param name="bitAlign">Alignment in bits for the type</param>
        /// <param name="elementType">Type of elements in the Vector</param>
        /// <param name="subscripts">Dimensions for the Vector</param>
        /// <returns><see cref="DICompositeType"/> for the Vector</returns>
        public DICompositeType CreateVectorType(ulong bitSize, uint bitAlign, DIType elementType, params DINode[] subscripts)
        {
            return this.CreateVectorType(bitSize, bitAlign, elementType, (IEnumerable<DINode>)subscripts);
        }

        /// <summary>Creates debug information for a vector type</summary>
        /// <param name="bitSize">Size, in bits for the type</param>
        /// <param name="bitAlign">Alignment in bits for the type</param>
        /// <param name="elementType">Type of elements in the Vector</param>
        /// <param name="subscripts">Dimensions for the Vector</param>
        /// <returns><see cref="DICompositeType"/> for the Vector</returns>
        public DICompositeType CreateVectorType(ulong bitSize, uint bitAlign, DIType elementType, IEnumerable<DINode> subscripts)
        {
            var subScriptHandles = subscripts.Select(s => s.MetadataHandle).ToArray();
            var handle = this.BuilderHandle.CreateVectorType(bitSize, bitAlign, elementType.MetadataHandle, subScriptHandles, (uint)subScriptHandles.Length);
            return MDNode.FromHandle<DICompositeType>(handle)!;
        }

        /// <summary>Creates debug information for a type definition (e.g. type alias)</summary>
        /// <param name="type">Debug information for the aliased type</param>
        /// <param name="name">Name of the alias</param>
        /// <param name="file">File for the declaration of the typedef</param>
        /// <param name="line">line for the typedef</param>
        /// <param name="context">Context for creating the typedef</param>
        /// <param name="alignInBits">Bit alignment for the type</param>
        /// <returns><see cref="DIDerivedType"/>for the alias</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DIDerivedType CreateTypedef(DIType? type, string name, DIFile? file, uint line, DINode? context, uint alignInBits)
        {
            var handle = this.BuilderHandle.CreateTypedef(
                type?.MetadataHandle ?? default,
                name,
                file?.MetadataHandle ?? default,
                line,
                context?.MetadataHandle ?? default,
                alignInBits);

            return MDNode.FromHandle<DIDerivedType>(handle)!;
        }

        /// <summary>Creates a new <see cref="DISubRange"/></summary>
        /// <param name="lowerBound">Lower bounds of the <see cref="DISubRange"/></param>
        /// <param name="count">Count of elements in the sub range</param>
        /// <returns><see cref="DISubRange"/></returns>
        public DISubRange CreateSubRange(long lowerBound, long count)
        {
            var handle = this.BuilderHandle.GetOrCreateSubrange(lowerBound, count);
            return MDNode.FromHandle<DISubRange>(handle)!;
        }

        /// <summary>Gets or creates a node array with the specified elements</summary>
        /// <param name="elements">Elements of the array</param>
        /// <returns><see cref="DINodeArray"/></returns>
        /// <remarks>
        /// <note type="Note">
        /// As of LLVM 8.0 there's not much reason to manually construct a <see cref="DINodeArray"/>
        /// since use as an "in" parameter were superseded by overloads taking an actual array.
        /// </note>
        /// </remarks>
        public DINodeArray GetOrCreateArray(IEnumerable<DINode> elements)
        {
            var buf = elements.Select(d => d?.MetadataHandle ?? default).ToArray();
            long actualLen = buf.LongLength;

            var handle = this.BuilderHandle.GetOrCreateArray(buf, buf.LongLength);
            if (handle == default)
            {
                throw new InternalCodeGeneratorException("Got a null MDTuple from LLVMDIBuilderGetOrCreateArray");
            }

            // assume wrapped tuple is not null since underlying handle is already checked.
            var tuple = LlvmMetadata.FromHandle<MDTuple>(this.OwningModule.Context, handle)!;
            return new DINodeArray(tuple);
        }

        /// <summary>Gets or creates a Type array with the specified types</summary>
        /// <param name="types">Types</param>
        /// <returns><see cref="DITypeArray"/></returns>
        public DITypeArray GetOrCreateTypeArray(params DIType[] types) => this.GetOrCreateTypeArray((IEnumerable<DIType>)types);

        /// <summary>Gets or creates a Type array with the specified types</summary>
        /// <param name="types">Types</param>
        /// <returns><see cref="DITypeArray"/></returns>
        public DITypeArray GetOrCreateTypeArray(IEnumerable<DIType> types)
        {
            var buf = types.Select(t => t?.MetadataHandle ?? default).ToArray();
            var handle = this.BuilderHandle.GetOrCreateTypeArray(buf, buf.LongLength);
            return new DITypeArray(MDNode.FromHandle<MDTuple>(handle));
        }

        /// <summary>Creates a value for an enumeration</summary>
        /// <param name="name">Name of the value</param>
        /// <param name="value">Value of the enumerated value</param>
        /// <param name="isUnsigned">Indicates if the value is unsigned [Default: false]</param>
        /// <returns><see cref="DIEnumerator"/> for the name, value pair</returns>
        public DIEnumerator CreateEnumeratorValue(string name, long value, bool isUnsigned = false)
        {
            var handle = this.BuilderHandle.CreateEnumerator(name, value, isUnsigned);
            return MDNode.FromHandle<DIEnumerator>(handle)!;
        }

        /// <summary>Creates an enumeration type</summary>
        /// <param name="scope">Containing scope for the type</param>
        /// <param name="name">source language name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="file">Source file containing the type</param>
        /// <param name="lineNumber">Source file line number for the type</param>
        /// <param name="sizeInBits">Size, in bits, for the type</param>
        /// <param name="alignInBits">Alignment, in bits for the type</param>
        /// <param name="elements"><see cref="DIEnumerator"/> elements for the type</param>
        /// <param name="underlyingType">Underlying type for the enumerated type</param>
        /// <returns><see cref="DICompositeType"/> for the enumerated type</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DICompositeType CreateEnumerationType(
            DIScope? scope,
            string name,
            DIFile? file,
            uint lineNumber,
            ulong sizeInBits,
            uint alignInBits,
            IEnumerable<DIEnumerator> elements,
            DIType? underlyingType)
        {
            var elementHandles = elements.Select(e => e.MetadataHandle).ToArray();
            var handle = this.BuilderHandle.CreateEnumerationType(
                                                             scope?.MetadataHandle ?? default,
                                                             name,
                                                             file?.MetadataHandle ?? default,
                                                             lineNumber,
                                                             sizeInBits,
                                                             alignInBits,
                                                             elementHandles,
                                                             checked((uint)elementHandles.Length),
                                                             underlyingType?.MetadataHandle ?? default);

            return MDNode.FromHandle<DICompositeType>(handle)!;
        }

        /// <summary>Creates a new <see cref="DIGlobalVariableExpression"/></summary>
        /// <param name="scope">Scope for the expression</param>
        /// <param name="name">Source language name of the expression</param>
        /// <param name="linkageName">Linkage name of the expression</param>
        /// <param name="file">Source file for the expression</param>
        /// <param name="lineNo">Source Line number for the expression</param>
        /// <param name="type"><see cref="DIType"/> of the expression</param>
        /// <param name="isLocalToUnit">Flag to indicate if this is local to the compilation unit (e.g. static in C)</param>
        /// <param name="value"><see cref="Value"/> for the variable</param>
        /// <param name="declaration"><see cref="DINode"/> for the declaration of the variable</param>
        /// <param name="bitAlign">Bit alignment for the expression</param>
        /// <returns><see cref="DIGlobalVariableExpression"/> from the parameters</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DIGlobalVariableExpression CreateGlobalVariableExpression(
            DINode? scope,
            string name,
            string linkageName,
            DIFile? file,
            uint lineNo,
            DIType? type,
            bool isLocalToUnit,
            DIExpression? value,
            DINode? declaration = null,
            uint bitAlign = 0)
        {
            if (string.IsNullOrWhiteSpace(linkageName))
            {
                linkageName = name;
            }

            var handle = this.BuilderHandle.CreateGlobalVariableExpression(
                                                                      scope?.MetadataHandle ?? default,
                                                                      name,
                                                                      linkageName,
                                                                      file?.MetadataHandle ?? default,
                                                                      lineNo,
                                                                      type?.MetadataHandle ?? default,
                                                                      isLocalToUnit,
                                                                      value?.MetadataHandle ?? default,
                                                                      declaration?.MetadataHandle ?? default,
                                                                      bitAlign);
            return MDNode.FromHandle<DIGlobalVariableExpression>(handle)!;
        }

        /// <summary>Finalizes debug information for all items built by this builder</summary>
        /// <remarks>
        /// <note type="note">
        ///  The term "finalize" here is in the context of LLVM rather than the .NET concept of Finalization.
        ///  In particular this will trigger resolving temporaries and will complete the list of locals for
        ///  any functions. So, the only nodes allowed after this is called are those that are fully resolved.
        /// </note>
        /// </remarks>
        public void Finish()
        {
            if (this.isFinished)
            {
                return;
            }

            this.BuilderHandle.DIBuilderFinalize();
            this.isFinished = true;
        }

        /// <summary>Inserts an llvm.dbg.declare instruction before the given instruction</summary>
        /// <param name="storage">Value the declaration is bound to</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> for <paramref name="storage"/></param>
        /// <param name="location"><see cref="DILocation"/>for the variable</param>
        /// <param name="insertBefore"><see cref="Instruction"/> to insert the declaration before</param>
        /// <returns><see cref="CallInstruction"/> for the call to llvm.dbg.declare</returns>
        /// <remarks>
        /// This adds a call to the <see href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">llvm.dbg.declare</see> intrinsic.
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">LLVM: llvm.dbg.declare</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        public CallInstruction InsertDeclare(Value storage, DILocalVariable varInfo, DILocation location, Instruction insertBefore)
        {
            return this.InsertDeclare(storage, varInfo, this.CreateExpression(), location, insertBefore);
        }

        /// <summary>Inserts an llvm.dbg.declare instruction before the given instruction</summary>
        /// <param name="storage">Value the declaration is bound to</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> for <paramref name="storage"/></param>
        /// <param name="expression"><see cref="DIExpression"/> for a debugger to use when extracting the value</param>
        /// <param name="location"><see cref="DILocation"/>for the variable</param>
        /// <param name="insertBefore"><see cref="Instruction"/> to insert the declaration before</param>
        /// <returns><see cref="CallInstruction"/> for the call to llvm.dbg.declare</returns>
        /// <remarks>
        /// This adds a call to the <see href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">llvm.dbg.declare</see> intrinsic.
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">LLVM: llvm.dbg.declare</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public CallInstruction InsertDeclare(Value storage, DILocalVariable varInfo, DIExpression expression, DILocation location, Instruction insertBefore)
        {
            var handle = this.BuilderHandle.InsertDeclareBefore(
                                                           storage.ValueHandle,
                                                           varInfo.MetadataHandle,
                                                           expression.MetadataHandle,
                                                           location.MetadataHandle,
                                                           insertBefore.ValueHandle);

            return Value.FromHandle<CallInstruction>(handle)!;
        }

        /// <summary>Inserts an llvm.dbg.declare instruction before the given instruction</summary>
        /// <param name="storage">Value the declaration is bound to</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> for <paramref name="storage"/></param>
        /// <param name="location"><see cref="DILocation"/>for the variable</param>
        /// <param name="insertAtEnd"><see cref="BasicBlock"/> to insert the declaration at the end of</param>
        /// <returns><see cref="CallInstruction"/> for the call to llvm.dbg.declare</returns>
        /// <remarks>
        /// This adds a call to the <see href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">llvm.dbg.declare</see> intrinsic.
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">LLVM: llvm.dbg.declare</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        public CallInstruction InsertDeclare(Value storage, DILocalVariable varInfo, DILocation location, BasicBlock insertAtEnd)
        {
            return this.InsertDeclare(storage, varInfo, this.CreateExpression(), location, insertAtEnd);
        }

        /// <summary>Inserts an llvm.dbg.declare instruction before the given instruction</summary>
        /// <param name="storage">Value the declaration is bound to</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> for <paramref name="storage"/></param>
        /// <param name="expression"><see cref="DIExpression"/> for a debugger to use when extracting the value</param>
        /// <param name="location"><see cref="DILocation"/>for the variable</param>
        /// <param name="insertAtEnd"><see cref="BasicBlock"/> to insert the declaration at the end of</param>
        /// <returns><see cref="CallInstruction"/> for the call to llvm.dbg.declare</returns>
        /// <remarks>
        /// This adds a call to the <see href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">llvm.dbg.declare</see> intrinsic.
        /// <note type="note">
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </note>
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-declare">LLVM: llvm.dbg.declare</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public CallInstruction InsertDeclare(Value storage, DILocalVariable varInfo, DIExpression expression, DILocation location, BasicBlock insertAtEnd)
        {
            if (location.Scope.SubProgram != varInfo.Scope.SubProgram)
            {
                throw new ArgumentException();
            }

            var handle = this.BuilderHandle.InsertDeclareAtEnd(
                                                          storage.ValueHandle,
                                                          varInfo.MetadataHandle,
                                                          expression.MetadataHandle,
                                                          location.MetadataHandle,
                                                          insertAtEnd.BlockHandle);
            return Value.FromHandle<CallInstruction>(handle)!;
        }

        /// <summary>Inserts a call to the llvm.dbg.value intrinsic before the specified instruction</summary>
        /// <param name="value">New value</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> describing the variable</param>
        /// <param name="location"><see cref="DILocation"/>for the assignment</param>
        /// <param name="insertBefore">Location to insert the intrinsic</param>
        /// <returns><see cref="CallInstruction"/> for the intrinsic</returns>
        /// <remarks>
        /// This intrinsic provides information when a user source variable is set to a new value.
        /// <note type="note">
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </note>
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-value">LLVM: llvm.dbg.value</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        public CallInstruction InsertValue(
            Value value,
            DILocalVariable varInfo,
            DILocation location,
            Instruction insertBefore)
        {
            return this.InsertValue(value, varInfo, null, location, insertBefore);
        }

        /// <summary>Inserts a call to the llvm.dbg.value intrinsic before the specified instruction</summary>
        /// <param name="value">New value</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> describing the variable</param>
        /// <param name="expression"><see cref="DIExpression"/> for the variable</param>
        /// <param name="location"><see cref="DILocation"/>for the assignment</param>
        /// <param name="insertBefore">Location to insert the intrinsic</param>
        /// <returns><see cref="CallInstruction"/> for the intrinsic</returns>
        /// <remarks>
        /// This intrinsic provides information when a user source variable is set to a new value.
        /// <note type="note">
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </note>
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-value">LLVM: llvm.dbg.value</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Interop API requires specific derived type")]
        public CallInstruction InsertValue(
            Value value,
            DILocalVariable varInfo,
            DIExpression? expression,
            DILocation location,
            Instruction insertBefore)
        {
            var handle = this.BuilderHandle.InsertDbgValueBefore(
                                                            value.ValueHandle,
                                                            varInfo.MetadataHandle,
                                                            expression?.MetadataHandle ?? this.CreateExpression().MetadataHandle,
                                                            location.MetadataHandle,
                                                            insertBefore.ValueHandle);
            var retVal = Value.FromHandle<CallInstruction>(handle)!;
            retVal.IsTailCall = true;
            return retVal;
        }

        /// <summary>Inserts a call to the llvm.dbg.value intrinsic at the end of a basic block</summary>
        /// <param name="value">New value</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> describing the variable</param>
        /// <param name="location"><see cref="DILocation"/>for the assignment</param>
        /// <param name="insertAtEnd">Block to append the intrinsic to the end of</param>
        /// <returns><see cref="CallInstruction"/> for the intrinsic</returns>
        /// <remarks>
        /// This intrinsic provides information when a user source variable is set to a new value.
        /// <note type="note">
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </note>
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-value">LLVM: llvm.dbg.value</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        public CallInstruction InsertValue(
            Value value,
            DILocalVariable varInfo,
            DILocation location,
            BasicBlock insertAtEnd)
        {
            return this.InsertValue(value, varInfo, null, location, insertAtEnd);
        }

        /// <summary>Inserts a call to the llvm.dbg.value intrinsic at the end of a basic block</summary>
        /// <param name="value">New value</param>
        /// <param name="varInfo"><see cref="DILocalVariable"/> describing the variable</param>
        /// <param name="expression"><see cref="DIExpression"/> for the variable</param>
        /// <param name="location"><see cref="DILocation"/>for the assignment</param>
        /// <param name="insertAtEnd">Block to append the intrinsic to the end of</param>
        /// <returns><see cref="CallInstruction"/> for the intrinsic</returns>
        /// <remarks>
        /// This intrinsic provides information when a user source variable is set to a new value.
        /// <note type="note">
        /// The call has no impact on the actual machine code generated, as it is removed or ignored for actual target instruction
        /// selection. Instead, this provides a means to bind the LLVM Debug information metadata to a particular LLVM <see cref="Value"/>
        /// that allows the transformation and optimization passes to track the debug information. Thus, even with optimized code
        /// the actual debug information is retained.
        /// </note>
        /// </remarks>
        /// <seealso href="xref:llvm_sourcelevel_debugging#lvm-dbg-value">LLVM: llvm.dbg.value</seealso>
        /// <seealso href="xref:llvm_sourcelevel_debugging#source-level-debugging-with-llvm">LLVM: Source Level Debugging with LLVM</seealso>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Interop API requires specific derived type")]
        public CallInstruction InsertValue(
            Value value,
            DILocalVariable varInfo,
            DIExpression? expression,
            DILocation location,
            BasicBlock insertAtEnd)
        {
            if (location.Scope != varInfo.Scope)
            {
                throw new ArgumentException();
            }

            if (insertAtEnd.ContainingFunction is null)
            {
                throw new ArgumentException();
            }

            var handle = this.BuilderHandle.InsertDbgValueAtEnd(
                                                           value.ValueHandle,
                                                           varInfo.MetadataHandle,
                                                           expression?.MetadataHandle ?? this.CreateExpression().MetadataHandle,
                                                           location.MetadataHandle,
                                                           insertAtEnd.BlockHandle);

            var retVal = Value.FromHandle<CallInstruction>(handle)!;
            retVal.IsTailCall = true;
            return retVal;
        }

        /// <summary>Creates a <see cref="DIExpression"/> from the provided <see cref="ExpressionOp"/>s</summary>
        /// <param name="operations">Operation sequence for the expression</param>
        /// <returns><see cref="DIExpression"/></returns>
        public DIExpression CreateExpression(params ExpressionOp[] operations)
            => this.CreateExpression((IEnumerable<ExpressionOp>)operations);

        /// <summary>Creates a <see cref="DIExpression"/> from the provided <see cref="ExpressionOp"/>s</summary>
        /// <param name="operations">Operation sequence for the expression</param>
        /// <returns><see cref="DIExpression"/></returns>
        public DIExpression CreateExpression(IEnumerable<ExpressionOp> operations)
        {
            long[] args = operations.Cast<long>().ToArray();
            var handle = this.BuilderHandle.CreateExpression(args, args.LongLength);
            return MDNode.FromHandle<DIExpression>(handle)!;
        }

        /// <summary>Creates a <see cref="DIExpression"/> for a constant value</summary>
        /// <param name="value">Value of the expression</param>
        /// <returns><see cref="DIExpression"/></returns>
        public DIExpression CreateConstantValueExpression(long value)
        {
            LLVMMetadataRef handle = this.BuilderHandle.CreateConstantValueExpression(value);
            return MDNode.FromHandle<DIExpression>(handle)!;
        }

        /// <summary>Creates a replaceable composite type</summary>
        /// <param name="tag">Debug information <see cref="Tag"/> for the composite type (only values for a composite type are allowed)</param>
        /// <param name="name">Name of the type (use <see cref="string.Empty"/> for anonymous types)</param>
        /// <param name="scope">Scope of the type</param>
        /// <param name="file">Source file for the type</param>
        /// <param name="line">Source line for the type</param>
        /// <param name="lang">Source language the type is defined in</param>
        /// <param name="sizeInBits">size of the type in bits</param>
        /// <param name="alignBits">alignment of the type in bits</param>
        /// <param name="flags"><see cref="DebugInfoFlags"/> for the type</param>
        /// <param name="uniqueId">Unique identifier for the type</param>
        /// <returns><see cref="DICompositeType"/></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public DICompositeType CreateReplaceableCompositeType(
            Tag tag,
            string name,
            DIScope? scope,
            DIFile? file,
            uint line,
            uint lang = 0,
            ulong sizeInBits = 0,
            uint alignBits = 0,
            DebugInfoFlags flags = DebugInfoFlags.None,
            string uniqueId = "")
        {
            if (uniqueId == null)
            {
                uniqueId = string.Empty;
            }

            var handle = this.BuilderHandle.CreateReplaceableCompositeType(
                                                                      (uint)tag,
                                                                      name,
                                                                      scope?.MetadataHandle ?? default,
                                                                      file?.MetadataHandle ?? default,
                                                                      line,
                                                                      lang,
                                                                      sizeInBits,
                                                                      alignBits,
                                                                      (LLVMDIFlags)flags,
                                                                      uniqueId);
            return MDNode.FromHandle<DICompositeType>(handle)!;
        }

        internal DebugInfoBuilder(BitcodeModule owningModule)
            : this(owningModule, true)
        {
        }

        internal LLVMDIBuilderRef BuilderHandle { get; }

        // keeping this private for now as there doesn't seem to be a good reason to support
        // allowUnresolved == false
        private DebugInfoBuilder(BitcodeModule owningModule, bool allowUnresolved)
        {
            this.BuilderHandle = allowUnresolved
                ? owningModule.ModuleHandle.CreateDIBuilder()
                : owningModule.ModuleHandle.CreateDIBuilderDisallowUnresolved();

            this.OwningModule = owningModule;
        }

        private bool isFinished;
    }
}
