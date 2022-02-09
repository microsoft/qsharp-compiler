// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.InteropServices;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMExecutionEngineRef : IDisposable, IEquatable<LLVMExecutionEngineRef>
    {
        public IntPtr Handle;

        public LLVMExecutionEngineRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public LLVMTargetDataRef TargetData => (this.Handle != IntPtr.Zero) ? LLVM.GetExecutionEngineTargetData(this) : default;

        public LLVMTargetMachineRef TargetMachine => (this.Handle != IntPtr.Zero) ? LLVM.GetExecutionEngineTargetMachine(this) : default;

        public static implicit operator LLVMExecutionEngineRef(LLVMOpaqueExecutionEngine* value) => new LLVMExecutionEngineRef((IntPtr)value);

        public static implicit operator LLVMOpaqueExecutionEngine*(LLVMExecutionEngineRef value) => (LLVMOpaqueExecutionEngine*)value.Handle;

        public static bool operator ==(LLVMExecutionEngineRef left, LLVMExecutionEngineRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMExecutionEngineRef left, LLVMExecutionEngineRef right) => !(left == right);

        public void AddGlobalMapping(LLVMValueRef global, IntPtr addr) => LLVM.AddGlobalMapping(this, global, (void*)addr);

        public void AddModule(LLVMModuleRef m) => LLVM.AddModule(this, m);

        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero)
            {
                LLVM.DisposeExecutionEngine(this);
                this.Handle = IntPtr.Zero;
            }
        }

        public override bool Equals(object obj) => (obj is LLVMExecutionEngineRef other) && this.Equals(other);

        public bool Equals(LLVMExecutionEngineRef other) => this == other;

        public LLVMValueRef FindFunction(string name) => this.FindFunction(name.AsSpan());

        public LLVMValueRef FindFunction(ReadOnlySpan<char> name)
        {
            if (!this.TryFindFunction(name, out var fn))
            {
                throw new ExternalException();
            }

            return fn;
        }

        public void FreeMachineCodeForFunction(LLVMValueRef f) => LLVM.FreeMachineCodeForFunction(this, f);

        public ulong GetFunctionAddress(string name) => this.GetFunctionAddress(name.AsSpan());

        public ulong GetFunctionAddress(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetFunctionAddress(this, marshaledName);
        }

        public ulong GetGlobalValueAddress(string name) => this.GetGlobalValueAddress(name.AsSpan());

        public ulong GetGlobalValueAddress(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetGlobalValueAddress(this, marshaledName);
        }

        public override int GetHashCode() => this.Handle.GetHashCode();

        public IntPtr GetPointerToGlobal(LLVMValueRef global) => (IntPtr)LLVM.GetPointerToGlobal(this, global);

        public TDelegate GetPointerToGlobal<TDelegate>(LLVMValueRef global)
        {
            var pGlobal = this.GetPointerToGlobal(global);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(pGlobal);
        }

        public LLVMModuleRef RemoveModule(LLVMModuleRef m)
        {
            if (!this.TryRemoveModule(m, out LLVMModuleRef mod, out string error))
            {
                throw new ExternalException(error);
            }

            return mod;
        }

        public LLVMGenericValueRef RunFunction(LLVMValueRef f, LLVMGenericValueRef[] args) => this.RunFunction(f, args.AsSpan());

        public LLVMGenericValueRef RunFunction(LLVMValueRef f, ReadOnlySpan<LLVMGenericValueRef> args)
        {
            fixed (LLVMGenericValueRef* pArgs = args)
            {
                return LLVM.RunFunction(this, f, (uint)args.Length, (LLVMOpaqueGenericValue**)pArgs);
            }
        }

        public int RunFunctionAsMain(LLVMValueRef f, uint argC, string[] argV, string[] envP) => this.RunFunctionAsMain(f, argC, argV.AsSpan(), envP.AsSpan());

        public int RunFunctionAsMain(LLVMValueRef f, uint argC, ReadOnlySpan<string> argV, ReadOnlySpan<string> envP)
        {
            using var marshaledArgV = new MarshaledStringArray(argV);
            using var marshaledEnvP = new MarshaledStringArray(envP);

            var pArgV = stackalloc sbyte*[marshaledArgV.Count];
            marshaledArgV.Fill(pArgV);

            var pEnvP = stackalloc sbyte*[marshaledEnvP.Count];
            marshaledEnvP.Fill(pEnvP);

            return LLVM.RunFunctionAsMain(this, f, argC, pArgV, pEnvP);
        }

        public void RunStaticConstructors() => LLVM.RunStaticConstructors(this);

        public void RunStaticDestructors() => LLVM.RunStaticDestructors(this);

        public IntPtr RecompileAndRelinkFunction(LLVMValueRef fn) => (IntPtr)LLVM.RecompileAndRelinkFunction(this, fn);

        public override string ToString() => $"{nameof(LLVMExecutionEngineRef)}: {this.Handle:X}";

        public bool TryFindFunction(string name, out LLVMValueRef outFn) => this.TryFindFunction(name.AsSpan(), out outFn);

        public bool TryFindFunction(ReadOnlySpan<char> name, out LLVMValueRef outFn)
        {
            fixed (LLVMValueRef* pOutFn = &outFn)
            {
                using var marshaledName = new MarshaledString(name);
                return LLVM.FindFunction(this, marshaledName, (LLVMOpaqueValue**)pOutFn) == 0;
            }
        }

        public bool TryRemoveModule(LLVMModuleRef m, out LLVMModuleRef outMod, out string outError)
        {
            fixed (LLVMModuleRef* pOutMod = &outMod)
            {
                sbyte* pError = null;
                var result = LLVM.RemoveModule(this, m, (LLVMOpaqueModule**)pOutMod, &pError);

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
}
