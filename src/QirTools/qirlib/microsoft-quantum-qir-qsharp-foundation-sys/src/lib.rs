use lazy_static::lazy_static;
use libloading::Library;
use mut_static::MutStatic;

lazy_static! {
    pub(crate) static ref FOUNDATION_LIBRARY: MutStatic<Library> = unsafe {
        MutStatic::from(
            qir_libloading::load_library_bytes(
                "Microsoft.Quantum.Qir.QSharp.Foundation",
                FOUNDATION_BYTES,
            )
            .unwrap(),
        )
    };
}

#[cfg(target_os = "linux")]
const FOUNDATION_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/linux-x64/native/libMicrosoft.Quantum.Qir.QSharp.Foundation.so"
);

#[cfg(target_os = "mac_os")]
const FOUNDATION_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/osx-x64/native/libMicrosoft.Quantum.Qir.QSharp.Foundation.dylib"
);

#[cfg(target_os = "windows")]
const FOUNDATION_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/win-x64/native/Microsoft.Quantum.Qir.QSharp.Foundation.dll"
);

pub struct QSharpFoundation {}

impl QSharpFoundation {
    pub fn new() -> QSharpFoundation {
        QSharpFoundation {}
    }
}

#[link(name = "Microsoft.Quantum.Qir.QSharp.Foundation")]
extern "C" {
    // Q# Math:
    pub fn __quantum__qis__nan__body() -> f64;
    pub fn __quantum__qis__isnan__body(d: f64) -> bool;
    pub fn __quantum__qis__infinity__body() -> f64;
    pub fn __quantum__qis__isinf__body(d: f64) -> bool;
    pub fn __quantum__qis__isnegativeinfinity__body(d: f64) -> bool;
    pub fn __quantum__qis__sin__body(d: f64) -> f64;
    pub fn __quantum__qis__cos__body(d: f64) -> f64;
    pub fn __quantum__qis__tan__body(d: f64) -> f64;
    pub fn __quantum__qis__arctan2__body(y: f64, x: f64) -> f64;
    pub fn __quantum__qis__sinh__body(theta: f64) -> f64;
    pub fn __quantum__qis__cosh__body(theta: f64) -> f64;
    pub fn __quantum__qis__tanh__body(theta: f64) -> f64;
    pub fn __quantum__qis__arcsin__body(theta: f64) -> f64;
    pub fn __quantum__qis__arccos__body(theta: f64) -> f64;
    pub fn __quantum__qis__arctan__body(theta: f64) -> f64;
    pub fn __quantum__qis__sqrt__body(d: f64) -> f64;
    pub fn __quantum__qis__log__body(d: f64) -> f64;

    /*
     f64 __quantum__qis__ieeeremainder__body(x: f64, y: f64);
     int64_t __quantum__qis__drawrandomint__body(int64_t minimum, int64_t maximum);
     f64 __quantum__qis__drawrandomf64__body(f64 minimum, f64 maximum);

     // Q# ApplyIf:
     void __quantum__qis__applyifelseintrinsic__body(RESULT*, QirCallable*, QirCallable*);
     void __quantum__qis__applyconditionallyintrinsic__body(
     QirArray*, QirArray*, QirCallable*, QirCallable*);

     // Q# Assert Measurement:
     void __quantum__qis__assertmeasurementprobability__body(
     QirArray* bases, QirArray* qubits, RESULT* result, f64 prob, QirString* msg, f64 tol);
     void __quantum__qis__assertmeasurementprobability__ctl(
     QirArray* ctls, QirAssertMeasurementProbabilityTuple* args);
    */
}
