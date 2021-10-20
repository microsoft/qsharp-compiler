#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use libloading::Error;
use libloading::Library;
use std::ffi::CString;
use std::path::Path;

use super::load_library;

pub(crate) struct Runtime {
    core: Library,
    foundation: Library,
    runtime: Library,
}

#[repr(C)]
pub struct Driver {}

type CreateFullstateSimulatorC = unsafe extern "C" fn(seed: i32) -> *mut Driver;
type InitializeQirContext =
    unsafe extern "C" fn(driver: *mut Driver, trackAllocatedObjects: bool) -> *mut Driver;

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

    pub unsafe fn CreateFullstateSimulatorC(&self, seed: i32) -> *mut Driver {
        let create = self
            .core
            .get::<CreateFullstateSimulatorC>(
                CString::new(name_of_type!(CreateFullstateSimulatorC))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        create(seed)
    }

    pub unsafe fn InitializeQirContext(&self, driver: *mut Driver, trackAllocatedObjects: bool) {
        let init = self
            .runtime
            .get::<InitializeQirContext>(
                CString::new(name_of_type!(InitializeQirContext))
                    .unwrap()
                    .as_bytes_with_nul(),
            )
            .unwrap();
        init(driver, trackAllocatedObjects);
    }
}
