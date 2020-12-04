// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;

namespace Microsoft.Quantum.QIR
{
    /// <summary>
    /// Each class instance contains the QIR types defined and used
    /// within the compilation unit given upon instantiation.
    /// </summary>
    public class Types
    {
        /// <summary>
        /// Represents the type of a 64-bit signed integer in QIR.
        /// </summary>
        public readonly ITypeRef Int;

        /// <summary>
        /// Represents the type of a double precision floating point number in QIR.
        /// </summary>
        public readonly ITypeRef Double;

        /// <summary>
        /// Represents the type of a boolean value in QIR.
        /// </summary>
        public readonly ITypeRef Bool;

        /// <summary>
        /// Represents the type of a single-qubit Pauli matrix in QIR
        /// used to indicate e.g. the basis of a quantum measurement.
        /// The type is a two-bit integer type.
        /// </summary>
        public readonly ITypeRef Pauli;

        /// <summary>
        /// Represents the type of a result value from a quantum measurement in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public readonly IPointerType Result;

        /// <summary>
        /// Represents the type of a string value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public readonly IPointerType Qubit;

        /// <summary>
        /// Represents the type of a string value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public readonly IPointerType String;

        /// <summary>
        /// Represents the type of a big integer value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public readonly IPointerType BigInt;

        /// <summary>
        /// Represents the type of an array in QIR.
        /// The type is a pointer to an opaque struct.
        /// Item access is provided by the runtime library.
        /// The library method(s) return byte pointers
        /// that need to be cast to the appropriate type.
        /// </summary>
        public readonly IPointerType Array;

        /// <summary>
        /// Represents the type of a tuple value in QIR.
        /// The type is a pointer to an opaque struct.
        /// For item access and deconstruction, tuple values need to be cast
        /// to a suitable concrete type depending on the types of their items.
        /// Such a concrete tuple type is constructed using <see cref="CreateConcreteTupleType"/>.
        /// </summary>
        public readonly IPointerType Tuple;

        /// <summary>
        /// Represents the type of a callable value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public readonly IPointerType Callable;

        /// <summary>
        /// Represents the signature of a callable specialization in QIR.
        /// It takes three tuples (pointers) as input and returns void. The inputs are
        /// a tuple containing all captures values,
        /// a tuple containing all arguments,
        /// and a tuple where the output will be stored.
        /// </summary>
        public readonly IFunctionType FunctionSignature;

        /// <summary>
        /// Represents the type of a range of numbers defined by a start, step, and end value.
        /// The type is a named struct that contains three 64-bit integers.
        /// </summary>
        public readonly IStructType Range;

        // private fields

        private readonly IStructType tupleHeader;
        private readonly Context context;

        // constructor

        internal Types(Context context)
        {
            this.context = context;

            this.tupleHeader = context.CreateStructType(TypeNames.Tuple, false, context.Int32Type); // private
            this.Range = context.CreateStructType(TypeNames.Range, false, context.Int64Type, context.Int64Type, context.Int64Type);

            this.Result = context.CreateStructType(TypeNames.Result).CreatePointerType();
            this.Qubit = context.CreateStructType(TypeNames.Qubit).CreatePointerType();
            this.String = context.CreateStructType(TypeNames.String).CreatePointerType();
            this.BigInt = context.CreateStructType(TypeNames.BigInt).CreatePointerType();
            this.Tuple = this.tupleHeader.CreatePointerType();
            this.Array = context.CreateStructType(TypeNames.Array).CreatePointerType();
            this.Callable = context.CreateStructType(TypeNames.Callable).CreatePointerType();

            this.FunctionSignature = context.GetFunctionType(
                context.VoidType,
                new[] { this.Tuple, this.Tuple, this.Tuple });

            this.Int = context.Int64Type;
            this.Double = context.DoubleType;
            this.Bool = context.BoolType;
            this.Pauli = context.GetIntType(2);
        }

        // public members

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains the given items.
        /// Values of this type always contain a tuple header as the first item and are
        /// always passed as a pointer to that item.
        /// </summary>
        public IStructType CreateConcreteTupleType(IEnumerable<ITypeRef> items) =>
            this.context.CreateStructType(false, items.Prepend(this.tupleHeader).ToArray());

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains the given items.
        /// Values of this type always contain a tuple header as the first item
        /// and are always passed as a pointer to that item.
        /// </summary>
        public IStructType CreateConcreteTupleType(params ITypeRef[] items) =>
            this.CreateConcreteTupleType((IEnumerable<ITypeRef>)items);

        /// <summary>
        /// Determines whether an LLVM type is a pointer to a tuple.
        /// Tuple values in QIR always contain a tuple header as the first item.
        /// </summary>
        public bool IsTupleType(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Members.Count > 0
            && st.Members[0] == this.tupleHeader;
    }

    /// <summary>
    /// Contains the names of all LLVM structures used to represent types in QIR.
    /// </summary>
    public static class TypeNames
    {
        public const string Int = "Int";
        public const string Double = "Double";
        public const string Bool = "Bool";
        public const string Pauli = "Pauli";

        // names of used structs

        public const string Callable = "Callable";
        public const string Result = "Result";
        public const string Qubit = "Qubit";
        public const string Range = "Range";
        public const string BigInt = "BigInt";
        public const string String = "String";
        public const string Array = "Array";
        public const string Tuple = "TupleHeader";

        // There is no separate struct type for Tuple,
        // since within QIR, there is not type distinction between tuples with different item types or number of items.
        // Using the TupleHeader struct is hence sufficient to ensure this limited type safety.
        // The Tuple pointer is hence simply a pointer to the tuple header and no additional separate struct exists.
    }
}
