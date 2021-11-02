// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

#[cfg(target_os = "macos")]
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
