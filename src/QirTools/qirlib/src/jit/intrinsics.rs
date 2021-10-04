#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use std::ffi::CString;

use inkwell::execution_engine::ExecutionEngine;
use libloading::Library;

use crate::emit::Context;

use super::runtime::{PauliId, QirArray, QirExpTuple, QirRTuple, QUBIT, RESULT};

type __quantum__qis__exp__body =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: f64, arg3: *mut QirArray);
type __quantum__qis__exp__adj =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: f64, arg3: *mut QirArray);
type __quantum__qis__exp__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QirExpTuple);
type __quantum__qis__exp__ctladj =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QirExpTuple);
type __quantum__qis__h__body = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__h__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__measure__body =
    unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QirArray) -> *mut RESULT;
type __quantum__qis__r__body = unsafe extern "C" fn(arg1: PauliId, arg2: f64, arg3: *mut QUBIT);
type __quantum__qis__r__adj = unsafe extern "C" fn(arg1: PauliId, arg2: f64, arg3: *mut QUBIT);
type __quantum__qis__r__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QirRTuple);
type __quantum__qis__r__ctladj = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QirRTuple);
type __quantum__qis__s__body = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__s__adj = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__s__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__s__ctladj = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__t__body = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__t__adj = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__t__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__t__ctladj = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__x__body = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__x__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__y__body = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__y__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__z__body = unsafe extern "C" fn(arg1: *mut QUBIT);
type __quantum__qis__z__ctl = unsafe extern "C" fn(arg1: *mut QirArray, arg2: *mut QUBIT);
type __quantum__qis__dumpmachine__body = unsafe extern "C" fn(arg1: *mut u8);
type __quantum__qis__dumpregister__body = unsafe extern "C" fn(arg1: *mut u8, arg2: *mut QirArray);

macro_rules! name_of_type {
    // Covers Types
    ($t: ty) => {{
        let _ = || {
            let _: $t;
        };
        stringify!($t)
    }};
}

pub unsafe fn map_intrinsic_calls<'ctx>(
    core: &Library,
    context: &Context<'ctx>,
    ee: &ExecutionEngine<'ctx>,
) {
    let dumpmachine = core
        .get::<__quantum__qis__exp__body>(
            CString::new(name_of_type!(__quantum__qis__dumpmachine__body))
                .unwrap()
                .as_bytes_with_nul(),
        )
        .unwrap();
    ee.add_global_mapping(&context.intrinsics.dumpmachine, *dumpmachine as usize);

    let h = core
        .get::<__quantum__qis__h__body>(
            CString::new(name_of_type!(__quantum__qis__h__body))
                .unwrap()
                .as_bytes_with_nul(),
        )
        .unwrap();
    ee.add_global_mapping(&context.intrinsics.h, *h as usize);

    let m = core
        .get::<__quantum__qis__measure__body>(
            CString::new(name_of_type!(__quantum__qis__measure__body))
                .unwrap()
                .as_bytes_with_nul(),
        )
        .unwrap();
    ee.add_global_mapping(&context.intrinsics.m, *m as usize);

    let x_ctl = core
        .get::<__quantum__qis__x__ctl>(
            CString::new(name_of_type!(__quantum__qis__x__ctl))
                .unwrap()
                .as_bytes_with_nul(),
        )
        .unwrap();
    ee.add_global_mapping(&context.intrinsics.x_ctl, *x_ctl as usize);

    todo!("Add other missing qis instructions");
}
