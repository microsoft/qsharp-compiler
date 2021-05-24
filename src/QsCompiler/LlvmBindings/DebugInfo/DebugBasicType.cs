// -----------------------------------------------------------------------
// <copyright file="DebugBasicType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information binding between an LLVM native <see cref="ITypeRef"/> and a <see cref="DIBasicType"/></summary>
    /// <remarks>
    /// This class provides a binding between an LLVM type and a corresponding <see cref="DIBasicType"/>.
    /// In LLVM all primitive types are unnamed and interned. That is, any use of an i8 is always the same
    /// type. However, at the source language level it is common to have named primitive types that map
    /// to the same underlying LLVM. For example, in C and C++ char maps to i8 but so does unsigned char
    /// (LLVM integral types don't have signed vs unsigned). This class is designed to handle this sort
    /// of one to many mapping of the lower level LLVM types to source level debugging types. Each
    /// instance of this class represents a source level basic type and the corresponding representation
    /// for LLVM.
    /// </remarks>
    /// <seealso href="xref:llvm_langref#dibasictype">LLVM DIBasicType</seealso>
    public class DebugBasicType
        : DebugType<ITypeRef, DIBasicType>
    {
        /// <summary>Initializes a new instance of the <see cref="DebugBasicType"/> class.</summary>
        /// <param name="llvmType">Type to wrap debug information for</param>
        /// <param name="module">Module to use when constructing the debug information</param>
        /// <param name="name">Source language name of the type</param>
        /// <param name="encoding">Encoding for the type</param>
        public DebugBasicType(ITypeRef llvmType, BitcodeModule module, string name, DiTypeKind encoding)
            : base(
                llvmType,
                module
                          .DIBuilder
                          .CreateBasicType(
                              name,
                              module.Layout.BitSizeOf(llvmType),
                              encoding))
        {
            if (module.Layout == null)
            {
                throw new ArgumentException();
            }

            switch (llvmType.Kind)
            {
            case TypeKind.Void:
            case TypeKind.Float16:
            case TypeKind.Float32:
            case TypeKind.Float64:
            case TypeKind.X86Float80:
            case TypeKind.Float128m112:
            case TypeKind.Float128:
            case TypeKind.Integer:
                break;

            default:
                throw new ArgumentException();
            }
        }
    }
}
