// -----------------------------------------------------------------------
// <copyright file="GlobalVariable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>An LLVM Global Variable</summary>
    public class GlobalVariable
        : GlobalObject
    {
        /// <summary>Gets or sets a value indicating whether this variable is initialized in an external module</summary>
        public bool IsExternallyInitialized
        {
            get => ValueHandle.IsExternallyInitialized;
            set
            {
                var val = ValueHandle;
                val.IsExternallyInitialized = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether this global is a Constant</summary>
        public bool IsConstant
        {
            get => ValueHandle.IsGlobalConstant;
            set
            {
                var val = ValueHandle;
                val.IsGlobalConstant = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether this global is stored per thread</summary>
        public bool IsThreadLocal
        {
            get => ValueHandle.IsThreadLocal;
            set
            {
                var val = ValueHandle;
                val.IsThreadLocal = value;
            }
        }

        /// <summary>Gets or sets the initial value for the variable</summary>
        public Constant Initializer
        {
            get
            {
                var handle = ValueHandle.Initializer;
                return handle == default ? default : FromHandle<Constant>( handle );
            }

            set
            {
                var val = ValueHandle;
                val.Initializer = value?.ValueHandle ?? default;
            }
        }

        internal GlobalVariable( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
