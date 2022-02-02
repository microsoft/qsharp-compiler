// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.InteropServices;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMModuleRef : IDisposable, IEquatable<LLVMModuleRef>
    {
        public IntPtr Handle;

        public LLVMModuleRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public LLVMContextRef Context => (this.Handle != IntPtr.Zero) ? LLVM.GetModuleContext(this) : default;

        public string DataLayout
        {
            get
            {
                if (this.Handle == IntPtr.Zero)
                {
                    return string.Empty;
                }

                var pDataLayoutStr = LLVM.GetDataLayout(this);

                if (pDataLayoutStr == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pDataLayoutStr, int.MaxValue);
                return span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            set
            {
                using var marshaledDataLayoutStr = new MarshaledString(value.AsSpan());
                LLVM.SetDataLayout(this, marshaledDataLayoutStr);
            }
        }

        public LLVMValueRef FirstFunction => (this.Handle != IntPtr.Zero) ? LLVM.GetFirstFunction(this) : default;

        public LLVMValueRef FirstGlobal => (this.Handle != IntPtr.Zero) ? LLVM.GetFirstGlobal(this) : default;

        public LLVMValueRef LastFunction => (this.Handle != IntPtr.Zero) ? LLVM.GetLastFunction(this) : default;

        public LLVMValueRef LastGlobal => (this.Handle != IntPtr.Zero) ? LLVM.GetLastGlobal(this) : default;

        public string Target
        {
            get
            {
                if (this.Handle == IntPtr.Zero)
                {
                    return string.Empty;
                }

                var pTriple = LLVM.GetTarget(this);

                if (pTriple == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pTriple, int.MaxValue);
                return span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            set
            {
                using var marshaledTriple = new MarshaledString(value.AsSpan());
                LLVM.SetTarget(this, marshaledTriple);
            }
        }

        public static implicit operator LLVMModuleRef(LLVMOpaqueModule* value) => new LLVMModuleRef((IntPtr)value);

        public static implicit operator LLVMOpaqueModule*(LLVMModuleRef value) => (LLVMOpaqueModule*)value.Handle;

        public static bool operator ==(LLVMModuleRef left, LLVMModuleRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMModuleRef left, LLVMModuleRef right) => !(left == right);

        public static LLVMModuleRef CreateWithName(string moduleID) => CreateWithName(moduleID.AsSpan());

        public static LLVMModuleRef CreateWithName(ReadOnlySpan<char> moduleID)
        {
            using var marshaledModuleID = new MarshaledString(moduleID);
            return LLVM.ModuleCreateWithName(marshaledModuleID);
        }

        public LLVMValueRef AddAlias(LLVMTypeRef ty, LLVMValueRef aliasee, string name) => this.AddAlias(ty, aliasee, name.AsSpan());

        public LLVMValueRef AddAlias(LLVMTypeRef ty, LLVMValueRef aliasee, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.AddAlias(this, ty, aliasee, marshaledName);
        }

        public LLVMValueRef AddFunction(string name, LLVMTypeRef functionTy) => this.AddFunction(name.AsSpan(), functionTy);

        public LLVMValueRef AddFunction(ReadOnlySpan<char> name, LLVMTypeRef functionTy)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.AddFunction(this, marshaledName, functionTy);
        }

        public LLVMValueRef AddGlobal(LLVMTypeRef ty, string name) => this.AddGlobal(ty, name.AsSpan());

        public LLVMValueRef AddGlobal(LLVMTypeRef ty, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.AddGlobal(this, ty, marshaledName);
        }

        public LLVMValueRef AddGlobalInAddressSpace(LLVMTypeRef ty, string name, uint addressSpace) => this.AddGlobalInAddressSpace(ty, name.AsSpan(), addressSpace);

        public LLVMValueRef AddGlobalInAddressSpace(LLVMTypeRef ty, ReadOnlySpan<char> name, uint addressSpace)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.AddGlobalInAddressSpace(this, ty, marshaledName, addressSpace);
        }

        public void AddModuleFlag(string flagName, LLVMModuleFlagBehavior behavior, uint valAsUInt)
        {
            LLVMOpaqueValue* valAsValueRef = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, valAsUInt);
            this.AddModuleFlag(flagName, behavior, valAsValueRef);
        }

        public void AddModuleFlag(string flagName, LLVMModuleFlagBehavior behavior, LLVMValueRef valAsValueRef)
        {
            LLVMOpaqueMetadata* valAsMetadata = LLVM.ValueAsMetadata(valAsValueRef);
            this.AddModuleFlag(flagName, behavior, valAsMetadata);
        }

        public void AddModuleFlag(string flagName, LLVMModuleFlagBehavior behavior, LLVMMetadataRef valAsMetadataRef) => this.AddModuleFlag(flagName.AsSpan(), behavior, valAsMetadataRef);

        public void AddModuleFlag(ReadOnlySpan<char> flagName, LLVMModuleFlagBehavior behavior, LLVMMetadataRef valAsMetadataRef)
        {
            using var marshaledName = new MarshaledString(flagName);
            LLVM.AddModuleFlag(this, behavior, marshaledName, (UIntPtr)flagName.Length, valAsMetadataRef);
        }

        public void AddNamedMetadataOperand(string name, LLVMValueRef val) => this.AddNamedMetadataOperand(name.AsSpan(), val);

        public void AddNamedMetadataOperand(ReadOnlySpan<char> name, LLVMValueRef val)
        {
            using var marshaledName = new MarshaledString(name);
            LLVM.AddNamedMetadataOperand(this, marshaledName, val);
        }

        public LLVMDIBuilderRef CreateDIBuilder()
        {
            return new LLVMDIBuilderRef((IntPtr)LLVM.CreateDIBuilder(this));
        }

        public LLVMExecutionEngineRef CreateMCJITCompiler()
        {
            if (!this.TryCreateMCJITCompiler(out LLVMExecutionEngineRef jit, out string error))
            {
                throw new ExternalException(error);
            }

            return jit;
        }

        public LLVMExecutionEngineRef CreateMCJITCompiler(ref LLVMMCJITCompilerOptions options)
        {
            if (!this.TryCreateMCJITCompiler(out LLVMExecutionEngineRef jit, ref options, out string error))
            {
                throw new ExternalException(error);
            }

            return jit;
        }

        public LLVMModuleRef Clone() => LLVM.CloneModule(this);

        public LLVMModuleProviderRef CreateModuleProvider() => LLVM.CreateModuleProviderForExistingModule(this);

        public void AddNamedMetadataOperand(string name, LLVMMetadataRef compileUnitMetadata) => this.AddNamedMetadataOperand(name.AsSpan(), compileUnitMetadata);

        public void AddNamedMetadataOperand(ReadOnlySpan<char> name, LLVMMetadataRef compileUnitMetadata)
        {
            using var marshaledName = new MarshaledString(name);
            LLVM.AddNamedMetadataOperand(this, marshaledName, LLVM.MetadataAsValue(this.Context, compileUnitMetadata));
        }

        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero)
            {
                LLVM.DisposeModule(this);
                this.Handle = IntPtr.Zero;
            }
        }

        public void Dump() => LLVM.DumpModule(this);

        public override bool Equals(object obj) => (obj is LLVMModuleRef other) && this.Equals(other);

        public bool Equals(LLVMModuleRef other) => this == other;

        public LLVMValueRef GetNamedFunction(string name) => this.GetNamedFunction(name.AsSpan());

        public LLVMValueRef GetNamedFunction(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetNamedFunction(this, marshaledName);
        }

        public override int GetHashCode() => this.Handle.GetHashCode();

        public LLVMValueRef GetNamedGlobal(string name) => this.GetNamedGlobal(name.AsSpan());

        public LLVMValueRef GetNamedGlobal(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetNamedGlobal(this, marshaledName);
        }

        public LLVMValueRef[] GetNamedMetadataOperands(string name) => this.GetNamedMetadataOperands(name.AsSpan());

        public LLVMValueRef[] GetNamedMetadataOperands(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            var dest = new LLVMValueRef[LLVM.GetNamedMetadataNumOperands(this, marshaledName)];

            fixed (LLVMValueRef* pDest = dest)
            {
                LLVM.GetNamedMetadataOperands(this, marshaledName, (LLVMOpaqueValue**)pDest);
            }

            return dest;
        }

        public uint GetNamedMetadataOperandsCount(string name) => this.GetNamedMetadataOperandsCount(name.AsSpan());

        public uint GetNamedMetadataOperandsCount(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetNamedMetadataNumOperands(this, marshaledName);
        }

        public LLVMTypeRef GetTypeByName(string name) => this.GetTypeByName(name.AsSpan());

        public LLVMTypeRef GetTypeByName(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetTypeByName(this, marshaledName);
        }

        public void PrintToFile(string filename) => this.PrintToFile(filename.AsSpan());

        public void PrintToFile(ReadOnlySpan<char> filename)
        {
            if (!this.TryPrintToFile(filename, out string errorMessage))
            {
                throw new ExternalException(errorMessage);
            }
        }

        public string PrintToString()
        {
            var pStr = LLVM.PrintModuleToString(this);

            if (pStr == null)
            {
                return string.Empty;
            }

            var span = new ReadOnlySpan<byte>(pStr, int.MaxValue);

            var result = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public void SetModuleInlineAsm(string asm) => this.SetModuleInlineAsm(asm.AsSpan());

        public void SetModuleInlineAsm(ReadOnlySpan<char> asm)
        {
            using var marshaledAsm = new MarshaledString(asm);
            LLVM.SetModuleInlineAsm(this, marshaledAsm);
        }

        public override string ToString() => (this.Handle != IntPtr.Zero) ? this.PrintToString() : string.Empty;

        public bool TryCreateMCJITCompiler(out LLVMExecutionEngineRef outJIT, out string outError)
        {
            var options = LLVMMCJITCompilerOptions.Create();
            return this.TryCreateMCJITCompiler(out outJIT, ref options, out outError);
        }

        public bool TryCreateMCJITCompiler(out LLVMExecutionEngineRef outJIT, ref LLVMMCJITCompilerOptions options, out string outError)
        {
            fixed (LLVMExecutionEngineRef* pOutJIT = &outJIT)
            {
                fixed (LLVMMCJITCompilerOptions* pOptions = &options)
                {
                    sbyte* pError = null;
                    var result = LLVM.CreateMCJITCompilerForModule((LLVMOpaqueExecutionEngine**)pOutJIT, this, pOptions, (UIntPtr)Marshal.SizeOf<LLVMMCJITCompilerOptions>(), &pError);

                    if (pError is null)
                    {
                        outError = string.Empty;
                    }
                    else
                    {
                        var span = new ReadOnlySpan<byte>(pError, int.MaxValue);
                        outError = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
                    }

                    return result == 0;
                }
            }
        }

        public bool TryPrintToFile(string filename, out string errorMessage) => this.TryPrintToFile(filename.AsSpan(), out errorMessage);

        public bool TryPrintToFile(ReadOnlySpan<char> filename, out string errorMessage)
        {
            using var marshaledFilename = new MarshaledString(filename);

            sbyte* pErrorMessage = null;
            int result = 0;
            try
            {
                result = LLVM.PrintModuleToFile(this, marshaledFilename, &pErrorMessage);
            }
            catch (Exception)
            {
            }

            if (pErrorMessage == null)
            {
                errorMessage = string.Empty;
            }
            else
            {
                var span = new ReadOnlySpan<byte>(pErrorMessage, int.MaxValue);
                errorMessage = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            return result == 0;
        }

        public bool TryVerify(LLVMVerifierFailureAction action, out string outMessage)
        {
            sbyte* pMessage = null;
            var result = LLVM.VerifyModule(this, action, &pMessage);

            if (pMessage == null)
            {
                outMessage = string.Empty;
            }
            else
            {
                var span = new ReadOnlySpan<byte>(pMessage, int.MaxValue);
                outMessage = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            return result == 0;
        }

        public void Verify(LLVMVerifierFailureAction action)
        {
            if (!this.TryVerify(action, out string message))
            {
                throw new ExternalException(message);
            }
        }

        public int WriteBitcodeToFile(string path) => this.WriteBitcodeToFile(path.AsSpan());

        public int WriteBitcodeToFile(ReadOnlySpan<char> path)
        {
            using var marshaledPath = new MarshaledString(path);
            return LLVM.WriteBitcodeToFile(this, marshaledPath);
        }

        public int WriteBitcodeToFD(int fD, int shouldClose, int unbuffered) => LLVM.WriteBitcodeToFD(this, fD, shouldClose, unbuffered);

        public int WriteBitcodeToFileHandle(int handle) => LLVM.WriteBitcodeToFileHandle(this, handle);

        public LLVMMemoryBufferRef WriteBitcodeToMemoryBuffer() => LLVM.WriteBitcodeToMemoryBuffer(this);
    }
}
