// -----------------------------------------------------------------------
// <copyright file="GlobalIndirectSymbol.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Global Indirect Symbol</summary>
    public class GlobalIndirectSymbol
        : GlobalValue
    {
        /// <summary>Gets or sets the symbol this indirectly references</summary>
        public Constant IndirectSymbol
        {
            get => Operands.GetOperand<Constant>( 0 )!;
            set => Operands[ 0 ] = value;
        }

        internal GlobalIndirectSymbol( LLVMValueRef handle )
            : base( handle )
        {
        }
    }
}
