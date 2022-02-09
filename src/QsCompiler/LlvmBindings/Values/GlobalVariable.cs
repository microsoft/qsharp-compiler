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
    /// <summary>An LLVM Global Variable.</summary>
    public class GlobalVariable
        : GlobalObject
    {
        internal GlobalVariable(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }

        /// <summary>Gets or sets a value indicating whether this variable is initialized in an external module.</summary>
        public bool IsExternallyInitialized
        {
            get => this.ValueHandle.IsExternallyInitialized;
            set
            {
                var val = this.ValueHandle;
                val.IsExternallyInitialized = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether this global is a Constant.</summary>
        public bool IsConstant
        {
            get => this.ValueHandle.IsGlobalConstant;
            set
            {
                var val = this.ValueHandle;
                val.IsGlobalConstant = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether this global is stored per thread.</summary>
        public bool IsThreadLocal
        {
            get => this.ValueHandle.IsThreadLocal;
            set
            {
                var val = this.ValueHandle;
                val.IsThreadLocal = value;
            }
        }

        /// <summary>Gets or sets the initial value for the variable.</summary>
        public Constant? Initializer
        {
            get
            {
                var handle = this.ValueHandle.Initializer;
                return handle == default ? default : FromHandle<Constant>(handle);
            }

            set
            {
                var val = this.ValueHandle;
                val.Initializer = value?.ValueHandle ?? default;
            }
        }
    }
}
