// -----------------------------------------------------------------------
// <copyright file="Constant.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;


using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Contains an LLVM Constant value</summary>
    public unsafe class Constant
        : User
    {
        /// <summary>Gets a value indicating whether the constant is a Zero value for the its type</summary>
        public bool IsZeroValue => ValueHandle.IsAConstantAggregateZero != default;

        /// <summary>Create a NULL pointer for a given type</summary>
        /// <param name="typeRef">Type of pointer to create a null vale for</param>
        /// <returns>Constant NULL pointer of the specified type</returns>
        public static Constant NullValueFor( ITypeRef typeRef )
        {
            if( typeRef == default )
            {
                throw new ArgumentNullException( nameof( typeRef ) );
            }

            var kind = typeRef.Kind;
            if( kind == TypeKind.Label || kind == TypeKind.Function || ( typeRef is StructType structType && structType.IsOpaque ) )
            {
                throw new ArgumentException( );
            }

            return FromHandle<Constant>( LLVM.ConstNull( typeRef.GetTypeRef( ) ) )!;
        }

        /// <summary>Creates a constant instance of <paramref name="typeRef"/> with all bits in the instance set to 1</summary>
        /// <param name="typeRef">Type of value to create</param>
        /// <returns>Constant for the type with all instance bits set to 1</returns>
        public static Constant AllOnesValueFor( ITypeRef typeRef )
            => FromHandle<Constant>( LLVM.ConstAllOnes( typeRef.GetTypeRef( ) ) )!;

        /// <summary>Creates an <see cref="Constant"/> representing an undefined value for <paramref name="typeRef"/></summary>
        /// <param name="typeRef">Type to create the undefined value for</param>
        /// <returns>
        /// <see cref="Constant"/> representing an undefined value of <paramref name="typeRef"/>
        /// </returns>
        public static Constant UndefinedValueFor( ITypeRef typeRef )
            => FromHandle<Constant>( LLVM.GetUndef( typeRef.GetTypeRef( ) ) )!;

        /// <summary>Create a constant NULL pointer for a given type</summary>
        /// <param name="typeRef">Type of pointer to create a null value for</param>
        /// <returns>Constant NULL pointer of the specified type</returns>
        public static Constant ConstPointerToNullFor( ITypeRef typeRef )
            => FromHandle<Constant>( LLVM.ConstPointerNull( typeRef.GetTypeRef( ) ) )!;

        internal Constant( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
