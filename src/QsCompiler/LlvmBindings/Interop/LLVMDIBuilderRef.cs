// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMDIBuilderRef : IEquatable<LLVMDIBuilderRef>
    {
        public IntPtr Handle;

        public LLVMDIBuilderRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMDIBuilderRef(LLVMOpaqueDIBuilder* value) => new LLVMDIBuilderRef((IntPtr)value);

        public static implicit operator LLVMOpaqueDIBuilder*(LLVMDIBuilderRef value) => (LLVMOpaqueDIBuilder*)value.Handle;

        public static bool operator ==(LLVMDIBuilderRef left, LLVMDIBuilderRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMDIBuilderRef left, LLVMDIBuilderRef right) => !(left == right);

        public LLVMMetadataRef CreateCompileUnit(
            LLVMDWARFSourceLanguage sourceLanguage,
            LLVMMetadataRef fileMetadata,
            string producer,
            int isOptimized,
            string flags,
            uint runtimeVersion,
            string splitName,
            LLVMDWARFEmissionKind dwarfEmissionKind,
            uint dWOld,
            int splitDebugInlining,
            int debugInfoForProfiling,
            string sysRoot,
            string sDK) => this.CreateCompileUnit(
                sourceLanguage,
                fileMetadata,
                producer.AsSpan(),
                isOptimized,
                flags.AsSpan(),
                runtimeVersion,
                splitName.AsSpan(),
                dwarfEmissionKind,
                dWOld,
                splitDebugInlining,
                debugInfoForProfiling,
                sysRoot.AsSpan(),
                sDK.AsSpan());

        public LLVMMetadataRef CreateCompileUnit(
            LLVMDWARFSourceLanguage sourceLanguage,
            LLVMMetadataRef fileMetadata,
            ReadOnlySpan<char> producer,
            int isOptimized,
            ReadOnlySpan<char> flags,
            uint runtimeVersion,
            ReadOnlySpan<char> splitName,
            LLVMDWARFEmissionKind dwarfEmissionKind,
            uint dWOld,
            int splitDebugInlining,
            int debugInfoForProfiling,
            ReadOnlySpan<char> sysRoot,
            ReadOnlySpan<char> sDK)
        {
            using var marshaledProducer = new MarshaledString(producer);
            using var marshaledFlags = new MarshaledString(flags);
            using var marshaledSplitNameFlags = new MarshaledString(splitName);
            using var marshaledSysRoot = new MarshaledString(sysRoot);
            using var marshaledSDK = new MarshaledString(sDK);

            return LLVM.DIBuilderCreateCompileUnit(
                this,
                sourceLanguage,
                fileMetadata,
                marshaledProducer,
                (UIntPtr)marshaledProducer.Length,
                isOptimized,
                marshaledFlags,
                (UIntPtr)marshaledFlags.Length,
                runtimeVersion,
                marshaledSplitNameFlags,
                (UIntPtr)marshaledSplitNameFlags.Length,
                dwarfEmissionKind,
                dWOld,
                splitDebugInlining,
                debugInfoForProfiling,
                marshaledSysRoot,
                (UIntPtr)marshaledSysRoot.Length,
                marshaledSDK,
                (UIntPtr)marshaledSDK.Length);
        }

        public LLVMMetadataRef CreateFile(string fullPath, string directory) => this.CreateFile(fullPath.AsSpan(), directory.AsSpan());

        public LLVMMetadataRef CreateFile(ReadOnlySpan<char> fullPath, ReadOnlySpan<char> directory)
        {
            using var marshaledFullPath = new MarshaledString(fullPath);
            using var marshaledDirectory = new MarshaledString(directory);
            return LLVM.DIBuilderCreateFile(this, marshaledFullPath, (UIntPtr)marshaledFullPath.Length, marshaledDirectory, (UIntPtr)marshaledDirectory.Length);
        }

        public LLVMMetadataRef CreateFunction(
            LLVMMetadataRef scope,
            string name,
            string linkageName,
            LLVMMetadataRef file,
            uint lineNo,
            LLVMMetadataRef type,
            int isLocalToUnit,
            int isDefinition,
            uint scopeLine,
            LLVMDIFlags flags,
            int isOptimized) => this.CreateFunction(scope, name.AsSpan(), linkageName.AsSpan(), file, lineNo, type, isLocalToUnit, isDefinition, scopeLine, flags, isOptimized);

        public LLVMMetadataRef CreateFunction(
            LLVMMetadataRef scope,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> linkageName,
            LLVMMetadataRef file,
            uint lineNo,
            LLVMMetadataRef type,
            int isLocalToUnit,
            int isDefinition,
            uint scopeLine,
            LLVMDIFlags flags,
            int isOptimized)
        {
            using var marshaledName = new MarshaledString(name);
            using var marshaledLinkageName = new MarshaledString(linkageName);
            var methodNameLength = (uint)marshaledName.Length;
            var linkageNameLength = (uint)marshaledLinkageName.Length;

            return LLVM.DIBuilderCreateFunction(
                this,
                scope,
                marshaledName,
                (UIntPtr)methodNameLength,
                marshaledLinkageName,
                (UIntPtr)linkageNameLength,
                file,
                lineNo,
                type,
                isLocalToUnit,
                isDefinition,
                scopeLine,
                flags,
                isOptimized);
        }

        public LLVMMetadataRef CreateMacro(LLVMMetadataRef parentMacroFile, uint line, LLVMDWARFMacinfoRecordType recordType, string name, string value) => this.CreateMacro(parentMacroFile, line, recordType, name.AsSpan(), value.AsSpan());

        public LLVMMetadataRef CreateMacro(LLVMMetadataRef parentMacroFile, uint line, LLVMDWARFMacinfoRecordType recordType, ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            using var marshaledName = new MarshaledString(name);
            using var marshaledValue = new MarshaledString(value);
            var nameLength = (uint)marshaledName.Length;
            var valueLength = (uint)marshaledValue.Length;

            return LLVM.DIBuilderCreateMacro(this, parentMacroFile, line, recordType, marshaledName, (UIntPtr)nameLength, marshaledValue, (UIntPtr)valueLength);
        }

        public LLVMMetadataRef CreateModule(LLVMMetadataRef parentScope, string name, string configMacros, string includePath, string sysRoot) => this.CreateModule(parentScope, name.AsSpan(), configMacros.AsSpan(), includePath.AsSpan(), sysRoot.AsSpan());

        public LLVMMetadataRef CreateModule(LLVMMetadataRef parentScope, ReadOnlySpan<char> name, ReadOnlySpan<char> configMacros, ReadOnlySpan<char> includePath, ReadOnlySpan<char> sysRoot)
        {
            using var marshaledName = new MarshaledString(name);
            using var marshaledConfigMacros = new MarshaledString(configMacros);
            using var marshaledIncludePath = new MarshaledString(includePath);
            using var marshaledSysRoot = new MarshaledString(sysRoot);
            var nameLength = (uint)marshaledName.Length;
            var configMacrosLength = (uint)marshaledConfigMacros.Length;
            var includePathLength = (uint)marshaledIncludePath.Length;
            var sysRootLength = (uint)marshaledSysRoot.Length;

            return LLVM.DIBuilderCreateModule(this, parentScope, marshaledName, (UIntPtr)nameLength, marshaledConfigMacros, (UIntPtr)configMacrosLength, marshaledIncludePath, (UIntPtr)includePathLength, marshaledSysRoot, (UIntPtr)sysRootLength);
        }

        public LLVMMetadataRef CreateSubroutineType(LLVMMetadataRef file, LLVMMetadataRef[] parameterTypes, LLVMDIFlags flags) => this.CreateSubroutineType(file, parameterTypes.AsSpan(), flags);

        public LLVMMetadataRef CreateSubroutineType(LLVMMetadataRef file, ReadOnlySpan<LLVMMetadataRef> parameterTypes, LLVMDIFlags flags)
        {
            fixed (LLVMMetadataRef* pParameterTypes = parameterTypes)
            {
                return LLVM.DIBuilderCreateSubroutineType(this, file, (LLVMOpaqueMetadata**)pParameterTypes, (uint)parameterTypes.Length, flags);
            }
        }

        public LLVMMetadataRef CreateTempMacroFile(LLVMMetadataRef parentMacroFile, uint line, LLVMMetadataRef file) => LLVM.DIBuilderCreateTempMacroFile(this, parentMacroFile, line, file);

        public LLVMMetadataRef CreateTypedef(LLVMMetadataRef type, string name, LLVMMetadataRef file, uint lineNo, LLVMMetadataRef scope, uint alignInBits) => this.CreateTypedef(type, name.AsSpan(), file, lineNo, scope, alignInBits);

        public LLVMMetadataRef CreateTypedef(LLVMMetadataRef type, ReadOnlySpan<char> name, LLVMMetadataRef file, uint lineNo, LLVMMetadataRef scope, uint alignInBits)
        {
            using var marshaledName = new MarshaledString(name);
            var nameLength = (uint)marshaledName.Length;

            return LLVM.DIBuilderCreateTypedef(this, type, marshaledName, (UIntPtr)nameLength, file, lineNo, scope, alignInBits);
        }

        public void DIBuilderFinalize() => LLVM.DIBuilderFinalize(this);

        public override bool Equals(object obj) => (obj is LLVMDIBuilderRef other) && this.Equals(other);

        public bool Equals(LLVMDIBuilderRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMDIBuilderRef)}: {this.Handle:X}";
    }
}
