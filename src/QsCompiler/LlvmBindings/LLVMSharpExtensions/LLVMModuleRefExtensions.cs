// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMModuleRef class.</summary>
    public static unsafe class LLVMModuleRefExtensions
    {
        /// <summary>Convenience wrapper for LLVM.GetFirstGlobalAlias.</summary>
        public static LLVMValueRef FirstGlobalAlias(this LLVMModuleRef self) => (self.Handle != default) ? LLVM.GetFirstGlobalAlias(self) : default;

        /// <summary>Convenience wrapper for LLVM.GetModuleIdentifier.</summary>
        public static string GetModuleIdentifier(this LLVMModuleRef self)
        {
            if (self.Handle == default)
            {
                return string.Empty;
            }

            UIntPtr len;
            var pStr = LLVM.GetModuleIdentifier(self, &len);
            if (pStr == default)
            {
                return string.Empty;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        /// <summary>Convenience wrapper for LLVM.LinkModules2.</summary>
        public static bool Link(this LLVMModuleRef self, LLVMModuleRef other) => LLVM.LinkModules2(self, other) == 0;

        /// <summary>Convenience wrapper for LLVM.AddGlobalIFunc.</summary>
        public static LLVMValueRef AddGlobalIFunc(this LLVMModuleRef self, string name, LLVMTypeRef typeRef, uint addrSpace, LLVMValueRef resolver)
        {
            if (self.Handle == default)
            {
                return default;
            }

            return LLVM.AddGlobalIFunc(self, name.AsMarshaledString(), (UIntPtr)name.Length, typeRef, addrSpace, resolver);
        }

        /// <summary>Convenience wrapper for LLVM.GetNamedGlobalIFunc.</summary>
        public static LLVMValueRef GetNamedGlobalIFunc(this LLVMModuleRef self, string name)
        {
            if (self.Handle == default)
            {
                return default;
            }

            return LLVM.GetNamedGlobalIFunc(self, name.AsMarshaledString(), (UIntPtr)name.Length);
        }

        /// <summary>Convenience wrapper for LLVM.GetNamedGlobalAlias.</summary>
        public static LLVMValueRef GetNamedGlobalAlias(this LLVMModuleRef self, string name)
        {
            if (self.Handle == default)
            {
                return default;
            }

            return LLVM.GetNamedGlobalAlias(self, name.AsMarshaledString(), (UIntPtr)name.Length);
        }

        /// <summary>Convenience wrapper for LLVM.AddModuleFlag.</summary>
        public static void AddModuleFlag(this LLVMModuleRef self, LLVMModuleFlagBehavior behavior, string key, LLVMMetadataRef val)
        {
            LLVM.AddModuleFlag(self, behavior, key.AsMarshaledString(), (UIntPtr)key.Length, val);
        }

        /// <summary>Convenience wrapper for LLVM.GetIntrinsicDeclaration.</summary>
        public static LLVMValueRef GetIntrinsicDeclaration(this LLVMModuleRef self, uint id, LLVMTypeRef[] paramTypes)
        {
            fixed (LLVMTypeRef* pParamTypes = paramTypes.AsSpan())
            {
                return LLVM.GetIntrinsicDeclaration(self, id, (LLVMOpaqueType**)pParamTypes, (UIntPtr)paramTypes.Length);
            }
        }
    }
}
