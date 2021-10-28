#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]
#![allow(dead_code)]

use std::ffi::CString;
use std::path::Path;
use std::path::Path;
use std::{io, path::Path};

use super::load_library;
use libloading::Error;
use libloading::Library;
use libloading::Symbol;

use libloading::library_filename;

#[cfg(target_os = "linux")]
const SIMULATOR_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/linux-x64/native/libMicrosoft.Quantum.Simulator.Runtime.so"
);

#[cfg(target_os = "macos")]
const SIMULATOR_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/osx-x64/native/libMicrosoft.Quantum.Simulator.Runtime.dylib"
);

#[cfg(target_os = "windows")]
const SIMULATOR_BYTES: &'static [u8] =
    include_bytes!("../../qir-runtime/bin/win-x64/native/Microsoft.Quantum.Simulator.Runtime.dll");

macro_rules! name_of_type {
    // Covers Types
    ($t: ty) => {{
        let _ = || {
            let _: $t;
        };
        stringify!($t)
    }};
}

pub struct Simulator {
    library: Library,
}

impl Simulator {
    pub unsafe fn new() -> Result<Simulator, Box<dyn Error>> {
        let library = qir_libloading::load_library_bytes(
            "Microsoft.Quantum.Simulator.Runtime",
            SIMULATOR_BYTES,
        )?;
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

// Use the full Q# runtime
pub(crate) struct SimulatorRuntime {
    core: Core,
    foundation: Foundation,
    runtime: Runtime,
}

type InitializeQirContext =
    unsafe extern "C" fn(driver: *mut cty::c_void, trackAllocatedObjects: bool) -> *mut Driver;

impl SimulatorRuntime {
    pub unsafe fn new<P: AsRef<Path>>(base: &P) -> Result<Runtime, Error> {
        // Must load in this order or you have to mess with LD_LIBRARY_PATH on unix
        let runtime = Runtime::new()?;
        let core = Core::new()?;
        let foundation = Foundation::new()?;

        Ok(Runtime {
            core,
            foundation,
            runtime,
        })
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
        init(driver as *mut cty::c_void, trackAllocatedObjects);
    }
}
