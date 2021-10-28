use std::{io, path::Path};

use cty;

#[cfg(target_os = "linux")]
const CORE_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/linux-x64/native/libMicrosoft.Quantum.Qir.QSharp.Core.so"
);

#[cfg(target_os = "macos")]
const CORE_BYTES: &'static [u8] = include_bytes!(
    "../../qir-runtime/bin/osx-x64/native/libMicrosoft.Quantum.Qir.QSharp.Core.dylib"
);

#[cfg(target_os = "windows")]
const CORE_BYTES: &'static [u8] =
    include_bytes!("../../qir-runtime/bin/win-x64/native/Microsoft.Quantum.Qir.QSharp.Core.dll");

pub struct Core {
    library: Library,
}

impl Core {
    pub unsafe fn new() -> Result<Core, Box<dyn Error>> {
        let library =
            qir_libloading::load_library_bytes("Microsoft.Quantum.Qir.QSharp.Core", CORE_BYTES)?;
        Ok(Core { library })
    }
}
