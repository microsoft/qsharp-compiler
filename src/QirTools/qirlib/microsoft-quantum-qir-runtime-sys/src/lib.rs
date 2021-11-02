// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

#[repr(C)]
pub struct QirRTuple {
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
pub type PTuplePointedType = u8;
pub type PTuple = *mut PTuplePointedType;

pub type QUBIT = u64;

#[repr(C)]
pub struct QirArray {
    private: [u8; 0],
}

#[repr(C)]
#[derive(Debug)]
pub struct std_vector {
    private: [u8; 0],
}

pub type IRuntimeDriver = cty::c_void;

pub struct BasicRuntimeDriver {}

impl BasicRuntimeDriver {
    pub unsafe fn initialize_qir_context(track_allocated_objects: bool) {
        // The libloading calls need to be used instead of the extern "C" calls
        // to prevent linkage. Python can't init the lib if we take a hard
        // dependency on the library
        let driver = QirRuntime::create_basic_runtime_driver();
        QirRuntime::initialize_qir_context(driver, track_allocated_objects);
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
    pub unsafe fn create_basic_runtime_driver() -> *mut cty::c_void {
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

    pub unsafe fn initialize_qir_context(driver: *mut cty::c_void, track_allocated_objects: bool) {
        let library = RUNTIME_LIBRARY.read().unwrap();
        let init = library
            .get::<fn(*mut cty::c_void, bool)>(
                CString::new("InitializeQirContext")
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        init(driver, track_allocated_objects)
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
