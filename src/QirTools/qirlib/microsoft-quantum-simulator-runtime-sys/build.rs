use std::boxed::Box;
use std::error::Error;
use std::path::{Path, PathBuf};
use std::{env, fs};

use libloading::library_filename;

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
    link_runtime(&manifest_dir)?;
    Ok(())
}

fn link_x86_64_apple_darwin(manifest_dir: &str) -> Result<(), Box<dyn Error>> {
    link_runtime(&manifest_dir)?;
    Ok(())
}

fn link_x86_64_pc_windows_msvc(manifest_dir: &str) -> Result<(), Box<dyn Error>> {
    link_runtime(&manifest_dir)?;
    Ok(())
}

fn get_output_path() -> PathBuf {
    return PathBuf::from(env::var("OUT_DIR").unwrap());
}

fn link_runtime(manifest_dir: &str) -> Result<(), Box<dyn Error>> {
    let output_path = get_output_path();
    let native_dir =
        fs::canonicalize(PathBuf::from(&manifest_dir).join("../qir-runtime/bin/linux-x64/native"))?;
    println!("cargo:rustc-link-search=native={}", output_path.display());
    println!("cargo:rustc-link-lib=dylib=Microsoft.Quantum.Simulator.Runtime");

    let name = library_filename("Microsoft.Quantum.Simulator.Runtime")
        .into_string()
        .expect("Could not get library name as string");
    let input_path = Path::new(&native_dir).join(name.as_str());
    let output_lib = Path::new(&output_path).join(name.as_str());
    println!(
        "Copying {} to {}",
        input_path.to_str().unwrap(),
        output_lib.to_str().unwrap()
    );
    std::fs::copy(input_path, output_lib)?;
    Ok(())
}
