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
    /// <summary>Unified predicate enumeration</summary>
    /// <remarks>
    /// For floating point predicates "Ordered" means that neither operand is a QNAN
    /// while unordered means that either operand may be a QNAN.
    /// </remarks>
    [SuppressMessage( "Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "Not flags and shouldn't be marked as such" )]
    public enum Predicate
    {
        /// <summary>No comparison, always returns floating point false</summary>
        False = LLVMRealPredicate.LLVMRealPredicateFalse,

        /// <summary>Ordered and equal floating point comparison</summary>
        OrderedAndEqual = LLVMRealPredicate.LLVMRealOEQ,

        /// <summary>Ordered and greater than floating point comparison</summary>
        OrderedAndGreaterThan = LLVMRealPredicate.LLVMRealOGT,

        /// <summary>Ordered and greater than or equal floating point comparison</summary>
        OrderedAndGreaterThanOrEqual = LLVMRealPredicate.LLVMRealOGE,

        /// <summary>Ordered and less than floating point comparison</summary>
        OrderedAndLessThan = LLVMRealPredicate.LLVMRealOLT,

        /// <summary>Ordered and less than or equal floating point comparison</summary>
        OrderedAndLessThanOrEqual = LLVMRealPredicate.LLVMRealOLE,

        /// <summary>Ordered and not equal floating point comparison</summary>
        OrderedAndNotEqual = LLVMRealPredicate.LLVMRealONE,

        /// <summary>Ordered floating point comparison</summary>
        Ordered = LLVMRealPredicate.LLVMRealORD,

        /// <summary>Unordered floating point comparison</summary>
        Unordered = LLVMRealPredicate.LLVMRealUNO,

        /// <summary>Unordered and equal floating point comparison</summary>
        UnorderedAndEqual = LLVMRealPredicate.LLVMRealUEQ,

        /// <summary>Unordered or greater than floating point comparison</summary>
        UnorderedOrGreaterThan = LLVMRealPredicate.LLVMRealUGT,

        /// <summary>Unordered or greater than or Equal floating point comparison</summary>
        UnorderedOrGreaterThanOrEqual = LLVMRealPredicate.LLVMRealUGE,

        /// <summary>Unordered or Less than floating point comparison</summary>
        UnorderedOrLessThan = LLVMRealPredicate.LLVMRealULT,

        /// <summary>Unordered or Less than or Equal floating point comparison</summary>
        UnorderedOrLessThanOrEqual = LLVMRealPredicate.LLVMRealULE,

        /// <summary>Unordered or not equal floating point comparison</summary>
        UnorderedOrNotEqual = LLVMRealPredicate.LLVMRealUNE,

        /// <summary>No comparison, always returns true </summary>
        True = LLVMRealPredicate.LLVMRealPredicateTrue,

        /// <summary>Tag for the first floating point compare predicate, all floating point predicates are greater than or equal to this value</summary>
        FirstFcmpPredicate = False,

        /// <summary>Tag for the last floating point compare predicate, all floating point predicates are less than or equal to this value</summary>
        LastFcmpPredicate = True,

        /// <summary>Any value greater than or equal to this is not valid for Fcmp operations</summary>
        BadFcmpPredicate = LastFcmpPredicate + 1,

        /// <summary>Integer equality comparison</summary>
        Equal = LLVMIntPredicate.LLVMIntEQ,

        /// <summary>Integer not equal comparison</summary>
        NotEqual = LLVMIntPredicate.LLVMIntNE,

        /// <summary>Integer unsigned greater than comparison</summary>
        UnsignedGreaterThan = LLVMIntPredicate.LLVMIntUGT,

        /// <summary>Integer unsigned greater than or equal comparison</summary>
        UnsignedGreaterThanOrEqual = LLVMIntPredicate.LLVMIntUGE,

        /// <summary>Integer unsigned less than comparison</summary>
        UnsignedLessThan = LLVMIntPredicate.LLVMIntULT,

        /// <summary>Integer unsigned less than or equal comparison</summary>
        UnsignedLessThanOrEqual = LLVMIntPredicate.LLVMIntULE,

        /// <summary>Integer signed greater than comparison</summary>
        SignedGreaterThan = LLVMIntPredicate.LLVMIntSGT,

        /// <summary>Integer signed greater than or equal comparison</summary>
        SignedGreaterThanOrEqual = LLVMIntPredicate.LLVMIntSGE,

        /// <summary>Integer signed less than comparison</summary>
        SignedLessThan = LLVMIntPredicate.LLVMIntSLT,

        /// <summary>Integer signed less than or equal comparison</summary>
        SignedLessThanOrEqual = LLVMIntPredicate.LLVMIntSLE,

        /// <summary>Tag for the first integer compare predicate, all integer predicates are greater than or equal to this value</summary>
        FirstIcmpPredicate = Equal,

        /// <summary>Tag for the last integer compare predicate, all integer predicates are less than or equal to this value</summary>
        LastIcmpPredicate = SignedLessThanOrEqual,

        /// <summary>Any value Greater than or equal to this is not valid for cmp operations</summary>
        BadIcmpPredicate = LastIcmpPredicate + 1
    }

    /*
    TODO: extensions for predicate:
      static bool isFPPredicate(Predicate P) {
        return P >= FIRST_FCMP_PREDICATE && P <= LAST_FCMP_PREDICATE;
      }

      static bool isIntPredicate(Predicate P) {
        return P >= FIRST_ICMP_PREDICATE && P <= LAST_ICMP_PREDICATE;
      }

      /// For example, EQ->EQ, SLE->SGE, ULT->UGT,
      ///              OEQ->OEQ, ULE->UGE, OLT->OGT, etc.
      /// @returns the predicate that would be the result of exchanging the two
      /// operands of the CmpInst instruction without changing the result
      /// produced.
      /// @brief Return the predicate as if the operands were swapped
      static Predicate getSwappedPredicate(Predicate pred);

      /// For example, EQ -> NE, UGT -> ULE, SLT -> SGE,
      ///              OEQ -> UNE, UGT -> OLE, OLT -> UGE, etc.
      /// @returns the inverse predicate for predicate provided in \p pred.
      /// @brief Return the inverse of a given predicate
      static Predicate getInversePredicate(Predicate pred);

      /// For example, ULT->SLT, ULE->SLE, UGT->SGT, UGE->SGE, SLT->Failed assert
      /// @returns the signed version of the unsigned predicate pred.
      /// @brief return the signed version of a predicate
      static Predicate getSignedPredicate(Predicate pred);

      /// @returns true if the predicate is unsigned, false otherwise.
      /// @brief Determine if the predicate is an unsigned operation.
      static bool isUnsigned(Predicate predicate);

      /// @returns true if the predicate is signed, false otherwise.
      /// @brief Determine if the predicate is an signed operation.
      static bool isSigned(Predicate predicate);

      /// @brief Determine if the predicate is an ordered operation.
      static bool isOrdered(Predicate predicate);

      /// @brief Determine if the predicate is an unordered operation.
      static bool isUnordered(Predicate predicate);

      /// Determine if the predicate is true when comparing a value with itself.
      static bool isTrueWhenEqual(Predicate predicate);

      /// Determine if the predicate is false when comparing a value with itself.
      static bool isFalseWhenEqual(Predicate predicate);

      /// Determine if Pred1 implies Pred2 is true when two compares have matching
      /// operands.
      static bool isImpliedTrueByMatchingCmp(Predicate Pred1, Predicate Pred2);

      /// Determine if Pred1 implies Pred2 is false when two compares have matching
      /// operands.
      static bool isImpliedFalseByMatchingCmp(Predicate Pred1, Predicate Pred2);
    */

    /// <summary>Predicate enumeration for integer comparison</summary>
    public enum IntPredicate
    {
        /// <summary>No predicate, this is an invalid value for integer predicates</summary>
        None = 0,

        /// <summary>Integer equality comparison</summary>
        Equal = LLVMIntPredicate.LLVMIntEQ,

        /// <summary>Integer not equal comparison</summary>
        NotEqual = LLVMIntPredicate.LLVMIntNE,

        /// <summary>Integer unsigned greater than comparison</summary>
        UnsignedGreaterThan = LLVMIntPredicate.LLVMIntUGT,

        /// <summary>Integer unsigned greater than or equal comparison</summary>
        UnsignedGreaterOrEqual = LLVMIntPredicate.LLVMIntUGE,

        /// <summary>Integer unsigned less than comparison</summary>
        UnsignedLessThan = LLVMIntPredicate.LLVMIntULT,

        /// <summary>Integer unsigned less than or equal comparison</summary>
        UnsignedLessThanOrEqual = LLVMIntPredicate.LLVMIntULE,

        /// <summary>Integer signed greater than comparison</summary>
        SignedGreaterThan = LLVMIntPredicate.LLVMIntSGT,

        /// <summary>Integer signed greater than or equal comparison</summary>
        SignedGreaterThanOrEqual = LLVMIntPredicate.LLVMIntSGE,

        /// <summary>Integer signed less than comparison</summary>
        SignedLessThan = LLVMIntPredicate.LLVMIntSLT,

        /// <summary>Integer signed less than or equal comparison</summary>
        SignedLessThanOrEqual = LLVMIntPredicate.LLVMIntSLE
    }

    /// <summary>Predicate enumeration for floating point comparison</summary>
    /// <remarks>
    /// Floating point predicates "Ordered" means that neither operand is a QNAN
    /// while unordered means that either operand may be a QNAN.
    /// </remarks>
    public enum RealPredicate
    {
        /// <summary>No comparison, always returns floating point false</summary>
        False = LLVMRealPredicate.LLVMRealPredicateFalse,

        /// <summary>Ordered and equal floating point comparison</summary>
        OrderedAndEqual = LLVMRealPredicate.LLVMRealOEQ,

        /// <summary>Ordered and greater than floating point comparison</summary>
        OrderedAndGreaterThan = LLVMRealPredicate.LLVMRealOGT,

        /// <summary>Ordered and greater than or equal floating point comparison</summary>
        OrderedAndGreaterThanOrEqual = LLVMRealPredicate.LLVMRealOGE,

        /// <summary>Ordered and less than floating point comparison</summary>
        OrderedAndLessThan = LLVMRealPredicate.LLVMRealOLT,

        /// <summary>Ordered and less than or equal floating point comparison</summary>
        OrderedAndLessThanOrEqual = LLVMRealPredicate.LLVMRealOLE,

        /// <summary>Ordered and not equal floating point comparison</summary>
        OrderedAndNotEqual = LLVMRealPredicate.LLVMRealONE,

        /// <summary>Ordered floating point comparison</summary>
        Ordered = LLVMRealPredicate.LLVMRealORD,

        /// <summary>Unordered floating point comparison</summary>
        Unordered = LLVMRealPredicate.LLVMRealUNO,

        /// <summary>Unordered and equal floating point comparison</summary>
        UnorderedAndEqual = LLVMRealPredicate.LLVMRealUEQ,

        /// <summary>Unordered or greater than floating point comparison</summary>
        UnorderedOrGreaterThan = LLVMRealPredicate.LLVMRealUGT,

        /// <summary>Unordered or greater than or Equal floating point comparison</summary>
        UnorderedOrGreaterThanOrEqual = LLVMRealPredicate.LLVMRealUGE,

        /// <summary>Unordered or Less than floating point comparison</summary>
        UnorderedOrLessThan = LLVMRealPredicate.LLVMRealULT,

        /// <summary>Unordered or Less than or Equal floating point comparison</summary>
        UnorderedOrLessThanOrEqual = LLVMRealPredicate.LLVMRealULE,

        /// <summary>Unordered or not equal floating point comparison</summary>
        UnorderedOrNotEqual = LLVMRealPredicate.LLVMRealUNE,

        /// <summary>No comparison, always returns true </summary>
        True = LLVMRealPredicate.LLVMRealPredicateTrue
    }
}
