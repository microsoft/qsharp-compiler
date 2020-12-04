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

    /// <summary>
    /// Each class instance contains the QIR constants defined and used
    /// within the compilation unit given upon instantiation.
    /// </summary>
    public class Constants
    {
        public readonly Value ResultZero;
        public readonly Value ResultOne;
        public readonly Value PauliI;
        public readonly Value PauliX;
        public readonly Value PauliY;
        public readonly Value PauliZ;
        public readonly Value EmptyRange;

        internal Constants(Context context, BitcodeModule module, Types types)
        {
            Value CreatePauli(string name, ulong idx) =>
                module.AddGlobal(types.Pauli, true, Linkage.External, context.CreateConstant(types.Pauli, idx, false), name);

            this.ResultZero = module.AddGlobal(types.Result, "ResultZero");
            this.ResultOne = module.AddGlobal(types.Result, "ResultOne");
            this.PauliI = CreatePauli("PauliI", 0);
            this.PauliX = CreatePauli("PauliX", 1);
            this.PauliY = CreatePauli("PauliY", 3);
            this.PauliZ = CreatePauli("PauliZ", 2);
            this.EmptyRange = module.AddGlobal(
                types.Range,
                true,
                Linkage.Internal,
                context.CreateNamedConstantStruct(
                    types.Range,
                    context.CreateConstant(0L),
                    context.CreateConstant(1L),
                    context.CreateConstant(-1L)),
                "EmptyRange");
        }
    }

    /// <summary>
    /// Enum to distinguish different components that are ultimately combined
    /// to execute a program compiled into QIR.
    /// </summary>
    public enum Component
    {
        /// <summary>
        /// Contains all functions that are supported by the (classical) QIR runtime.
        /// </summary>
        RuntimeLibrary,

        /// <summary>
        /// Contains all functions that are supported by the quantum processor itself.
        /// </summary>
        QuantumInstructionSet
    }

    /// <summary>
    /// Static class that contains common conventions for QIR functions and callable values.
    /// </summary>
    public static class Callables
    {
        /// <summary>
        /// Generates a mangled name for a function that is expected to be provided by a component,
        /// such as QIR runtime library or the quantum instruction set, rather than defined in source code.
        /// The mangled names are a double underscore, "quantum", and another double underscore, followed by
        /// "rt" or "qis", another double underscore, and then the base name.
        /// </summary>
        /// <param name="kind">The component that is expected to provide the function</param>
        /// <param name="name">The name of the function without the component prefix</param>
        /// <returns>The mangled function name</returns>
        /// <exception cref="ArgumentException">No naming convention is defined for the given component.</exception>
        public static string FunctionName(Component component, string name) => component switch
        {
            Component.RuntimeLibrary => $"__quantum__rt__{name}",
            Component.QuantumInstructionSet => $"__quantum__qis__{name}",
            _ => throw new ArgumentException("unkown software component"),
        };
    }

    /// <summary>
    /// Static class that contains common conventions for the QIR runtime library.
    /// </summary>
    public static class RuntimeLibrary
    {
        // int functions
        public const string IntPower = "int_power";

        // result functions
        public const string ResultReference = "result_reference";
        public const string ResultUnreference = "result_unreference";
        public const string ResultEqual = "result_equal";

        // string functions
        public const string StringCreate = "string_create";
        public const string StringReference = "string_reference";
        public const string StringUnreference = "string_unreference";
        public const string StringConcatenate = "string_concatenate";
        public const string StringEqual = "string_equal";

        // to-string
        public const string BigintToString = "bigint_to_string";
        public const string BoolToString = "bool_to_string";
        public const string DoubleToString = "double_to_string";
        public const string IntToString = "int_to_string";
        public const string PauliToString = "pauli_to_string";
        public const string QubitToString = "qubit_to_string";
        public const string RangeToString = "range_to_string";
        public const string ResultToString = "result_to_string";

        // bigint functions
        public const string BigintCreateI64 = "bigint_create_i64";
        public const string BigintCreateArray = "bigint_create_array";
        public const string BigintReference = "bigint_reference";
        public const string BigintUnreference = "bigint_unreference";
        public const string BigintNegate = "bigint_negate";
        public const string BigintAdd = "bigint_add";
        public const string BigintSubtract = "bigint_subtract";
        public const string BigintMultiply = "bigint_multiply";
        public const string BigintDivide = "bigint_divide";
        public const string BigintModulus = "bigint_modulus";
        public const string BigintPower = "bigint_power";
        public const string BigintBitand = "bigint_bitand";
        public const string BigintBitor = "bigint_bitor";
        public const string BigintBitxor = "bigint_bitxor";
        public const string BigintBitnot = "bigint_bitnot";
        public const string BigintShiftleft = "bigint_shiftleft";
        public const string BigintShiftright = "bigint_shiftright";
        public const string BigintEqual = "bigint_equal";
        public const string BigintGreater = "bigint_greater";
        public const string BigintGreaterEq = "bigint_greater_eq";

        // tuple functions
        public const string TupleInitStack = "tuple_init_stack";
        public const string TupleInitHeap = "tuple_init_heap";
        public const string TupleCreate = "tuple_create";
        public const string TupleReference = "tuple_reference";
        public const string TupleUnreference = "tuple_unreference";
        public const string TupleIsWritable = "tuple_is_writable";

        // array functions
        public const string ArrayCreate = "array_create";
        public const string ArrayGetElementPtr = "array_get_element_ptr";

        public const string ArrayCreate1d = "array_create_1d";
        public const string ArrayGetElementPtr1d = "array_get_element_ptr_1d";
        public const string ArrayGetLength = "array_get_length";
        public const string ArrayReference = "array_reference";
        public const string ArrayUnreference = "array_unreference";
        public const string ArrayCopy = "array_copy";
        public const string ArrayConcatenate = "array_concatenate";
        public const string ArraySlice = "array_slice";

        // callable-related
        public const string CallableCreate = "callable_create";
        public const string CallableInvoke = "callable_invoke";
        public const string CallableCopy = "callable_copy";
        public const string CallableMakeAdjoint = "callable_make_adjoint";
        public const string CallableMakeControlled = "callable_make_controlled";
        public const string CallableReference = "callable_reference";
        public const string CallableUnreference = "callable_unreference";

        // qubit functions
        public const string QubitAllocate = "qubit_allocate";
        public const string QubitAllocateArray = "qubit_allocate_array";
        public const string QubitRelease = "qubit_release";
        public const string QubitReleaseArray = "qubit_release_array";

        // diagnostics
        public const string Fail = "fail";
        public const string Message = "message";
    }
}
