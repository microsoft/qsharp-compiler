// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <cstdint>
#include <cstdarg> // for va_list

#include "CoreTypes.hpp"
#include "QirTypes.hpp"

extern "C"
{
    // ------------------------------------------------------------------------
    // Qubit Management.
    // ------------------------------------------------------------------------

    // Allocate one qubit. Qubit is guaranteed to be in |0> state.
    // Qubit needs to be released via __quantum__rt__qubit_release.
    QIR_SHARED_API QUBIT* __quantum__rt__qubit_allocate(); // NOLINT

    // Allocate 'count' qubits, allocate and return an array that owns these qubits.
    // Array and qubits in the array need to be released via __quantum__rt__qubit_release_array.
    QIR_SHARED_API QirArray* __quantum__rt__qubit_allocate_array(int64_t count); // NOLINT

    // Release one qubit.
    QIR_SHARED_API void __quantum__rt__qubit_release(QUBIT*); // NOLINT

    // Release qubits, owned by the array and the array itself.
    QIR_SHARED_API void __quantum__rt__qubit_release_array(QirArray*); // NOLINT

    // Borrow one qubit. Qubit is not guaranteed to be in |0> state.
    // Qubit needs to be returned via __quantum__rt__qubit_return in the same state in which it was borrowed.
    QIR_SHARED_API QUBIT* __quantum__rt__qubit_borrow(); // NOLINT

    // Borrow 'count' qubits, allocate and return an array that owns these qubits.
    // Array and qubits in the array need to be returned via __quantum__rt__qubit_return_array.
    QIR_SHARED_API QirArray* __quantum__rt__qubit_borrow_array(int64_t count); // NOLINT

    // Return one borrowed qubit. Qubit must be in the same state in which it was borrowed.
    QIR_SHARED_API void __quantum__rt__qubit_return(QUBIT*); // NOLINT

    // Return borrowed qubits owned by the array. Release array itself.
    QIR_SHARED_API void __quantum__rt__qubit_return_array(QirArray*); // NOLINT

    // ------------------------------------------------------------------------
    // Qubit Management Restricted Reuse Control.
    // ------------------------------------------------------------------------

    // Start restricted reuse area.
    // Qubits released within one segment of an area cannot be reused in other segments of the same area.
    QIR_SHARED_API void __quantum__rt__qubit_restricted_reuse_area_start(); // NOLINT

    // End current restricted reuse segment and start the next one within the current area.
    QIR_SHARED_API void __quantum__rt__qubit_restricted_reuse_segment_next(); // NOLINT

    // End current restricted reuse area.
    QIR_SHARED_API void __quantum__rt__qubit_restricted_reuse_area_end(); // NOLINT

    // ------------------------------------------------------------------------
    // Utils
    // ------------------------------------------------------------------------

    // Allocate a block of memory on the heap.
    QIR_SHARED_API char* __quantum__rt__heap_alloc(uint64_t size); // NOLINT

    // Release a block of allocated heap memory.
    QIR_SHARED_API void __quantum__rt__heap_free(char* buffer); // NOLINT

    // Returns a pointer to the malloc-allocated block.
    QIR_SHARED_API char* __quantum__rt__memory_allocate(uint64_t size); // NOLINT

    // Fail the computation with the given error message.
    [[noreturn]] QIR_SHARED_API void __quantum__rt__fail(QirString* msg);       // NOLINT
    [[noreturn]] QIR_SHARED_API void __quantum__rt__fail_cstr(const char* msg); // NOLINT

    // Include the given message in the computation's execution log or equivalent.
    QIR_SHARED_API void __quantum__rt__message(QirString* msg); // NOLINT

    // ------------------------------------------------------------------------
    // Results
    // ------------------------------------------------------------------------

    // Returns true if the two results are the same, and false if they are different.
    QIR_SHARED_API bool __quantum__rt__result_equal(RESULT*, RESULT*); // NOLINT

    // Adds the given integer value to the reference count for the result. Deallocates the result if the reference count
    // becomes 0. The behavior is undefined if the reference count becomes negative.
    QIR_SHARED_API void __quantum__rt__result_update_reference_count(RESULT*, int32_t); // NOLINT

    QIR_SHARED_API RESULT* __quantum__rt__result_get_one();  // NOLINT
    QIR_SHARED_API RESULT* __quantum__rt__result_get_zero(); // NOLINT

    // ------------------------------------------------------------------------
    // Tuples
    // ------------------------------------------------------------------------

    // Allocates space for a tuple requiring the given number of bytes and sets the reference count to 1.
    QIR_SHARED_API PTuple __quantum__rt__tuple_create(int64_t); // NOLINT

    // Adds the given integer value to the reference count for the tuple. Deallocates the tuple if the reference count
    // becomes 0. The behavior is undefined if the reference count becomes negative.
    QIR_SHARED_API void __quantum__rt__tuple_update_reference_count(PTuple, int32_t); // NOLINT

    // Adds the given integer value to the alias count for the tuple. Fails if the count becomes negative.
    QIR_SHARED_API void __quantum__rt__tuple_update_alias_count(PTuple, int32_t); // NOLINT

    // Creates a shallow copy of the tuple if the user count is larger than 0 or the second argument is `true`.
    QIR_SHARED_API PTuple __quantum__rt__tuple_copy(PTuple, bool force); // NOLINT

    // ------------------------------------------------------------------------
    // Arrrays
    // ------------------------------------------------------------------------

    // Creates a new 1-dimensional array. The int is the size of each element in bytes. The int64_t is the length
    // of the array. The bytes of the new array should be set to zero.
    QIR_SHARED_API QirArray* __quantum__rt__array_create_1d(int32_t, int64_t); // NOLINT

    // Adds the given integer value to the reference count for the array. Deallocates the array if the reference count
    // becomes 0. The behavior is undefined if the reference count becomes negative.
    QIR_SHARED_API void __quantum__rt__array_update_reference_count(QirArray*, int32_t); // NOLINT

    // Adds the given integer value to the alias count for the array. Fails if the count becomes negative.
    QIR_SHARED_API void __quantum__rt__array_update_alias_count(QirArray*, int32_t); // NOLINT

    // Creates a shallow copy of the array if the user count is larger than 0 or the second argument is `true`.
    QIR_SHARED_API QirArray* __quantum__rt__array_copy(QirArray*, bool); // NOLINT

    // Returns a new array which is the concatenation of the two passed-in arrays.
    QIR_SHARED_API QirArray* __quantum__rt__array_concatenate(QirArray*, QirArray*); // NOLINT

    // Returns the length of a dimension of the array. The int is the zero-based dimension to return the length of; it
    // must be 0 for a 1-dimensional array.
    QIR_SHARED_API int64_t __quantum__rt__array_get_size(QirArray*, int32_t); // NOLINT
    QIR_SHARED_API int64_t __quantum__rt__array_get_size_1d(QirArray*);       // NOLINT

    // Returns a pointer to the element of the array at the zero-based index given by the int64_t.
    QIR_SHARED_API char* __quantum__rt__array_get_element_ptr_1d(QirArray*, int64_t); // NOLINT

    // Creates a new array. The first int is the size of each element in bytes. The second int is the dimension count.
    // The variable arguments should be a sequence of int64_ts contains the length of each dimension. The bytes of the
    // new array should be set to zero.
    QIR_SHARED_API QirArray* __quantum__rt__array_create(int, int, ...); // NOLINT
    QIR_SHARED_API QirArray* __quantum__rt__array_create_nonvariadic(    // NOLINT
        int itemSizeInBytes, int countDimensions, va_list dims);

    // Returns the number of dimensions in the array.
    QIR_SHARED_API int32_t __quantum__rt__array_get_dim(QirArray*); // NOLINT

    // Returns a pointer to the indicated element of the array. The variable arguments should be a sequence of int64_ts
    // that are the indices for each dimension.
    QIR_SHARED_API char* __quantum__rt__array_get_element_ptr(QirArray*, ...);                      // NOLINT
    QIR_SHARED_API char* __quantum__rt__array_get_element_ptr_nonvariadic(QirArray*, va_list dims); // NOLINT

    // Creates and returns an array that is a slice of an existing array. The int indicates which dimension
    // the slice is on. The %Range specifies the slice.
    QIR_SHARED_API QirArray* quantum__rt__array_slice(QirArray*, int32_t, const QirRange&, // NOLINT
                                                      bool /*ignored: forceNewInstance*/);

    // Creates and returns an array that is a projection of an existing array. The int indicates which dimension the
    // projection is on, and the int64_t specifies the specific index value to project. The returned Array* will have
    // one fewer dimension than the existing array.
    QIR_SHARED_API QirArray* __quantum__rt__array_project(QirArray*, int32_t, int64_t); // NOLINT

    // ------------------------------------------------------------------------
    // Callables
    // ------------------------------------------------------------------------

    // Initializes the callable with the provided function table and capture tuple. The capture tuple pointer
    // should be null if there is no capture.
    QIR_SHARED_API QirCallable* __quantum__rt__callable_create(t_CallableEntry*, t_CaptureCallback*, PTuple); // NOLINT

    // Adds the given integer value to the reference count for the callable. Deallocates the callable if the reference
    // count becomes 0. The behavior is undefined if the reference count becomes negative.
    QIR_SHARED_API void __quantum__rt__callable_update_reference_count(QirCallable*, int32_t); // NOLINT

    // Adds the given integer value to the alias count for the callable. Fails if the count becomes negative.
    QIR_SHARED_API void __quantum__rt__callable_update_alias_count(QirCallable*, int32_t); // NOLINT

    // Creates a shallow copy of the callable if the alias count is larger than 0 or the second argument is `true`.
    // Returns the given callable pointer otherwise, after increasing its reference count by 1.
    QIR_SHARED_API QirCallable* __quantum__rt__callable_copy(QirCallable*, bool); // NOLINT

    // Invokes the callable with the provided argument tuple and fills in the result tuple.
    QIR_SHARED_API void __quantum__rt__callable_invoke(QirCallable*, PTuple, PTuple); // NOLINT

    // Updates the callable by applying the Adjoint functor.
    QIR_SHARED_API void __quantum__rt__callable_make_adjoint(QirCallable*); // NOLINT

    // Updates the callable by applying the Controlled functor.
    QIR_SHARED_API void __quantum__rt__callable_make_controlled(QirCallable*); // NOLINT

    // Invokes the function in the corresponding index in the memory management table of the callable with the capture
    // tuple and the given 32-bit integer. Does nothing if  the memory management table pointer or the function pointer
    // at that index is null.
    QIR_SHARED_API void __quantum__rt__capture_update_reference_count(QirCallable*, int32_t); // NOLINT
    QIR_SHARED_API void __quantum__rt__capture_update_alias_count(QirCallable*, int32_t);     // NOLINT

    // ------------------------------------------------------------------------
    // Strings
    // ------------------------------------------------------------------------

    // Creates a string from an array of UTF-8 bytes.
    // TODO the provided constructor doesn't match the spec!
    // QIR_SHARED_API QirString* __quantum__rt__string_create(int, char*); // NOLINT
    QIR_SHARED_API QirString* __quantum__rt__string_create(const char*); // NOLINT

    // Adds the given integer value to the reference count for the string. Deallocates the string if the reference count
    // becomes 0. The behavior is undefined if the reference count becomes negative.
    QIR_SHARED_API void __quantum__rt__string_update_reference_count(QirString*, int32_t); // NOLINT

    // Creates a new string that is the concatenation of the two argument strings.
    QIR_SHARED_API QirString* __quantum__rt__string_concatenate(QirString*, QirString*); // NOLINT

    // Returns true if the two strings are equal, false otherwise.
    QIR_SHARED_API bool __quantum__rt__string_equal(QirString*, QirString*); // NOLINT

    // Returns a string representation of the integer.
    QIR_SHARED_API QirString* __quantum__rt__int_to_string(int64_t); // NOLINT

    // Returns a string representation of the double.
    QIR_SHARED_API QirString* __quantum__rt__double_to_string(double); // NOLINT

    // Returns a string representation of the Boolean.
    QIR_SHARED_API QirString* __quantum__rt__bool_to_string(bool); // NOLINT

    // Returns a string representation of the result.
    QIR_SHARED_API QirString* __quantum__rt__result_to_string(RESULT*); // NOLINT

    // Returns a string representation of the Pauli.
    QIR_SHARED_API QirString* __quantum__rt__pauli_to_string(PauliId); // NOLINT

    // Returns a string representation of the qubit.
    QIR_SHARED_API QirString* __quantum__rt__qubit_to_string(QUBIT*); // NOLINT

    // Returns a string representation of the range.
    QIR_SHARED_API QirString* quantum__rt__range_to_string(const QirRange&); // NOLINT

    // Returns a pointer to an array that contains a null-terminated sequence of characters
    // (i.e., a C-string) representing the current value of the string object.
    QIR_SHARED_API const char* __quantum__rt__string_get_data(QirString* str); // NOLINT

    // Returns the length of the string, in terms of bytes.
    // http://www.cplusplus.com/reference/string/string/size/
    QIR_SHARED_API uint32_t __quantum__rt__string_get_length(QirString* str); // NOLINT

    // Returns a string representation of the big integer.
    // TODO QIR_SHARED_API QirString* __quantum__rt__bigint_to_string(QirBigInt*); // NOLINT

    // ------------------------------------------------------------------------
    // BigInts
    // ------------------------------------------------------------------------

    // Creates a big integer with the specified initial value.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_create_int64_t(int64_t); // NOLINT

    // Creates a big integer with the initial value specified by the i8 array. The 0-th element of the array is the
    // highest-order byte, followed by the first element, etc.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_create_array(int, char*); // NOLINT

    // Adds the given integer value to the reference count for the big integer. Deallocates the big integer if the
    // reference count becomes 0. The behavior is undefined if the reference count becomes negative.
    // TODO QIR_SHARED_API void __quantum__rt__bigint_update_reference_count(QirBigInt*, int32_t); // NOLINT

    // Returns the negative of the big integer.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_negate(QirBigInt*); // NOLINT

    // Adds two big integers and returns their sum.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_add(QirBigInt*, QirBigInt*); // NOLINT

    // Subtracts the second big integer from the first and returns their difference.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_subtract(QirBigInt*, QirBigInt*); // NOLINT

    // Multiplies two big integers and returns their product.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_multiply(QirBigInt*, QirBigInt*); // NOLINT

    // Divides the first big integer by the second and returns their quotient.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_divide(QirBigInt*, QirBigInt*); // NOLINT

    // Returns the first big integer modulo the second.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_modulus(QirBigInt*, QirBigInt*); // NOLINT

    // Returns the big integer raised to the integer power.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_power(QirBigInt*, int); // NOLINT

    // Returns the bitwise-AND of two big integers.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_bitand(QirBigInt*, QirBigInt*); // NOLINT

    // Returns the bitwise-OR of two big integers.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_bitor(QirBigInt*, QirBigInt*); // NOLINT

    // Returns the bitwise-XOR of two big integers.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_bitxor(QirBigInt*, QirBigInt*); // NOLINT

    // Returns the bitwise complement of the big integer.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_bitnot(QirBigInt*); // NOLINT

    // Returns the big integer arithmetically shifted left by the integer amount of bits.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_shiftleft(QirBigInt*, int64_t); // NOLINT

    // Returns the big integer arithmetically shifted right by the integer amount of bits.
    // TODO QIR_SHARED_API QirBigInt* __quantum__rt__bigint_shiftright(QirBigInt*, int64_t); // NOLINT

    // Returns true if the two big integers are equal, false otherwise.
    // TODO QIR_SHARED_API bool __quantum__rt__bigint_equal(QirBigInt*, QirBigInt*); // NOLINT

    // Returns true if the first big integer is greater than the second, false otherwise.
    // TODO QIR_SHARED_API bool __quantum__rt__bigint_greater(QirBigInt*, QirBigInt*); // NOLINT

    // Returns true if the first big integer is greater than or equal to the second, false otherwise.
    // TODO QIR_SHARED_API bool __quantum__rt__bigint_greater_eq(QirBigInt*, QirBigInt*); // NOLINT
}

// TODO(rokuzmin): Consider separating the `extern "C"` exports and C++ exports.
namespace Microsoft // Replace with `namespace Microsoft::Quantum` after migration to C++17.
{
namespace Quantum
{
    // Deprecated, use `Microsoft::Quantum::OutputStream::ScopedRedirector` or `Microsoft::Quantum::OutputStream::Set()`
    // instead.
    QIR_SHARED_API std::ostream& SetOutputStream(std::ostream& newOStream);
} // namespace Quantum
} // namespace Microsoft
