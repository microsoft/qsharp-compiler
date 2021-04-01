// -----------------------------------------------------------------------
// <copyright file="ITypeRef.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Values;

// Interface+internal type matches file name
#pragma warning disable SA1649

namespace Ubiquity.NET.Llvm.Types
{
    /// <summary>Basic kind of a type.</summary>
    public enum TypeKind
    {
        /// <summary>Type with no size.</summary>
        Void = LLVMTypeKind.LLVMVoidTypeKind,

        /// <summary>16 bit floating point type.</summary>
        Float16 = LLVMTypeKind.LLVMHalfTypeKind,

        /// <summary>32 bit floating point type.</summary>
        Float32 = LLVMTypeKind.LLVMFloatTypeKind,

        /// <summary>64 bit floating point type.</summary>
        Float64 = LLVMTypeKind.LLVMDoubleTypeKind,

        /// <summary>80 bit floating point type (X87).</summary>
        X86Float80 = LLVMTypeKind.LLVMX86_FP80TypeKind,

        /// <summary>128 bit floating point type (112-bit mantissa).</summary>
        Float128m112 = LLVMTypeKind.LLVMFP128TypeKind,

        /// <summary>128 bit floating point type (two 64-bits).</summary>
        Float128 = LLVMTypeKind.LLVMPPC_FP128TypeKind,

        /// <summary><see cref="BasicBlock"/> instruction label.</summary>
        Label = LLVMTypeKind.LLVMLabelTypeKind,

        /// <summary>Arbitrary bit width integers.</summary>
        Integer = LLVMTypeKind.LLVMIntegerTypeKind,

        /// <summary><see cref="IFunctionType"/>.</summary>
        Function = LLVMTypeKind.LLVMFunctionTypeKind,

        /// <summary><see cref="IStructType"/>.</summary>
        Struct = LLVMTypeKind.LLVMStructTypeKind,

        /// <summary><see cref="IArrayType"/>.</summary>
        Array = LLVMTypeKind.LLVMArrayTypeKind,

        /// <summary><see cref="IPointerType"/>.</summary>
        Pointer = LLVMTypeKind.LLVMPointerTypeKind,

        /// <summary>SIMD 'packed' format, or other <see cref="IVectorType"/> implementation.</summary>
        Vector = LLVMTypeKind.LLVMVectorTypeKind,

        /// <summary><see cref="Llvm.LlvmMetadata"/>.</summary>
        Metadata = LLVMTypeKind.LLVMMetadataTypeKind,

        /// <summary>x86 MMX data type.</summary>
        X86MMX = LLVMTypeKind.LLVMX86_MMXTypeKind,

        /// <summary>Exception handler token.</summary>
        Token = LLVMTypeKind.LLVMTokenTypeKind,
    }

    /// <summary>Interface for a Type in LLVM.</summary>
    public interface ITypeRef
    {
        /// <summary>Gets a value indicating whether the type is sized.</summary>
        bool IsSized { get; }

        /// <summary>Gets the LLVM Type kind for this type.</summary>
        TypeKind Kind { get; }

        /// <summary>Gets a value indicating whether this type is an integer.</summary>
        bool IsInteger { get; }

        /// <summary>Gets a value indicating whether the type is a 32-bit IEEE floating point type.</summary>
        bool IsFloat { get; }

        /// <summary>Gets a value indicating whether the type is a 64-bit IEEE floating point type.</summary>
        bool IsDouble { get; }

        /// <summary>Gets a value indicating whether this type represents the void type.</summary>
        bool IsVoid { get; }

        /// <summary>Gets a value indicating whether this type is a structure type.</summary>
        bool IsStruct { get; }

        /// <summary>Gets a value indicating whether this type is a pointer.</summary>
        bool IsPointer { get; }

        /// <summary>Gets a value indicating whether this type is a sequence type.</summary>
        bool IsSequence { get; }

        /// <summary>Gets a value indicating whether this type is a floating point type.</summary>
        bool IsFloatingPoint { get; }

        /// <summary>Gets a value indicating whether this type is a pointer to a pointer.</summary>
        bool IsPointerPointer { get; }

        /// <summary>Gets the Context that owns this type.</summary>
        Context Context { get; }

        /// <summary>Gets the integer bit width of this type or 0 for non integer types.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Justification = "Value is the bit width of an integer, name is appropriate")]
        uint IntegerBitWidth { get; }

        /// <summary>Gets a null value (e.g. all bits == 0 ) for the type.</summary>
        /// <remarks>
        /// This is a getter function instead of a property as it can throw exceptions
        /// for types that don't support such a thing (i.e. void ).
        /// </remarks>
        /// <returns><see cref="Constant"/> that represents a null (0) value of this type.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "See comment remarks")]
        Constant GetNullValue();

        /// <summary>Array type factory for an array with elements of this type.</summary>
        /// <param name="count">Number of elements in the array.</param>
        /// <returns><see cref="IArrayType"/> for the array.</returns>
        IArrayType CreateArrayType(uint count);

        /// <summary>Get a <see cref="IPointerType"/> for a type that points to elements of this type in the default (0) address space.</summary>
        /// <returns><see cref="IPointerType"/>corresponding to the type of a pointer that refers to elements of this type.</returns>
        IPointerType CreatePointerType();

        /// <summary>Get a <see cref="IPointerType"/> for a type that points to elements of this type in the specified address space.</summary>
        /// <param name="addressSpace">Address space for the pointer.</param>
        /// <returns><see cref="IPointerType"/>corresponding to the type of a pointer that refers to elements of this type.</returns>
        IPointerType CreatePointerType(uint addressSpace);
    }

    /// <summary>Internal interface for getting access to the raw type handle internally.</summary>
    /// <remarks>This is usually implemented as an explicit interface implementation so that it isn't exposed publicly.</remarks>
    internal interface ITypeHandleOwner
    {
        /// <summary>Gets the LibLLVM handle for the type.</summary>
        LLVMTypeRef TypeHandle { get; }
    }

    internal static class TypeRefExtensions
    {
        internal static LLVMTypeRef GetTypeRef(this ITypeRef self)
        {
            return ((ITypeHandleOwner)self).TypeHandle;
        }
    }
}
