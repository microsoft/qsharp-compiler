// -----------------------------------------------------------------------
// <copyright file="IAttributeAccessor.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Function index for attributes.</summary>
    /// <remarks>
    /// Attributes on functions apply to the function itself, the return type
    /// or one of the function's parameters. This enumeration is used to
    /// identify where the attribute applies.
    /// </remarks>
    public enum FunctionAttributeIndex
    {
        /// <summary>The attribute applies to the function itself.</summary>
        Function = LLVMAttributeIndex.LLVMAttributeFunctionIndex,

        /// <summary>The attribute applies to the return type of the function.</summary>
        ReturnType = LLVMAttributeIndex.LLVMAttributeReturnIndex,

        /// <summary>The attribute applies to the first parameter of the function.</summary>
        /// <remarks>
        /// Additional parameters are identified by simply adding an integer value to
        /// this value. (i.e. FunctionAttributeIndex.Parameter0 + 1 ).
        /// </remarks>
        Parameter0 = ReturnType + 1,
    }

    /// <summary>Interface for raw attribute access.</summary>
    /// <remarks>
    /// As of LLVM v3.9x and later, Functions and call sites use distinct LLVM-C API sets for
    /// manipulating attributes. Fortunately, they have consistent signatures so this interface
    /// is used to abstract the difference via derived types specialized for each case.
    /// Going forward this is the most direct way to manipulate attributes on a value as all the
    /// other forms ultimately come down to this interface.
    /// </remarks>
    public interface IAttributeAccessor
        : IAttributeContainer
    {
        /// <summary>Gets the count of attributes on a given index.</summary>
        /// <param name="index">Index to get the count for.</param>
        /// <returns>Number of attributes on the specified index.</returns>
        uint GetAttributeCountAtIndex(FunctionAttributeIndex index);

        /// <summary>Gets the attributes on a given index.</summary>
        /// <param name="index">index to get the attributes for.</param>
        /// <returns>Attributes for the index.</returns>
        IEnumerable<AttributeValue> GetAttributesAtIndex(FunctionAttributeIndex index);

        /// <summary>Gets a specific attribute at a given index.</summary>
        /// <param name="index">Index to get the attribute from.</param>
        /// <param name="kind"><see cref="AttributeKind"/> to get.</param>
        /// <returns>The specified attribute or the default <see cref="AttributeValue"/>.</returns>
        AttributeValue GetAttributeAtIndex(FunctionAttributeIndex index, AttributeKind kind);

        /// <summary>Gets a named attribute at a given index.</summary>
        /// <param name="index">Index to get the attribute from.</param>
        /// <param name="name">name of the attribute to get.</param>
        /// <returns>The specified attribute or the default <see cref="AttributeValue"/>.</returns>
        AttributeValue GetAttributeAtIndex(FunctionAttributeIndex index, string name);

        /// <summary>Adds an <see cref="AttributeValue"/> at a specified index.</summary>
        /// <param name="index">Index to add the attribute to.</param>
        /// <param name="attrib">Attribute to add.</param>
        void AddAttributeAtIndex(FunctionAttributeIndex index, AttributeValue attrib);

        /// <summary>Removes an <see cref="AttributeKind"/> at a specified index.</summary>
        /// <param name="index">Index to add the attribute to.</param>
        /// <param name="kind">Attribute to Remove.</param>
        void RemoveAttributeAtIndex(FunctionAttributeIndex index, AttributeKind kind);

        /// <summary>Removes a named attribute at a specified index.</summary>
        /// <param name="index">Index to add the attribute to.</param>
        /// <param name="name">Name of the attribute to remove.</param>
        void RemoveAttributeAtIndex(FunctionAttributeIndex index, string name);
    }
}
