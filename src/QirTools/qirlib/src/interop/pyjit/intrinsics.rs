#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]
#![allow(unused_variables)]
#![allow(unused_imports)]

use microsoft_quantum_qir_runtime_sys::{PauliId, QirArray, QirRTuple, QirRuntime, QUBIT};
use mut_static::ForceSomeRwLockWriteGuard;
use std::ops::DerefMut;

use super::gates::BaseProfile;

fn get_current_gate_processor() -> ForceSomeRwLockWriteGuard<'static, BaseProfile> {
    let v = crate::interop::pyjit::gates::CURRENT_GATES.write().unwrap();
    v
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__h__body(qubit: QUBIT) {
    log::debug!("/__quantum__qis__h__body/");
    let mut gs = get_current_gate_processor();
    gs.h(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__h__ctl(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__h__ctl/");
    let control = get_qubit_id(ctls);
    //let mut gs = get_current_gate_processor();
    todo!("Not yet implemented.");
    //gs.h_ctl(control, get_cubit_string(qubit));
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__measure__body(
    qubits: *mut QirArray,
    registers: *mut QirArray,
) {
    log::debug!("/__quantum__qis__measure__body/");

    let qubit = get_qubit_id(qubits);
    // let register = get_qubit_id(registers);
    let mut gs = get_current_gate_processor();
    gs.m(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__r__body(pauli: PauliId, theta: f64, qubit: QUBIT) {
    log::debug!("/__quantum__qis__r__body/");
    let mut gs = get_current_gate_processor();
    match pauli {
        1 => gs.rx(theta, qubit),
        3 => gs.ry(theta, qubit),
        2 => gs.rz(theta, qubit),
        _ => panic!("Unsupported Pauli value: {}", pauli),
    }
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__r__adj(pauli: PauliId, theta: f64, qubit: QUBIT) {
    log::debug!("/__quantum__qis__r__adj/");
    //let mut gs = get_current_gate_processor();
    todo!("Not yet implemented.");
    //gs.r_adj(pauli, theta, get_cubit_string(qubit));
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__r__ctl(ctls: *mut QirArray, qubit: *mut QirRTuple) {
    log::debug!("/__quantum__qis__r__ctl/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__r__ctladj(ctls: *mut QirArray, qubit: *mut QirRTuple) {
    log::debug!("/__quantum__qis__r__ctladj/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__s__body(qubit: QUBIT) {
    log::debug!("/__quantum__qis__s__body/");
    let mut gs = get_current_gate_processor();
    gs.s(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__s__adj(qubit: QUBIT) {
    log::debug!("/__quantum__qis__s__adj/");
    let mut gs = get_current_gate_processor();
    gs.s_adj(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__s__ctl(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__s__ctl/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__s__ctladj(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__s__ctladj/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__t__body(qubit: QUBIT) {
    log::debug!("/__quantum__qis__t__body/");
    let mut gs = get_current_gate_processor();
    gs.t(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__t__adj(qubit: QUBIT) {
    log::debug!("/__quantum__qis__t__adj/");
    let mut gs = get_current_gate_processor();
    gs.t_adj(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__t__ctl(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__t__ctl/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__t__ctladj(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__t__ctladj/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__x__body(qubit: QUBIT) {
    log::debug!("/__quantum__qis__x__body/");
    let mut gs = get_current_gate_processor();
    gs.x(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__x__ctl(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__x__ctl/");
    let control = get_qubit_id(ctls);
    let mut gs = get_current_gate_processor();
    gs.cx(control, qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__y__body(qubit: QUBIT) {
    log::debug!("/__quantum__qis__y__body/");
    let mut gs = get_current_gate_processor();
    gs.y(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__y__ctl(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__y__ctl/");
    todo!("Not yet implemented.");
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__z__body(qubit: QUBIT) {
    log::debug!("/__quantum__qis__z__body/");
    let mut gs = get_current_gate_processor();
    gs.y(qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__z__ctl(ctls: *mut QirArray, qubit: QUBIT) {
    log::debug!("/__quantum__qis__z__ctl/");
    let control = get_qubit_id(ctls);
    let mut gs = get_current_gate_processor();
    gs.cz(control, qubit);
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__dumpmachine__body(location: *mut u8) {
    log::debug!("/__quantum__qis__dumpmachine__body/");
    log::debug!("/__quantum__qis__h__body/");
    let mut gs = get_current_gate_processor();
    gs.dump_machine();
}

#[no_mangle]
pub unsafe extern "C" fn __quantum__qis__dumpregister__body(
    location: *mut u8,
    qubits: *mut QirArray,
) {
    log::debug!("/__quantum__qis__dumpregister__body/");
    todo!("Not yet implemented.");
}

pub unsafe fn get_qubit_id(ctls: *mut QirArray) -> QUBIT {
    let ctrl_qubit_ptr = QirRuntime::quantum_rt_array_get_element_ptr_1d(ctls, 0) as *mut u64;
    let ctrl_qubit = *ctrl_qubit_ptr;
    log::debug!("ctrl_qubit {}", ctrl_qubit);
    ctrl_qubit as QUBIT
}

/*
extern "C"
{
    // Q# Gate Set
    QIR_SHARED_API void __quantum__qis__exp__body(QirArray*, double, QirArray*); // NOLINT
    QIR_SHARED_API void __quantum__qis__exp__adj(QirArray*, double, QirArray*);  // NOLINT
    QIR_SHARED_API void __quantum__qis__exp__ctl(QirArray*, QirExpTuple*);       // NOLINT
    QIR_SHARED_API void __quantum__qis__exp__ctladj(QirArray*, QirExpTuple*);    // NOLINT
    QIR_SHARED_API void __quantum__qis__h__body(QUBIT*);                         // NOLINT
    QIR_SHARED_API void __quantum__qis__h__ctl(QirArray*, QUBIT*);               // NOLINT
    QIR_SHARED_API RESULT* __quantum__qis__measure__body(QirArray*, QirArray*);  // NOLINT
    QIR_SHARED_API void __quantum__qis__r__body(PauliId, double, QUBIT*);        // NOLINT
    QIR_SHARED_API void __quantum__qis__r__adj(PauliId, double, QUBIT*);         // NOLINT
    QIR_SHARED_API void __quantum__qis__r__ctl(QirArray*, QirRTuple*);           // NOLINT
    QIR_SHARED_API void __quantum__qis__r__ctladj(QirArray*, QirRTuple*);        // NOLINT
    QIR_SHARED_API void __quantum__qis__s__body(QUBIT*);                         // NOLINT
    QIR_SHARED_API void __quantum__qis__s__adj(QUBIT*);                          // NOLINT
    QIR_SHARED_API void __quantum__qis__s__ctl(QirArray*, QUBIT*);               // NOLINT
    QIR_SHARED_API void __quantum__qis__s__ctladj(QirArray*, QUBIT*);            // NOLINT
    QIR_SHARED_API void __quantum__qis__t__body(QUBIT*);                         // NOLINT
    QIR_SHARED_API void __quantum__qis__t__adj(QUBIT*);                          // NOLINT
    QIR_SHARED_API void __quantum__qis__t__ctl(QirArray*, QUBIT*);               // NOLINT
    QIR_SHARED_API void __quantum__qis__t__ctladj(QirArray*, QUBIT*);            // NOLINT
    QIR_SHARED_API void __quantum__qis__x__body(QUBIT*);                         // NOLINT
    QIR_SHARED_API void __quantum__qis__x__ctl(QirArray*, QUBIT*);               // NOLINT
    QIR_SHARED_API void __quantum__qis__y__body(QUBIT*);                         // NOLINT
    QIR_SHARED_API void __quantum__qis__y__ctl(QirArray*, QUBIT*);               // NOLINT
    QIR_SHARED_API void __quantum__qis__z__body(QUBIT*);                         // NOLINT
    QIR_SHARED_API void __quantum__qis__z__ctl(QirArray*, QUBIT*);               // NOLINT

    // Q# Dump:
    // Note: The param `location` must be `const void*`,
    // but it is called from .ll, where `const void*` is not supported.
    QIR_SHARED_API void __quantum__qis__dumpmachine__body(uint8_t* location);                          // NOLINT
    QIR_SHARED_API void __quantum__qis__dumpregister__body(uint8_t* location, const QirArray* qubits); // NOLINT
}
*/
