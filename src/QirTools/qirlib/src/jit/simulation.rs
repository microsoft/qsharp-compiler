#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use std::path::Path;

use libloading::Error;
use libloading::Library;
use libloading::Symbol;

use super::load_library;

pub(crate) struct Simulator {
    simulator: Library,
}

impl Simulator {
    pub unsafe fn new<P: AsRef<Path>>(base: &P) -> Result<Simulator, Error> {
        let simulator = load_library(&base, "Microsoft.Quantum.Simulator.Runtime")?;
        Ok(Simulator { simulator })
    }

    pub unsafe fn init(&self) -> cty::c_uint {
        let init: Symbol<'_, unsafe extern "C" fn() -> cty::c_uint> =
            self.simulator.get(b"init\0").unwrap();
        let id = init();
        id
    }

    pub unsafe fn destroy(&self, sid: cty::c_uint) {
        let destroy: libloading::Symbol<'_, unsafe extern "C" fn(cty::c_uint) -> ()> =
            self.simulator.get(b"destroy\0").unwrap();
        destroy(sid);
    }

    pub unsafe fn seed(&self, sid: cty::c_uint, s: cty::c_uint) {
        let seed: libloading::Symbol<'_, unsafe extern "C" fn(cty::c_uint, cty::c_uint) -> ()> =
            self.simulator.get(b"seed\0").unwrap();
        seed(sid, s);
    }
}
/*
pub type std_size_t = cty::c_ulong;
pub type size_t = cty::c_ulong;
extern "C" {
    pub fn init() -> cty::c_uint;
}
extern "C" {
    pub fn destroy(sid: cty::c_uint);
}
extern "C" {
    pub fn seed(sid: cty::c_uint, s: cty::c_uint);
}
extern "C" {
    pub fn Dump(
        sid: cty::c_uint,
        callback: ::core::option::Option<
            unsafe extern "C" fn(arg1: size_t, arg2: f64, arg3: f64) -> bool,
        >,
    );
}
extern "C" {
    pub fn DumpQubits(
        sid: cty::c_uint,
        n: cty::c_uint,
        q: *mut cty::c_uint,
        callback: ::core::option::Option<
            unsafe extern "C" fn(arg1: size_t, arg2: f64, arg3: f64) -> bool,
        >,
    ) -> bool;
}
pub type TDumpLocation = *mut cty::c_void;
pub type TDumpToLocationCallback = ::core::option::Option<
    unsafe extern "C" fn(arg1: size_t, arg2: f64, arg3: f64, arg4: TDumpLocation) -> bool,
>;
extern "C" {
    pub fn DumpToLocation(
        sid: cty::c_uint,
        callback: TDumpToLocationCallback,
        location: TDumpLocation,
    );
}
extern "C" {
    pub fn DumpQubitsToLocation(
        sid: cty::c_uint,
        n: cty::c_uint,
        q: *mut cty::c_uint,
        callback: TDumpToLocationCallback,
        location: TDumpLocation,
    ) -> bool;
}
extern "C" {
    pub fn DumpIds(
        sid: cty::c_uint,
        callback: ::core::option::Option<unsafe extern "C" fn(arg1: cty::c_uint)>,
    );
}
extern "C" {
    pub fn random_choice(sid: cty::c_uint, n: std_size_t, p: *mut f64) -> std_size_t;
}
extern "C" {
    pub fn JointEnsembleProbability(
        sid: cty::c_uint,
        n: cty::c_uint,
        b: *mut cty::c_int,
        q: *mut cty::c_uint,
    ) -> f64;
}
extern "C" {
    pub fn InjectState(
        sid: cty::c_uint,
        n: cty::c_uint,
        q: *mut cty::c_uint,
        re: *mut f64,
        im: *mut f64,
    ) -> bool;
}
extern "C" {
    pub fn PermuteBasis(
        sid: cty::c_uint,
        n: cty::c_uint,
        q: *mut cty::c_uint,
        table_size: std_size_t,
        permutation_table: *mut std_size_t,
    );
}
extern "C" {
    pub fn AdjPermuteBasis(
        sid: cty::c_uint,
        n: cty::c_uint,
        q: *mut cty::c_uint,
        table_size: std_size_t,
        permutation_table: *mut std_size_t,
    );
}
*/
