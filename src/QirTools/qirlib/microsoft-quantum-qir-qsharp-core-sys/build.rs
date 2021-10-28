use std::boxed::Box;
use std::env;
use std::error::Error;
use std::path::PathBuf;

use qir_libloading::link_runtime;

fn main() -> Result<(), Box<dyn Error>> {
    println!("cargo:rerun-if-env-changed=TARGET");
    println!("cargo:rerun-if-changed=build.rs");
    set_linkage()?;
    Ok(())
}

fn set_linkage() -> Result<(), Box<dyn Error>> {
    let manifest_dir = env::var("CARGO_MANIFEST_DIR")?;
    let include_dir = PathBuf::from(&manifest_dir).join("qir-runtime/include");
    println!("cargo:include={}", include_dir.display());

    let target = env::var("TARGET").unwrap();
    return match &target[..] {
        "x86_64-unknown-linux-gnu" => link_x86_64_unknown_linux_gnu(&manifest_dir),
        "x86_64-apple-darwin" => link_x86_64_apple_darwin(&manifest_dir),
        "x86_64-pc-windows-msvc" => link_x86_64_pc_windows_msvc(&manifest_dir),
        _ => Ok(()),
    };
}

fn link_x86_64_unknown_linux_gnu(manifest_dir: &str) -> Result<(), Box<dyn Error>> {
    link_runtime(&manifest_dir, "Microsoft.Quantum.Qir.QSharp.Core")?;
    Ok(())
}

fn link_x86_64_apple_darwin(manifest_dir: &str) -> Result<(), Box<dyn Error>> {
    link_runtime(&manifest_dir, "Microsoft.Quantum.Qir.QSharp.Core")?;
    Ok(())
}

fn link_x86_64_pc_windows_msvc(manifest_dir: &str) -> Result<(), Box<dyn Error>> {
    link_runtime(&manifest_dir, "Microsoft.Quantum.Qir.QSharp.Core")?;
    Ok(())
}
