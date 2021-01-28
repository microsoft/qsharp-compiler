// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QIR
{
    /// <summary>
    /// Static class that contains common conventions for the QIR runtime library.
    /// </summary>
    public static class RuntimeLibrary
    {
        // int functions
        public const string IntPower = "int_power";

        // result functions
        public const string ResultUpdateReferenceCount = "result_update_reference_count";
        public const string ResultEqual = "result_equal";

        // string functions
        public const string StringCreate = "string_create";
        public const string StringUpdateReferenceCount = "string_update_reference_count";
        public const string StringConcatenate = "string_concatenate";
        public const string StringEqual = "string_equal";

        // to-string
        public const string BigIntToString = "bigint_to_string";
        public const string BoolToString = "bool_to_string";
        public const string DoubleToString = "double_to_string";
        public const string IntToString = "int_to_string";
        public const string PauliToString = "pauli_to_string";
        public const string QubitToString = "qubit_to_string";
        public const string RangeToString = "range_to_string";
        public const string ResultToString = "result_to_string";

        // bigint functions
        public const string BigIntCreateI64 = "bigint_create_i64";
        public const string BigIntCreateArray = "bigint_create_array";
        public const string BigIntUpdateReferenceCount = "bigint_update_reference_count";
        public const string BigIntNegate = "bigint_negate";
        public const string BigIntAdd = "bigint_add";
        public const string BigIntSubtract = "bigint_subtract";
        public const string BigIntMultiply = "bigint_multiply";
        public const string BigIntDivide = "bigint_divide";
        public const string BigIntModulus = "bigint_modulus";
        public const string BigIntPower = "bigint_power";
        public const string BigIntBitwiseAnd = "bigint_bitand";
        public const string BigIntBitwiseOr = "bigint_bitor";
        public const string BigIntBitwiseXor = "bigint_bitxor";
        public const string BigIntBitwiseNot = "bigint_bitnot";
        public const string BigIntShiftLeft = "bigint_shiftleft";
        public const string BigIntShiftRight = "bigint_shiftright";
        public const string BigIntEqual = "bigint_equal";
        public const string BigIntGreater = "bigint_greater";
        public const string BigIntGreaterEq = "bigint_greater_eq";

        // tuple functions
        public const string TupleCreate = "tuple_create";
        public const string TupleUpdateAliasCount = "tuple_update_alias_count";
        public const string TupleUpdateReferenceCount = "tuple_update_reference_count";
        public const string TupleCopy = "tuple_copy";

        // array functions
        public const string ArrayCreate = "array_create";
        public const string ArrayGetElementPtr = "array_get_element_ptr";
        // TODO: figure out how to call a varargs function and get rid of these two functions
        public const string ArrayCreate1d = "array_create_1d";
        public const string ArrayGetElementPtr1d = "array_get_element_ptr_1d";
        public const string ArrayUpdateAliasCount = "array_update_alias_count";
        public const string ArrayUpdateReferenceCount = "array_update_reference_count";
        public const string ArrayCopy = "array_copy";
        public const string ArrayConcatenate = "array_concatenate";
        public const string ArraySlice = "array_slice";
        public const string ArraySlice1d = "array_slice_1d";
        public const string ArrayGetSize1d = "array_get_size_1d";

        // callable-related
        public const string CallableCreate = "callable_create";
        public const string CallableInvoke = "callable_invoke";
        public const string CallableCopy = "callable_copy";
        public const string CallableMakeAdjoint = "callable_make_adjoint";
        public const string CallableMakeControlled = "callable_make_controlled";
        public const string CallableUpdateAliasCount = "callable_update_alias_count";
        public const string CallableUpdateReferenceCount = "callable_update_reference_count";
        public const string CallableMemoryManagement = "callable_memory_management";

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
