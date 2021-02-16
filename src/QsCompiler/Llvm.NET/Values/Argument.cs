// -----------------------------------------------------------------------
// <copyright file="Argument.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>An LLVM Value representing an Argument to a function</summary>
    public class Argument
        : Value
    {
        /// <summary>Gets the function this argument belongs to</summary>
        public IrFunction ContainingFunction => FromHandle<IrFunction>( ValueHandle.ParamParent )!;

        internal Argument( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
