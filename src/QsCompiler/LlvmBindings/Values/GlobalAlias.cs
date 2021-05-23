// -----------------------------------------------------------------------
// <copyright file="GlobalAlias.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>LLVM Global Alias for a function or global value.</summary>
    public class GlobalAlias
        : GlobalIndirectSymbol
    {
        /// <summary>Gets or sets the aliasee that this Alias refers to.</summary>
        public Constant Aliasee
        {
            get => this.IndirectSymbol!;
            set => this.IndirectSymbol = value;
        }

        internal GlobalAlias(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
