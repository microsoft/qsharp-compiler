// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR
{
    public class Types
    {
        public readonly ITypeRef Int;
        public readonly ITypeRef Double;
        public readonly ITypeRef Bool;
        public readonly ITypeRef Pauli;

        public readonly IPointerType Result;
        public readonly IPointerType Qubit;
        public readonly IPointerType String;
        public readonly IPointerType BigInt;
        public readonly IPointerType Tuple;
        public readonly IPointerType Array;
        public readonly IPointerType Callable;

        public readonly IStructType Range;
        public readonly IStructType TupleHeader;

        /// <summary>
        /// Represents the signature of a callable specialization in QIR.
        /// It takes three tuples (pointers) as input and returns void. The inputs are
        /// a tuple containing all captures values,
        /// a tuple containing all arguments,
        /// and a tuple where the output will be stored.
        /// </summary>
        public readonly IFunctionType FunctionSignature;

        internal Types(Context context)
        {
            this.Int = context.Int64Type;
            this.Double = context.DoubleType;
            this.Bool = context.BoolType;
            this.Pauli = context.GetIntType(2);

            this.Range = context.CreateStructType("Range", false, context.Int64Type, context.Int64Type, context.Int64Type);
            // It would be nice if TupleHeader were opaque, but it can't be because it appears directly
            // (that is, not as a pointer) in tuple structures, but would have unknown length if it were opaque.
            this.TupleHeader = context.CreateStructType("TupleHeader", false, context.Int32Type);

            this.Result = context.CreateStructType("Result").CreatePointerType();
            this.Qubit = context.CreateStructType("Qubit").CreatePointerType();
            this.String = context.CreateStructType("String").CreatePointerType();
            this.BigInt = context.CreateStructType("BigInt").CreatePointerType();
            this.Tuple = this.TupleHeader.CreatePointerType();
            this.Array = context.CreateStructType("Array").CreatePointerType();
            this.Callable = context.CreateStructType("Callable").CreatePointerType();

            this.FunctionSignature = context.GetFunctionType(
                context.VoidType,
                new[] { this.Tuple, this.Tuple, this.Tuple });
        }
    }

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

    public enum Component
    {
        RuntimeLibrary,
        QuantumInstructionSet
    }

    public static class Callables
    {
        /// <summary>
        /// Generates a mangled name for a special function.
        /// Special functions are either part of the QIR runtime library or part of the target-specified
        /// quantum instruction set.
        /// The mangled names are a double underscore, "quantum", and another double underscore, followed by
        /// "rt" or "qis", another double underscore, and then the base name.
        /// </summary>
        /// <param name="kind">The kind of special function</param>
        /// <param name="name">The name of the special function</param>
        /// <returns>The mangled function name</returns>
        /// <exception cref="ArgumentException">No naming convention is defined for the given component.</exception>
        public static string FunctionName(Component kind, string name)
        {
            return kind switch
            {
                Component.RuntimeLibrary => $"__quantum__rt__{name}",
                Component.QuantumInstructionSet => $"__quantum__qis__{name}",
                _ => throw new ArgumentException("unkown software component"),
            };
        }
    }

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
