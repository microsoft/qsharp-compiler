#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use inkwell::execution_engine::ExecutionEngine;
use std::ffi::CString;
use std::path::Path;
use libloading::Error;
use libloading::Library;

use crate::emit::Context;

use super::load_library;

#[repr(C)]
#[derive(Debug, Copy, Clone)]
pub struct QUBIT {
    _unused: [u8; 0],
}

#[repr(C)]
pub struct QirRTuple {}

#[repr(C)]
pub struct QirExpTuple {}

#[repr(C)]
#[derive(Debug, Copy, Clone)]
pub struct RESULT {
    _unused: [u8; 0],
}

#[repr(C)]
pub struct QirArray {}

#[repr(C)]
pub struct QirCallable {}

#[repr(C)]
pub struct QirString {}

pub(crate) struct Runtime {
    core: Library,
    foundation: Library,
    runtime: Library,
}

pub type PauliId = i8;
pub type PTuplePointedType = u8;
pub type t_CallableEntry =
    ::core::option::Option<unsafe extern "C" fn(arg1: PTuple, arg2: PTuple, arg3: PTuple)>;
pub type t_CaptureCallback = ::core::option::Option<unsafe extern "C" fn(arg1: PTuple, arg2: i32)>;

pub type PTuple = *mut PTuplePointedType;

#[repr(C)]
#[derive(Debug, Copy, Clone)]
pub struct __va_list_tag {
    pub gp_offset: cty::c_uint,
    pub fp_offset: cty::c_uint,
    pub overflow_arg_area: *mut cty::c_void,
    pub reg_save_area: *mut cty::c_void,
}

type __quantum__rt__qubit_allocate = unsafe extern "C" fn() -> *mut QUBIT;
type __quantum__rt__qubit_allocate_array = unsafe extern "C" fn(count: i64) -> *mut QirArray;
type __quantum__rt__qubit_release = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__rt__qubit_release_array = unsafe extern "C" fn(arg1: *mut QirArray);
type __quantum__rt__qubit_borrow = unsafe extern "C" fn() -> *mut QUBIT;
type __quantum__rt__qubit_borrow_array = unsafe extern "C" fn(count: i64) -> *mut QirArray;
type __quantum__rt__qubit_return = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__rt__qubit_return_array = unsafe extern "C" fn(arg1: *mut QirArray);
type __quantum__rt__qubit_restricted_reuse_area_start = unsafe extern "C" fn();
type __quantum__rt__qubit_restricted_reuse_segment_next = unsafe extern "C" fn();
type __quantum__rt__qubit_restricted_reuse_area_end = unsafe extern "C" fn();
type __quantum__rt__heap_alloc = unsafe extern "C" fn(size: u64) -> *mut cty::c_char;
type __quantum__rt__heap_free = unsafe extern "C" fn(buffer: *mut cty::c_char);
type __quantum__rt__memory_allocate = unsafe extern "C" fn(size: u64) -> *mut cty::c_char;
type __quantum__rt__fail = unsafe extern "C" fn(msg: *mut QirString);
type __quantum__rt__fail_cstr = unsafe extern "C" fn(msg: *const cty::c_char);
type __quantum__rt__message = unsafe extern "C" fn(msg: *mut QirString);
type __quantum__rt__result_equal =
    unsafe extern "C" fn(arg1: *mut RESULT, arg2: *mut RESULT) -> bool;
type __quantum__rt__result_update_reference_count =
    unsafe extern "C" fn(arg1: *mut RESULT, arg2: i32);
type __quantum__rt__result_get_one = unsafe extern "C" fn() -> *mut RESULT;
type __quantum__rt__result_get_zero = unsafe extern "C" fn() -> *mut RESULT;
type __quantum__rt__tuple_create = unsafe extern "C" fn(arg1: i64) -> PTuple;
type __quantum__rt__tuple_update_reference_count = unsafe extern "C" fn(arg1: PTuple, arg2: i32);
type __quantum__rt__tuple_update_alias_count = unsafe extern "C" fn(arg1: PTuple, arg2: i32);
type __quantum__rt__tuple_copy = unsafe extern "C" fn(arg1: PTuple, force: bool) -> PTuple;
type __quantum__rt__array_create_1d = unsafe extern "C" fn(arg1: i32, arg2: i64) -> *mut QirArray;
type __quantum__rt__array_update_reference_count =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: i32);
type __quantum__rt__array_update_alias_count = unsafe extern "C" fn(arg1: *mut QirArray, arg2: i32);
type __quantum__rt__array_copy =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: bool) -> *mut QirArray;
type __quantum__rt__array_concatenate =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QirArray) -> *mut QirArray;
type __quantum__rt__array_get_size = unsafe extern "C" fn(arg1: *mut QirArray, arg2: i32) -> i64;
type __quantum__rt__array_get_size_1d = unsafe extern "C" fn(arg1: *mut QirArray) -> i64;
type __quantum__rt__array_get_element_ptr_1d =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: i64) -> *mut cty::c_char;
type __quantum__rt__array_create =
    unsafe extern "C" fn(arg1: cty::c_int, arg2: cty::c_int, ...) -> *mut QirArray;
type __quantum__rt__array_create_nonvariadic = unsafe extern "C" fn(
    itemSizeInBytes: cty::c_int,
    countDimensions: cty::c_int,
    dims: *mut __va_list_tag,
) -> *mut QirArray;
type __quantum__rt__array_get_dim = unsafe extern "C" fn(arg1: *mut QirArray) -> i32;
type __quantum__rt__array_get_element_ptr =
    unsafe extern "C" fn(arg1: *mut QirArray, ...) -> *mut cty::c_char;
type __quantum__rt__array_get_element_ptr_nonvariadic =
    unsafe extern "C" fn(arg1: *mut QirArray, dims: *mut __va_list_tag) -> *mut cty::c_char;
type __quantum__rt__array_project =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: i32, arg3: i64) -> *mut QirArray;
type __quantum__rt__callable_create = unsafe extern "C" fn(
    arg1: *mut t_CallableEntry,
    arg2: *mut t_CaptureCallback,
    arg3: PTuple,
) -> *mut QirCallable;
type __quantum__rt__callable_update_reference_count =
    unsafe extern "C" fn(arg1: *mut QirCallable, arg2: i32);
type __quantum__rt__callable_update_alias_count =
    unsafe extern "C" fn(arg1: *mut QirCallable, arg2: i32);
type __quantum__rt__callable_copy =
    unsafe extern "C" fn(arg1: *mut QirCallable, arg2: bool) -> *mut QirCallable;
type __quantum__rt__callable_invoke =
    unsafe extern "C" fn(arg1: *mut QirCallable, arg2: PTuple, arg3: PTuple);
type __quantum__rt__callable_make_adjoint = unsafe extern "C" fn(arg1: *mut QirCallable);
type __quantum__rt__callable_make_controlled = unsafe extern "C" fn(arg1: *mut QirCallable);
type __quantum__rt__capture_update_reference_count =
    unsafe extern "C" fn(arg1: *mut QirCallable, arg2: i32);
type __quantum__rt__capture_update_alias_count =
    unsafe extern "C" fn(arg1: *mut QirCallable, arg2: i32);
type __quantum__rt__string_create =
    unsafe extern "C" fn(arg1: *const cty::c_char) -> *mut QirString;
type __quantum__rt__string_update_reference_count =
    unsafe extern "C" fn(arg1: *mut QirString, arg2: i32);
type __quantum__rt__string_concatenate =
    unsafe extern "C" fn(arg1: *mut QirString, arg2: *mut QirString) -> *mut QirString;
type __quantum__rt__string_equal =
    unsafe extern "C" fn(arg1: *mut QirString, arg2: *mut QirString) -> bool;
type __quantum__rt__int_to_string = unsafe extern "C" fn(arg1: i64) -> *mut QirString;
type __quantum__rt__double_to_string = unsafe extern "C" fn(arg1: f64) -> *mut QirString;
type __quantum__rt__bool_to_string = unsafe extern "C" fn(arg1: bool) -> *mut QirString;
type __quantum__rt__result_to_string = unsafe extern "C" fn(arg1: *mut RESULT) -> *mut QirString;
type __quantum__rt__pauli_to_string = unsafe extern "C" fn(arg1: PauliId) -> *mut QirString;
type __quantum__rt__qubit_to_string = unsafe extern "C" fn(arg1: *mut QUBIT) -> *mut QirString;
type __quantum__rt__string_get_data =
    unsafe extern "C" fn(str_: *mut QirString) -> *const cty::c_char;
type __quantum__rt__string_get_length = unsafe extern "C" fn(str_: *mut QirString) -> u32;

macro_rules! name_of_type {
    // Covers Types
    ($t: ty) => {{
        let _ = || {
            let _: $t;
        };
        stringify!($t)
    }};
}

impl Runtime {
    pub unsafe fn new<P: AsRef<Path>>(base: &P) -> Result<Runtime, Error> {
        // Must load in this order or you have to mess with LD_LIBRARY_PATH on unix
        let runtime = load_library(&base, "Microsoft.Quantum.Qir.Runtime")?;
        let core = load_library(&base, "Microsoft.Quantum.Qir.QSharp.Core")?;
        let foundation = load_library(&base, "Microsoft.Quantum.Qir.QSharp.Foundation")?;

        Ok(Runtime {
            core,
            foundation,
            runtime,
        })
    }

    pub unsafe fn map_runtime_calls<'ctx>(
        &self,
        context: &Context<'ctx>,
        ee: &ExecutionEngine<'ctx>,
    ) {
        super::intrinsics::map_intrinsic_calls(&self.core, &context, &ee);

        let qubit_allocate = self
            .runtime
            .get::<__quantum__rt__qubit_allocate>(
                CString::new(name_of_type!(__quantum__rt__qubit_allocate))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.qubit_allocate,
            *qubit_allocate as usize,
        );

        if let Some(qubit_allocate_array_fn) = context.runtime_library.qubit_allocate_array {
            let qubit_allocate_array = self
                .runtime
                .get::<__quantum__rt__qubit_allocate_array>(
                    CString::new(name_of_type!(__quantum__rt__qubit_allocate_array))
                        .unwrap()
                        .as_bytes_with_nul(),
                )
                .unwrap();
            ee.add_global_mapping(&qubit_allocate_array_fn, *qubit_allocate_array as usize);
        }

        let qubit_release = self
            .runtime
            .get::<__quantum__rt__qubit_release>(
                CString::new(name_of_type!(__quantum__rt__qubit_release))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.qubit_release,
            *qubit_release as usize,
        );

        if let Some(qubit_release_array_fn) = context.runtime_library.qubit_release_array {
            let qubit_release_array = self
                .runtime
                .get::<__quantum__rt__qubit_release_array>(
                    CString::new(name_of_type!(__quantum__rt__qubit_release_array))
                        .unwrap()
                        .as_bytes_with_nul(),
                )
                .unwrap();
            ee.add_global_mapping(&qubit_release_array_fn, *qubit_release_array as usize);
        }
        /*
        __quantum__rt__qubit_borrow
        __quantum__rt__qubit_borrow_array
        __quantum__rt__qubit_return
        __quantum__rt__qubit_return_array
        __quantum__rt__qubit_restricted_reuse_area_start
        __quantum__rt__qubit_restricted_reuse_segment_next
        __quantum__rt__qubit_restricted_reuse_area_end
        __quantum__rt__heap_alloc
        __quantum__rt__heap_free
        */

        if let Some(memory_allocate_fn) = context.runtime_library.memory_allocate {
            let memory_allocate = self
                .runtime
                .get::<__quantum__rt__memory_allocate>(
                    CString::new(name_of_type!(__quantum__rt__memory_allocate))
                        .unwrap()
                        .as_bytes_with_nul(),
                )
                .unwrap();
            ee.add_global_mapping(&memory_allocate_fn, *memory_allocate as usize);
        }
        /*
        __quantum__rt__fail
        __quantum__rt__fail_cstr
        */

        let message = self
            .runtime
            .get::<__quantum__rt__message>(
                CString::new(name_of_type!(__quantum__rt__message))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(&context.runtime_library.message, *message as usize);

        let result_equal = self
            .runtime
            .get::<__quantum__rt__result_equal>(
                CString::new(name_of_type!(__quantum__rt__result_equal))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.result_equal,
            *result_equal as usize,
        );

        let result_update_reference_count = self
            .runtime
            .get::<__quantum__rt__result_update_reference_count>(
                CString::new(name_of_type!(__quantum__rt__result_update_reference_count))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.result_update_reference_count,
            *result_update_reference_count as usize,
        );

        let result_get_zero = self
            .runtime
            .get::<__quantum__rt__result_get_zero>(
                CString::new(name_of_type!(__quantum__rt__result_get_zero))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.result_get_zero,
            *result_get_zero as usize,
        );

        let result_get_one = self
            .runtime
            .get::<__quantum__rt__result_get_one>(
                CString::new(name_of_type!(__quantum__rt__result_get_one))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.result_get_one,
            *result_get_one as usize,
        );

        /*
        __quantum__rt__tuple_create
        __quantum__rt__tuple_update_reference_count
        __quantum__rt__tuple_update_alias_count
        __quantum__rt__tuple_copy
        */

        let array_create_1d = self
            .runtime
            .get::<__quantum__rt__array_create_1d>(
                CString::new(name_of_type!(__quantum__rt__array_create_1d))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.array_create_1d,
            *array_create_1d as usize,
        );

        let array_update_reference_count = self
            .runtime
            .get::<__quantum__rt__array_update_reference_count>(
                CString::new(name_of_type!(__quantum__rt__array_update_reference_count))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.array_update_reference_count,
            *array_update_reference_count as usize,
        );

        let array_update_alias_count = self
            .runtime
            .get::<__quantum__rt__array_update_alias_count>(
                CString::new(name_of_type!(__quantum__rt__array_update_alias_count))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.array_update_alias_count,
            *array_update_alias_count as usize,
        );

        /*
        __quantum__rt__array_copy
        __quantum__rt__array_concatenate
        __quantum__rt__array_get_size
        */

        let array_get_size_1d = self
            .runtime
            .get::<__quantum__rt__array_get_size_1d>(
                CString::new(name_of_type!(__quantum__rt__array_get_size_1d))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.array_get_size_1d,
            *array_get_size_1d as usize,
        );

        let array_get_element_ptr_1d = self
            .runtime
            .get::<__quantum__rt__array_get_element_ptr_1d>(
                CString::new(name_of_type!(__quantum__rt__array_get_element_ptr_1d))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.array_get_element_ptr_1d,
            *array_get_element_ptr_1d as usize,
        );

        /*
        __quantum__rt__array_create
        __quantum__rt__array_create_nonvariadic
        __quantum__rt__array_get_dim
        __quantum__rt__array_get_element_ptr
        __quantum__rt__array_get_element_ptr_nonvariadic
        __quantum__rt__array_project
        */
        /*
        __quantum__rt__callable_create
        __quantum__rt__callable_update_reference_count
        __quantum__rt__callable_update_alias_count
        __quantum__rt__callable_copy
        __quantum__rt__callable_invoke
        __quantum__rt__callable_make_adjoint
        __quantum__rt__callable_make_controlled
        __quantum__rt__capture_update_reference_count
        __quantum__rt__capture_update_alias_count
        */

        let string_create = self
            .runtime
            .get::<__quantum__rt__string_create>(
                CString::new(name_of_type!(__quantum__rt__string_create))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.string_create,
            *string_create as usize,
        );

        let string_update_reference_count = self
            .runtime
            .get::<__quantum__rt__string_update_reference_count>(
                CString::new(name_of_type!(__quantum__rt__string_update_reference_count))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.string_update_reference_count,
            *string_update_reference_count as usize,
        );

        let string_concatenate = self
            .runtime
            .get::<__quantum__rt__string_concatenate>(
                CString::new(name_of_type!(__quantum__rt__string_concatenate))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.string_concatenate,
            *string_concatenate as usize,
        );

        /*
        __quantum__rt__string_equal
        __quantum__rt__int_to_string
        __quantum__rt__double_to_string
        __quantum__rt__bool_to_string
        */
        let result_to_string = self
            .runtime
            .get::<__quantum__rt__result_to_string>(
                CString::new(name_of_type!(__quantum__rt__result_to_string))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        ee.add_global_mapping(
            &context.runtime_library.result_to_string,
            *result_to_string as usize,
        );
        /*
        __quantum__rt__pauli_to_string
        __quantum__rt__qubit_to_string
        __quantum__rt__string_get_data
        __quantum__rt__string_get_length
        */
    }
}
