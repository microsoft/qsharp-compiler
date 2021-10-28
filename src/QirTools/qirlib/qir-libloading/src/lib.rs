use libloading::{library_filename, Library};
use log;
use std::error::Error;
use std::path::{Path, PathBuf};
use std::{env, fs};
use tempfile::tempdir;

pub fn write_library<P: AsRef<Path>>(path: P, lib: &'static [u8]) -> Result<(), Box<dyn Error>> {
    println!("Writing {}", path.as_ref().display());
    std::fs::write(path, lib)?;
    Ok(())
}

pub unsafe fn load_library_bytes(
    base_name: &str,
    lib: &'static [u8],
) -> Result<Library, Box<dyn Error>> {
    let name = library_filename(base_name)
        .into_string()
        .expect("Could not get library name as string");
    let path = tempdir().expect("");
    let filepath = path.as_ref().join(name);
    write_library(&filepath, lib)?;
    let library = load_library(&filepath)?;
    Ok(library)
}

pub(crate) unsafe fn load_library<P: AsRef<Path>>(path: P) -> Result<Library, Box<dyn Error>> {
    println!("Loading {}", path.as_ref().display());
    let library = Library::new(path.as_ref().as_os_str())?;

    let library_path = path
        .as_ref()
        .to_str()
        .expect("Could not convert library path to &str");
    let was_loaded_by_llvm = inkwell::support::load_library_permanently(library_path);
    if was_loaded_by_llvm {
        log::error!("Failed to load {} into LLVM", library_path);
    } else {
        log::debug!("Loaded {} into LLVM", library_path);
    }
    Ok(library)
}

pub fn get_qir_runtime_lib_path() -> String {
    if cfg!(target_os = "linux") {
        "../qir-runtime/bin/linux-x64/native".to_owned()
    } else if cfg!(target_os = "windows") {
        "../qir-runtime/bin/win-x64/native".to_owned()
    } else if cfg!(target_os = "mac_os") {
        "../qir-runtime/bin/osx-x64/native".to_owned()
    } else {
        panic!("Unsupported platform")
    }
}

fn get_output_path() -> PathBuf {
    return PathBuf::from(env::var("OUT_DIR").unwrap());
}

pub fn link_runtime(manifest_dir: &str, library: &str) -> Result<(), Box<dyn Error>> {
    let output_path = get_output_path();
    let native_dir =
        fs::canonicalize(PathBuf::from(&manifest_dir).join(get_qir_runtime_lib_path()))?;
    println!("cargo:rustc-link-search=native={}", output_path.display());
    println!("cargo:rustc-link-lib=dylib={}", library);

    let name = library_filename(library)
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
