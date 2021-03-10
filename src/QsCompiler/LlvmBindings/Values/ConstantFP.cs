// -----------------------------------------------------------------------
// <copyright file="ConstantFP.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Floating point constant value in LLVM</summary>
    public sealed class ConstantFP
        : ConstantData
    {
        /// <summary>Gets the value of the constant, possibly losing precision</summary>
        public double Value => GetValueWithLoss( out bool _ );

        /// <summary>Gets the value of the constant, possibly losing precision</summary>
        /// <param name="loosesInfo">flag indicating if precision is lost</param>
        /// <returns>The value of the constant</returns>
        /// <remarks>
        /// Loss can occur when getting a target specific high resolution value,
        /// such as an 80bit Floating point value.
        /// </remarks>
        public double GetValueWithLoss( out bool loosesInfo ) => ValueHandle.GetConstRealDouble( out loosesInfo );

        internal ConstantFP( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
