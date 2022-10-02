// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using LlvmBindings;
using LlvmBindings.Types;
using LlvmBindings.Values;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

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
        public ITypeRef Int { get; }

        /// <summary>
        /// Represents the type of a double precision floating point number in QIR.
        /// </summary>
        public ITypeRef Double { get; }

        /// <summary>
        /// Represents the type of a boolean value in QIR.
        /// </summary>
        public ITypeRef Bool { get; }

        /// <summary>
        /// Represents the type of a single-qubit Pauli matrix in QIR
        /// used to indicate e.g. the basis of a quantum measurement.
        /// The type is a two-bit integer type.
        /// </summary>
        public ITypeRef Pauli { get; }

        /// <summary>
        /// Represents the type of a result value from a quantum measurement in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public IPointerType Result { get; }

        /// <summary>
        /// Represents the type of a string value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public IPointerType Qubit { get; }

        /// <summary>
        /// Represents the type of a string value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public IPointerType String { get; }

        /// <summary>
        /// Represents the type of a big integer value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public IPointerType BigInt { get; }

        /// <summary>
        /// Represents the type of an array in QIR.
        /// The type is a pointer to an opaque struct.
        /// Item access is provided by the runtime library.
        /// The library method(s) return byte pointers
        /// that need to be cast to the appropriate type.
        /// </summary>
        public IPointerType Array { get; }

        /// <summary>
        /// Represents the type of a tuple value in QIR.
        /// The type is a pointer to an opaque struct.
        /// For item access and deconstruction, tuple values need to be cast
        /// to a suitable concrete type depending on the types of their items.
        /// Such a concrete tuple type is constructed using <see cref="TypedTuple(Value[])"/>.
        /// </summary>
        public IPointerType Tuple { get; }

        /// <summary>
        /// Represents the type of a callable value in QIR.
        /// The type is a pointer to an opaque struct.
        /// </summary>
        public IPointerType Callable { get; }

        /// <summary>
        /// Represents the signature of a callable specialization in QIR.
        /// It takes three tuples (pointers) as input and returns void. The inputs are
        /// a tuple containing all captures values,
        /// a tuple containing all arguments,
        /// and a tuple where the output will be stored.
        /// </summary>
        public IFunctionType FunctionSignature { get; }

        /// <summary>
        /// Represents the type of a range of numbers defined by a start, step, and end value.
        /// The type is a named struct that contains three 64-bit integers.
        /// </summary>
        public IStructType Range { get; }

        /// <summary>
        /// Type by which data allocated as global constant array is passed to the runtime.
        /// String and big integers for example are instantiated with a data array.
        /// </summary>
        internal IPointerType DataArrayPointer { get; }

        /// <summary>
        /// Generic pointer used to pass values of unknown type that need to be cast before use.
        /// </summary>
        internal IPointerType BytePointer { get; }

        /* private and internal fields */

        internal IArrayType CallableTable { get; }

        internal IFunctionType CaptureCountFunction { get; }

        internal IArrayType CallableMemoryManagementTable { get; }

        internal QirTypeTransformation Transform { get; }

        private readonly Context context;

        private readonly Dictionary<string, IStructType> namedStructs;

        internal Types(Context context, Func<QsQualifiedName, QsCustomType?> getTypeDecl, bool useNamedLlvmArrays)
        {
            this.context = context;
            this.namedStructs = new Dictionary<string, IStructType>();
            this.Transform = new QirTypeTransformation(this, getTypeDecl, useNamedLlvmArrays);

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

            this.DataArrayPointer = context.Int8Type.CreatePointerType();
            this.BytePointer = context.Int8Type.CreatePointerType();

            this.FunctionSignature = context.GetFunctionType(context.VoidType, this.Tuple, this.Tuple, this.Tuple);
            this.CallableTable = this.FunctionSignature.CreatePointerType().CreateArrayType(4);
            this.CaptureCountFunction = context.GetFunctionType(context.VoidType, this.Tuple, context.Int32Type);
            this.CallableMemoryManagementTable = this.CaptureCountFunction.CreatePointerType().CreateArrayType(2);
        }

        // internal helpers to simplify common code

        /// <summary>
        /// Creates a struct type with the given name and element types if it does not already exist.
        /// If a struct with the given name already exists, the existing type is returned without validating the element types.
        /// </summary>
        private IStructType NamedStruct(string name, params ITypeRef[] elementTypes)
        {
            if (!this.namedStructs.TryGetValue(name, out var type))
            {
                type = this.context.CreateStructType(name, false, elementTypes);
                this.namedStructs.Add(name, type);
            }

            return type;
        }

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

        /// <summary>
        /// Creates the LLVM data type used for stack allocated arrays.
        /// </summary>
        /// <remarks>
        /// We use a struct containing an i64 and a struct to store the actual length of the array as well as the array itself.
        /// For the array itself, we choose a struct with a single entry that is a constant array.
        /// This permits us to both name the type (which can only be done for structs and not for constant arrays),
        /// such that we can easily edit the size of the type once it is known (relevant for command line arguments),
        /// while simultaneously access and update items via getElementPrt if needed
        /// (needed when the accurate length is not know for the initial IR emission).
        /// </remarks>
        internal IStructType NativeArray(ITypeRef elementType, uint nrElements, int? id = null)
        {
            var constArrType = elementType.CreateArrayType(nrElements);
            var storageType = id is null ? this.TypedTuple(constArrType) : this.NamedStruct($"ArrayStorage{id}", constArrType);
            return this.TypedTuple(this.Int, storageType);
        }

        // FIXME: get rid of this and instead simply have another "constructor"?
        internal void ArrayStorageTypeInfo(IStructType arrayStorageType, out ITypeRef elementType, out uint nrElements, out int? id)
        {
            var constArrType = (IArrayType)arrayStorageType.Members[0];
            elementType = constArrType.ElementType;
            nrElements = constArrType.Length;
            id = !string.IsNullOrEmpty(arrayStorageType.Name) && int.TryParse(arrayStorageType.Name["ArrayStorage".Length..], out var parsed) ? parsed : null;
        }

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
            && string.IsNullOrEmpty(st.Name)
            && st.Members.Count > 0;

        /// <summary>
        /// Determines whether an LLVM type is a pointer to an opaque tuple.
        /// </summary>
        public static bool IsTupleOrUnit(ITypeRef t) =>
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

        /// <summary>
        /// Determines whether an LLVM type is a struct type representing a range of integers.
        /// </summary>
        public static bool IsRange(ITypeRef t) =>
            t is IStructType st
            && st.Name == TypeNames.Range;
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

    /// <summary>
    /// Contains the names of common QIR attributes.
    /// </summary>
    public static class AttributeNames
    {
        public const string EntryPoint = "EntryPoint";
        public const string InteropFriendly = "InteropFriendly";
    }
}
