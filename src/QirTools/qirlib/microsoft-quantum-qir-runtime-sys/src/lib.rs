#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use lazy_static::lazy_static;
use mut_static::MutStatic;

use std::ffi::CString;

use cty;
use libloading::Library;

#[cfg(target_os = "linux")]
const RUNTIME_BYTES: &'static [u8] =
    include_bytes!("../../qir-runtime/bin/linux-x64/native/libMicrosoft.Quantum.Qir.Runtime.so");

#[cfg(target_os = "macos")]
const RUNTIME_BYTES: &'static [u8] =
    include_bytes!("../../qir-runtime/bin/osx-x64/native/libMicrosoft.Quantum.Qir.Runtime.dylib");

#[cfg(target_os = "windows")]
const RUNTIME_BYTES: &'static [u8] =
    include_bytes!("../../qir-runtime/bin/win-x64/native/Microsoft.Quantum.Qir.Runtime.dll");

/*
struct QirRTuple
{
    PauliId pauli;
    double angle;
    QUBIT* target;
};

struct QirExpTuple
{
    QirArray* paulis;
    double angle;
    QirArray* targets;
};
*/

#[repr(C)]
pub struct QirRTuple {
    //private: [u8; 0],
    pauli: PauliId,
    angle: f64,
    target: QUBIT,
}

#[repr(C)]
pub struct QirExpTuple {
    private: [u8; 0],
}

#[repr(C)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub struct RESULT {
    private: [u8; 0],
}

#[repr(C)]
pub struct QirCallable {
    private: [u8; 0],
}

#[repr(C)]
pub struct QirString {
    private: [u8; 0],
}

pub type PauliId = i8;
pub type size_t = cty::c_ulong;
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

pub type QUBIT = u64;

#[repr(C)]
#[derive(Debug)]
pub struct QirArray {
    pub count: QirArray_TItemCount,
    pub itemSizeInBytes: QirArray_TItemSize,
    pub dimensions: QirArray_TDimCount,
    pub dimensionSizes: QirArray_TDimContainer,
    pub buffer: *mut cty::c_char,
    pub ownsQubits: bool,
    pub refCount: cty::c_int,
    pub aliasCount: cty::c_int,
}
pub type QirArray_TItemCount = u32;
pub type QirArray_TItemSize = u32;
pub type QirArray_TBufSize = size_t;
pub type QirArray_TDimCount = u8;
pub type QirArray_TDimContainer = std_vector;

#[repr(C)]
#[derive(Debug)]
pub struct std_vector {
    private: [u8; 0],
}

#[link(name = "Microsoft.Quantum.Qir.Runtime")]
extern "C" {
    pub fn __quantum__rt__qubit_allocate() -> *mut QUBIT;
    pub fn __quantum__rt__qubit_allocate_array(count: i64) -> *mut QirArray;
    pub fn __quantum__rt__qubit_release(arg1: *mut QUBIT);
    pub fn __quantum__rt__qubit_release_array(arg1: *mut QirArray);
    pub fn __quantum__rt__qubit_borrow() -> *mut QUBIT;
    pub fn __quantum__rt__qubit_borrow_array(count: i64) -> *mut QirArray;
    pub fn __quantum__rt__qubit_return(arg1: *mut QUBIT);
    pub fn __quantum__rt__qubit_return_array(arg1: *mut QirArray);
    pub fn __quantum__rt__qubit_restricted_reuse_area_start();
    pub fn __quantum__rt__qubit_restricted_reuse_segment_next();
    pub fn __quantum__rt__qubit_restricted_reuse_area_end();
    pub fn __quantum__rt__heap_alloc(size: u64) -> *mut cty::c_char;
    pub fn __quantum__rt__heap_free(buffer: *mut cty::c_char);
    pub fn __quantum__rt__memory_allocate(size: u64) -> *mut cty::c_char;
    pub fn __quantum__rt__fail(msg: *mut QirString);
    pub fn __quantum__rt__fail_cstr(msg: *const cty::c_char);
    pub fn __quantum__rt__message(msg: *mut QirString);
    pub fn __quantum__rt__result_equal(arg1: *mut RESULT, arg2: *mut RESULT) -> bool;
    pub fn __quantum__rt__result_update_reference_count(arg1: *mut RESULT, arg2: i32);
    pub fn __quantum__rt__result_get_one() -> *mut RESULT;
    pub fn __quantum__rt__result_get_zero() -> *mut RESULT;
    pub fn __quantum__rt__tuple_create(arg1: i64) -> PTuple;
    pub fn __quantum__rt__tuple_update_reference_count(arg1: PTuple, arg2: i32);
    pub fn __quantum__rt__tuple_update_alias_count(arg1: PTuple, arg2: i32);
    pub fn __quantum__rt__tuple_copy(arg1: PTuple, force: bool) -> PTuple;
    pub fn __quantum__rt__array_create_1d(arg1: i32, arg2: i64) -> *mut QirArray;
    pub fn __quantum__rt__array_update_reference_count(arg1: *mut QirArray, arg2: i32);
    pub fn __quantum__rt__array_update_alias_count(arg1: *mut QirArray, arg2: i32);
    pub fn __quantum__rt__array_copy(arg1: *mut QirArray, arg2: bool) -> *mut QirArray;
    pub fn __quantum__rt__array_concatenate(
        arg1: *mut QirArray,
        arg2: *mut QirArray,
    ) -> *mut QirArray;
    pub fn __quantum__rt__array_get_size(arg1: *mut QirArray, arg2: i32) -> i64;
    pub fn __quantum__rt__array_get_size_1d(arg1: *mut QirArray) -> i64;
    pub fn __quantum__rt__array_get_element_ptr_1d(
        arg1: *mut QirArray,
        arg2: i64,
    ) -> *mut cty::c_char;
    pub fn __quantum__rt__array_create(arg1: cty::c_int, arg2: cty::c_int, ...) -> *mut QirArray;
    pub fn __quantum__rt__array_create_nonvariadic(
        itemSizeInBytes: cty::c_int,
        countDimensions: cty::c_int,
        dims: *mut __va_list_tag,
    ) -> *mut QirArray;
    pub fn __quantum__rt__array_get_dim(arg1: *mut QirArray) -> i32;
    pub fn __quantum__rt__array_get_element_ptr(arg1: *mut QirArray, ...) -> *mut cty::c_char;
    pub fn __quantum__rt__array_get_element_ptr_nonvariadic(
        arg1: *mut QirArray,
        dims: *mut __va_list_tag,
    ) -> *mut cty::c_char;
    pub fn __quantum__rt__array_project(arg1: *mut QirArray, arg2: i32, arg3: i64)
        -> *mut QirArray;
    pub fn __quantum__rt__callable_create(
        arg1: *mut t_CallableEntry,
        arg2: *mut t_CaptureCallback,
        arg3: PTuple,
    ) -> *mut QirCallable;
    pub fn __quantum__rt__callable_update_reference_count(arg1: *mut QirCallable, arg2: i32);
    pub fn __quantum__rt__callable_update_alias_count(arg1: *mut QirCallable, arg2: i32);
    pub fn __quantum__rt__callable_copy(arg1: *mut QirCallable, arg2: bool) -> *mut QirCallable;
    pub fn __quantum__rt__callable_invoke(arg1: *mut QirCallable, arg2: PTuple, arg3: PTuple);
    pub fn __quantum__rt__callable_make_adjoint(arg1: *mut QirCallable);
    pub fn __quantum__rt__callable_make_controlled(arg1: *mut QirCallable);
    pub fn __quantum__rt__capture_update_reference_count(arg1: *mut QirCallable, arg2: i32);
    pub fn __quantum__rt__capture_update_alias_count(arg1: *mut QirCallable, arg2: i32);
    pub fn __quantum__rt__string_create(arg1: *const cty::c_char) -> *mut QirString;
    pub fn __quantum__rt__string_update_reference_count(arg1: *mut QirString, arg2: i32);
    pub fn __quantum__rt__string_concatenate(
        arg1: *mut QirString,
        arg2: *mut QirString,
    ) -> *mut QirString;
    pub fn __quantum__rt__string_equal(arg1: *mut QirString, arg2: *mut QirString) -> bool;
    pub fn __quantum__rt__int_to_string(arg1: i64) -> *mut QirString;
    pub fn __quantum__rt__double_to_string(arg1: f64) -> *mut QirString;
    pub fn __quantum__rt__bool_to_string(arg1: bool) -> *mut QirString;
    pub fn __quantum__rt__result_to_string(arg1: *mut RESULT) -> *mut QirString;
    pub fn __quantum__rt__pauli_to_string(arg1: PauliId) -> *mut QirString;
    pub fn __quantum__rt__qubit_to_string(arg1: *mut QUBIT) -> *mut QirString;
    pub fn __quantum__rt__string_get_data(str_: *mut QirString) -> *const cty::c_char;
    pub fn __quantum__rt__string_get_length(str_: *mut QirString) -> u32;
}

pub type IRuntimeDriver = cty::c_void;

#[link(name = "Microsoft.Quantum.Qir.Runtime")]
extern "C" {
    pub fn CreateBasicRuntimeDriver() -> *mut IRuntimeDriver;
}

#[link(name = "Microsoft.Quantum.Qir.Runtime")]
extern "C" {
    pub fn InitializeQirContext(driver: *mut IRuntimeDriver, trackAllocatedObjects: bool);
}
// Use the Q# runtime but all __qis__ (gate) instructions must be provided.
// Gate instructions are typically found in Microsoft.Quantum.Qir.QSharp.Core
pub struct BasicRuntimeDriver {}

impl BasicRuntimeDriver {
    pub unsafe fn initialize_qir_context(track_allocated_objects: bool) {
        // The libloading calls need to be used instead of the extern "C" calls
        // to prevent linkage. Python can't init the lib if we take a hard
        // dependency on the library
        let driver = QirRuntime::CreateBasicRuntimeDriver();
        QirRuntime::InitializeQirContext(driver, track_allocated_objects);
    }
}

lazy_static! {
    pub(crate) static ref RUNTIME_LIBRARY: MutStatic<Library> = unsafe {
        MutStatic::from(
            qir_libloading::load_library_bytes("Microsoft.Quantum.Qir.Runtime", RUNTIME_BYTES)
                .unwrap(),
        )
    };
}

pub struct QirRuntime {}

impl QirRuntime {
    pub unsafe fn CreateBasicRuntimeDriver() -> *mut cty::c_void {
        let library = RUNTIME_LIBRARY.read().unwrap();
        let create = library
            .get::<fn() -> *mut IRuntimeDriver>(
                CString::new("CreateBasicRuntimeDriver")
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        create()
    }

    pub unsafe fn InitializeQirContext(driver: *mut cty::c_void, trackAllocatedObjects: bool) {
        let library = RUNTIME_LIBRARY.read().unwrap();
        let init = library
            .get::<fn(*mut cty::c_void, bool)>(
                CString::new("InitializeQirContext")
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        init(driver, trackAllocatedObjects)
    }

    pub unsafe fn quantum_rt_array_get_element_ptr_1d(
        array: *mut QirArray,
        index: i64,
    ) -> *mut cty::c_char {
        let library = RUNTIME_LIBRARY.read().unwrap();
        let get_element_ptr = library
            .get::<fn(*mut QirArray, arg2: i64) -> *mut cty::c_char>(
                CString::new("__quantum__rt__array_get_element_ptr_1d")
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        get_element_ptr(array, index)
    }
}
