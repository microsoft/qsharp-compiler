// -----------------------------------------------------------------------
// <copyright file="Predicates.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Unified predicate enumeration.</summary>
    /// <remarks>
    /// For floating point predicates "Ordered" means that neither operand is a QNAN
    /// while unordered means that either operand may be a QNAN.
    /// </remarks>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "Not flags and shouldn't be marked as such")]
    public enum Predicate
    {
        /// <summary>No comparison, always returns floating point false.</summary>
        False = LLVMRealPredicate.LLVMRealPredicateFalse,

        /// <summary>Ordered and equal floating point comparison.</summary>
        OrderedAndEqual = LLVMRealPredicate.LLVMRealOEQ,

        /// <summary>Ordered and greater than floating point comparison.</summary>
        OrderedAndGreaterThan = LLVMRealPredicate.LLVMRealOGT,

        /// <summary>Ordered and greater than or equal floating point comparison.</summary>
        OrderedAndGreaterThanOrEqual = LLVMRealPredicate.LLVMRealOGE,

        /// <summary>Ordered and less than floating point comparison.</summary>
        OrderedAndLessThan = LLVMRealPredicate.LLVMRealOLT,

        /// <summary>Ordered and less than or equal floating point comparison.</summary>
        OrderedAndLessThanOrEqual = LLVMRealPredicate.LLVMRealOLE,

        /// <summary>Ordered and not equal floating point comparison.</summary>
        OrderedAndNotEqual = LLVMRealPredicate.LLVMRealONE,

        /// <summary>Ordered floating point comparison.</summary>
        Ordered = LLVMRealPredicate.LLVMRealORD,

        /// <summary>Unordered floating point comparison.</summary>
        Unordered = LLVMRealPredicate.LLVMRealUNO,

        /// <summary>Unordered and equal floating point comparison.</summary>
        UnorderedAndEqual = LLVMRealPredicate.LLVMRealUEQ,

        /// <summary>Unordered or greater than floating point comparison.</summary>
        UnorderedOrGreaterThan = LLVMRealPredicate.LLVMRealUGT,

        /// <summary>Unordered or greater than or Equal floating point comparison.</summary>
        UnorderedOrGreaterThanOrEqual = LLVMRealPredicate.LLVMRealUGE,

        /// <summary>Unordered or Less than floating point comparison.</summary>
        UnorderedOrLessThan = LLVMRealPredicate.LLVMRealULT,

        /// <summary>Unordered or Less than or Equal floating point comparison.</summary>
        UnorderedOrLessThanOrEqual = LLVMRealPredicate.LLVMRealULE,

        /// <summary>Unordered or not equal floating point comparison.</summary>
        UnorderedOrNotEqual = LLVMRealPredicate.LLVMRealUNE,

        /// <summary>No comparison, always returns true. </summary>
        True = LLVMRealPredicate.LLVMRealPredicateTrue,

        /// <summary>Tag for the first floating point compare predicate, all floating point predicates are greater than or equal to this value.</summary>
        FirstFcmpPredicate = False,

        /// <summary>Tag for the last floating point compare predicate, all floating point predicates are less than or equal to this value.</summary>
        LastFcmpPredicate = True,

        /// <summary>Any value greater than or equal to this is not valid for Fcmp operations.</summary>
        BadFcmpPredicate = LastFcmpPredicate + 1,

        /// <summary>Integer equality comparison.</summary>
        Equal = LLVMIntPredicate.LLVMIntEQ,

        /// <summary>Integer not equal comparison.</summary>
        NotEqual = LLVMIntPredicate.LLVMIntNE,

        /// <summary>Integer unsigned greater than comparison.</summary>
        UnsignedGreaterThan = LLVMIntPredicate.LLVMIntUGT,

        /// <summary>Integer unsigned greater than or equal comparison.</summary>
        UnsignedGreaterThanOrEqual = LLVMIntPredicate.LLVMIntUGE,

        /// <summary>Integer unsigned less than comparison.</summary>
        UnsignedLessThan = LLVMIntPredicate.LLVMIntULT,

        /// <summary>Integer unsigned less than or equal comparison.</summary>
        UnsignedLessThanOrEqual = LLVMIntPredicate.LLVMIntULE,

        /// <summary>Integer signed greater than comparison.</summary>
        SignedGreaterThan = LLVMIntPredicate.LLVMIntSGT,

        /// <summary>Integer signed greater than or equal comparison.</summary>
        SignedGreaterThanOrEqual = LLVMIntPredicate.LLVMIntSGE,

        /// <summary>Integer signed less than comparison.</summary>
        SignedLessThan = LLVMIntPredicate.LLVMIntSLT,

        /// <summary>Integer signed less than or equal comparison.</summary>
        SignedLessThanOrEqual = LLVMIntPredicate.LLVMIntSLE,

        /// <summary>Tag for the first integer compare predicate, all integer predicates are greater than or equal to this value.</summary>
        FirstIcmpPredicate = Equal,

        /// <summary>Tag for the last integer compare predicate, all integer predicates are less than or equal to this value.</summary>
        LastIcmpPredicate = SignedLessThanOrEqual,

        /// <summary>Any value Greater than or equal to this is not valid for cmp operations.</summary>
        BadIcmpPredicate = LastIcmpPredicate + 1,
    }

    /// <summary>Predicate enumeration for integer comparison.</summary>
    public enum IntPredicate
    {
        /// <summary>No predicate, this is an invalid value for integer predicates.</summary>
        None = 0,

        /// <summary>Integer equality comparison.</summary>
        Equal = LLVMIntPredicate.LLVMIntEQ,

        /// <summary>Integer not equal comparison.</summary>
        NotEqual = LLVMIntPredicate.LLVMIntNE,

        /// <summary>Integer unsigned greater than comparison.</summary>
        UnsignedGreaterThan = LLVMIntPredicate.LLVMIntUGT,

        /// <summary>Integer unsigned greater than or equal comparison.</summary>
        UnsignedGreaterOrEqual = LLVMIntPredicate.LLVMIntUGE,

        /// <summary>Integer unsigned less than comparison.</summary>
        UnsignedLessThan = LLVMIntPredicate.LLVMIntULT,

        /// <summary>Integer unsigned less than or equal comparison.</summary>
        UnsignedLessThanOrEqual = LLVMIntPredicate.LLVMIntULE,

        /// <summary>Integer signed greater than comparison.</summary>
        SignedGreaterThan = LLVMIntPredicate.LLVMIntSGT,

        /// <summary>Integer signed greater than or equal comparison.</summary>
        SignedGreaterThanOrEqual = LLVMIntPredicate.LLVMIntSGE,

        /// <summary>Integer signed less than comparison.</summary>
        SignedLessThan = LLVMIntPredicate.LLVMIntSLT,

        /// <summary>Integer signed less than or equal comparison.</summary>
        SignedLessThanOrEqual = LLVMIntPredicate.LLVMIntSLE,
    }

    /// <summary>Predicate enumeration for floating point comparison.</summary>
    /// <remarks>
    /// Floating point predicates "Ordered" means that neither operand is a QNAN
    /// while unordered means that either operand may be a QNAN.
    /// </remarks>
    public enum RealPredicate
    {
        /// <summary>No comparison, always returns floating point false.</summary>
        False = LLVMRealPredicate.LLVMRealPredicateFalse,

        /// <summary>Ordered and equal floating point comparison.</summary>
        OrderedAndEqual = LLVMRealPredicate.LLVMRealOEQ,

        /// <summary>Ordered and greater than floating point comparison.</summary>
        OrderedAndGreaterThan = LLVMRealPredicate.LLVMRealOGT,

        /// <summary>Ordered and greater than or equal floating point comparison.</summary>
        OrderedAndGreaterThanOrEqual = LLVMRealPredicate.LLVMRealOGE,

        /// <summary>Ordered and less than floating point comparison.</summary>
        OrderedAndLessThan = LLVMRealPredicate.LLVMRealOLT,

        /// <summary>Ordered and less than or equal floating point comparison.</summary>
        OrderedAndLessThanOrEqual = LLVMRealPredicate.LLVMRealOLE,

        /// <summary>Ordered and not equal floating point comparison.</summary>
        OrderedAndNotEqual = LLVMRealPredicate.LLVMRealONE,

        /// <summary>Ordered floating point comparison.</summary>
        Ordered = LLVMRealPredicate.LLVMRealORD,

        /// <summary>Unordered floating point comparison.</summary>
        Unordered = LLVMRealPredicate.LLVMRealUNO,

        /// <summary>Unordered and equal floating point comparison.</summary>
        UnorderedAndEqual = LLVMRealPredicate.LLVMRealUEQ,

        /// <summary>Unordered or greater than floating point comparison.</summary>
        UnorderedOrGreaterThan = LLVMRealPredicate.LLVMRealUGT,

        /// <summary>Unordered or greater than or Equal floating point comparison.</summary>
        UnorderedOrGreaterThanOrEqual = LLVMRealPredicate.LLVMRealUGE,

        /// <summary>Unordered or Less than floating point comparison.</summary>
        UnorderedOrLessThan = LLVMRealPredicate.LLVMRealULT,

        /// <summary>Unordered or Less than or Equal floating point comparison.</summary>
        UnorderedOrLessThanOrEqual = LLVMRealPredicate.LLVMRealULE,

        /// <summary>Unordered or not equal floating point comparison.</summary>
        UnorderedOrNotEqual = LLVMRealPredicate.LLVMRealUNE,

        /// <summary>No comparison, always returns true. </summary>
        True = LLVMRealPredicate.LLVMRealPredicateTrue,
    }
}
