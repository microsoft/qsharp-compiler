use std::{io, path::Path};

use cty;

#[cfg(target_os = "linux")]
const CORE_BYTES: &'static [u8] = include_bytes!("../../qir-runtime/bin/linux-x64/native/libMicrosoft.Quantum.Qir.QSharp.Core.so");

pub struct Core {
    library : Library,
}

impl Core {
    pub unsafe fn new() -> Result<Core, Box<dyn Error>> {
        let library = qir_libloading::load_library_bytes(
            "Microsoft.Quantum.Qir.QSharp.Core",
            CORE_BYTES,
        )?;
        Ok(Core { library })
    }
}
