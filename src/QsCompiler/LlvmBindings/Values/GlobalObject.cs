// -----------------------------------------------------------------------
// <copyright file="GlobalObject.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Base class for Global objects in an LLVM Module.</summary>
    public unsafe class GlobalObject
        : GlobalValue
    {
        internal GlobalObject(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }

        /// <summary>Gets or sets the alignment requirements for this object.</summary>
        public uint Alignment
        {
            get => this.ValueHandle == default ? default : this.ValueHandle.Alignment;
            set
            {
                var val = this.ValueHandle;
                val.Alignment = value;
            }
        }

        /// <summary>Gets or sets the linker section this object belongs to.</summary>
        public string? Section
        {
            get => this.ValueHandle == default ? default : this.ValueHandle.Section;
            set
            {
                var val = this.ValueHandle;
                val.Section = value;
            }
        }
    }
}
