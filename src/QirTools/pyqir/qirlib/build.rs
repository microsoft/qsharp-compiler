use std::env;
use std::error::Error;
use std::io::{self, Write};
use std::path::PathBuf;
use std::process::Command;

fn main() -> Result<(), Box<dyn Error>> {
    println!("cargo:rerun-if-env-changed=TARGET");
    println!("cargo:rerun-if-changed=build.rs");
    println!("cargo:rerun-if-changed=runtime.csproj");

    let root_path = env::var("CARGO_MANIFEST_DIR")?;
    let packages = PathBuf::from(format!("{}/packages", root_path.clone()));

    let output = Command::new("dotnet")
        .current_dir(root_path.clone())
        .arg("restore")
        .arg("--packages")
        .arg(packages)
        .output()
        .expect("failed to execute process");

    println!("status: {}", output.status);
    io::stdout().write_all(&output.stdout).unwrap();
    io::stderr().write_all(&output.stderr).unwrap();

    assert!(output.status.success());

    Ok(())
}
