// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

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
        /// Such a concrete tuple type is constructed using <see cref="TypedTuple"/>.
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

        // private and internal fields

        private readonly Context context;

        internal readonly IArrayType CallableTable;
        internal readonly IFunctionType CaptureCountFunction;
        internal readonly IArrayType CallableMemoryManagementTable;

        // constructor

        internal Types(Context context)
        {
            this.context = context;

            this.Int = context.Int64Type;
            this.Double = context.DoubleType;
            this.Bool = context.BoolType;
            this.Pauli = context.GetIntType(2);
            this.Range = context.CreateStructType(TypeNames.Range, false, context.Int64Type, context.Int64Type, context.Int64Type);

            this.Result = context.CreateStructType(TypeNames.Result).CreatePointerType();
            this.Qubit = context.CreateStructType(TypeNames.Qubit).CreatePointerType();
            this.String = context.CreateStructType(TypeNames.String).CreatePointerType();
            this.BigInt = context.CreateStructType(TypeNames.BigInt).CreatePointerType();
            this.Tuple = context.CreateStructType(TypeNames.Tuple).CreatePointerType();
            this.Array = context.CreateStructType(TypeNames.Array).CreatePointerType();
            this.Callable = context.CreateStructType(TypeNames.Callable).CreatePointerType();

            this.FunctionSignature = context.GetFunctionType(context.VoidType, this.Tuple, this.Tuple, this.Tuple);
            this.CallableTable = this.FunctionSignature.CreatePointerType().CreateArrayType(4);
            this.CaptureCountFunction = context.GetFunctionType(context.VoidType, this.Tuple, this.Int);
            this.CallableMemoryManagementTable = this.CaptureCountFunction.CreatePointerType().CreateArrayType(2);
        }

        // internal helpers to simplify common code

        /// <summary>
        /// Type by which data allocated as global constant array is passed to the runtime.
        /// String and big integers for example are instantiated with a data array.
        /// </summary>
        internal IPointerType DataArrayPointer =>
            this.context.Int8Type.CreatePointerType();

        /// <summary>
        /// Given the type of a pointer to a struct, returns the type of the struct.
        /// This method thus is the inverse mapping of CreatePointerType.
        /// Throws an argument exception if the given type is not a pointer to a struct.
        /// </summary>
        internal static IStructType StructFromPointer(ITypeRef pointer) =>
            pointer is IPointerType pt && pt.ElementType is IStructType st ? st :
            throw new ArgumentException("the given argument is not a pointer to a struct");

        /// <summary>
        /// Given a pointer, returns the type of the value it points to.
        /// Casts the type of the given value to an IPointerType in the process,
        /// throwing the corresponding exception if the cast fails.
        /// </summary>
        internal static ITypeRef PointerElementType(Value pointer) =>
            ((IPointerType)pointer.NativeType).ElementType;

        // public members

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains the given items.
        /// </summary>
        public IStructType TypedTuple(params Value[] items) =>
            this.context.CreateStructType(false, items.Select(v => v.NativeType).ToArray());

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains items of the given types.
        /// </summary>
        public IStructType TypedTuple(IEnumerable<ITypeRef> items) =>
            this.context.CreateStructType(false, items.ToArray());

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains items of the given types.
        /// </summary>
        public IStructType TypedTuple(params ITypeRef[] items) =>
            this.context.CreateStructType(false, items);

        /// <summary>
        /// Determines whether an LLVM type is a pointer to a typed tuple.
        /// </summary>
        public static bool IsTypedTuple(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == null
            && st.Members.Count > 0;

        /// <summary>
        /// Determines whether an LLVM type is a pointer to an opaque tuple.
        /// </summary>
        public static bool IsTuple(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.Tuple;

        /// <summary>
        /// Determines whether an LLVM type is a pointer to an opaque array.
        /// </summary>
        public static bool IsArray(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.Array;

        /// <summary>
        /// Determines whether an LLVM type is a pointer to an opaque callable.
        /// </summary>
        public static bool IsCallable(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.Callable;

        /// <summary>
        /// Determines whether an LLVM type is a qubit pointer.
        /// </summary>
        public static bool IsQubit(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.Qubit;

        /// <summary>
        /// Determines whether an LLVM type is a measurement result pointer.
        /// </summary>
        public static bool IsResult(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.Result;

        /// <summary>
        /// Determines whether an LLVM type is a big int pointer.
        /// </summary>
        public static bool IsBigInt(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.BigInt;

        /// <summary>
        /// Determines whether an LLVM type is a string pointer.
        /// </summary>
        public static bool IsString(ITypeRef t) =>
            t is IPointerType pt
            && pt.ElementType is IStructType st
            && st.Name == TypeNames.String;
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
        public const string Tuple = "Tuple";
    }
}
