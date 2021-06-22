// <copyright file="LLVMModuleRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMModuleRef"/>.</summary>
    public static unsafe class LLVMModuleRefExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVM.GetFirstGlobalAlias"/>.</summary>
        public static LLVMValueRef FirstGlobalAlias(this LLVMModuleRef self) => (self.Handle != default) ? LLVM.GetFirstGlobalAlias(self) : default;

        /// <summary>Convenience wrapper for <see cref="LLVM.GetModuleIdentifier"/>.</summary>
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

        /// <summary>Convenience wrapper for <see cref="LLVM.LinkModules2"/>.</summary>
        public static bool Link(this LLVMModuleRef self, LLVMModuleRef other) => LLVM.LinkModules2(self, other) == 0;

        /// <summary>Convenience wrapper for <see cref="LLVM.AddGlobalIFunc"/>.</summary>
        public static LLVMValueRef AddGlobalIFunc(this LLVMModuleRef self, string name, LLVMTypeRef typeRef, uint addrSpace, LLVMValueRef resolver)
        {
            if (self.Handle == default)
            {
                return default;
            }

            return LLVM.AddGlobalIFunc(self, name.AsMarshaledString(), (UIntPtr)name.Length, typeRef, addrSpace, resolver);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetNamedGlobalIFunc"/>.</summary>
        public static LLVMValueRef GetNamedGlobalIFunc(this LLVMModuleRef self, string name)
        {
            if (self.Handle == default)
            {
                return default;
            }

            return LLVM.GetNamedGlobalIFunc(self, name.AsMarshaledString(), (UIntPtr)name.Length);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetNamedGlobalAlias"/>.</summary>
        public static LLVMValueRef GetNamedGlobalAlias(this LLVMModuleRef self, string name)
        {
            if (self.Handle == default)
            {
                return default;
            }

            return LLVM.GetNamedGlobalAlias(self, name.AsMarshaledString(), (UIntPtr)name.Length);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.AddModuleFlag"/>.</summary>
        public static void AddModuleFlag(this LLVMModuleRef self, LLVMModuleFlagBehavior behavior, string key, LLVMMetadataRef val)
        {
            LLVM.AddModuleFlag(self, behavior, key.AsMarshaledString(), (UIntPtr)key.Length, val);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetIntrinsicDeclaration"/>.</summary>
        public static LLVMValueRef GetIntrinsicDeclaration(this LLVMModuleRef self, uint id, LLVMTypeRef[] paramTypes)
        {
            fixed (LLVMTypeRef* pParamTypes = paramTypes.AsSpan())
            {
                return LLVM.GetIntrinsicDeclaration(self, id, (LLVMOpaqueType**)pParamTypes, (UIntPtr)paramTypes.Length);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.CreateDIBuilderDisallowUnresolved"/>.</summary>
        public static LLVMDIBuilderRef CreateDIBuilderDisallowUnresolved(this LLVMModuleRef self)
        {
            return LLVM.CreateDIBuilderDisallowUnresolved(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetModuleDataLayout"/>.</summary>
        public static LLVMTargetDataRef GetDataLayout(this LLVMModuleRef self)
        {
            return LLVM.GetModuleDataLayout(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.SetModuleDataLayout"/>.</summary>
        public static void SetDataLayout(this LLVMModuleRef self, LLVMTargetDataRef layout)
        {
            LLVM.SetModuleDataLayout(self, layout);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.CopyModuleFlagsMetadata"/>.</summary>
        public static (LLVMModuleFlagEntry Entry, ulong Length) CopyModuleFlagsMetadata(this LLVMModuleRef self)
        {
            UIntPtr len;
            LLVMModuleFlagEntry result = LLVM.CopyModuleFlagsMetadata(self, &len);
            return (result, (ulong)len);
        }
    }
}
