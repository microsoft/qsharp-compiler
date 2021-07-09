## Functional Summary

Python API for generating QIR modules.

## Tech Overview

The project is a mix of Python and Rust. The project is built as a platform dependent Python wheel which provides a thin API surface over a compiled Rust `cdylib`. This `cdylib` is statically linked against LLVM 11. This gives us a 4MB~12MB wheel.

Projects like `llvmlite` which provide a platform independent LLVM shim require that the user has installed LLVM correctly on their system which is a barrier to entry for users. 
To make a transparent update from the pass-thru API phase to supporting QIR, this library bundles all required libraries.
This changes the build requirements for the module as it now has to be built per platform. There is no avoiding the need for platform specific binaries; however, we can choose whether the user or our build systems take that cost.

The Rust project uses:
- [inkwell](https://github.com/TheDan64/inkwell): Idiomatic Safe Rust bindings for LLVM. Leverages `llvm-sys` as a transitive dependency which can also be leveraged for functionality that may not be exposed via `inkwell`.
  - Apache License 2.0
  - 814 stars, 84 forks, active development and support for latest LLVM versions
  - Now downloaded via crates.io
- [llvm-sys](https://crates.io/crates/llvm-sys): Rust bindings to LLVM's C API. This requires LLVM be installed on the system building the Rust code.
  - MIT License
  - 225k downloads, 30k downloads of latest version, actively developed
- [pyo3](https://crates.io/crates/pyo3): Rust bindings for Python. This includes running and interacting with Python code from a Rust binary, as well as writing native Python modules.
  - Also leveraged by the experimental QIR Rust runtime.
  - 2MM+ downloads, 992k recent downloads
  - Apache License 2.0
- [LLVM]
  - [Apache License 2.0 with exceptions](https://releases.llvm.org/11.0.0/LICENSE.TXT)

The Python project uses [qiskit-terra](https://pypi.org/project/qiskit-terra/) for `QuantumCircuit` processing along with dev dependencies:

- [setuptools-rust](https://pypi.org/project/setuptools-rust/): setuptools-rust is a plugin for setuptools to build Rust Python extensions implemented with PyO3 or rust-cpython.
  - MIT License
- [setuptools](https://pypi.org/project/setuptools/): Easily download, build, install, upgrade, and uninstall Python packages
  - MIT License
- [wheel](https://pypi.org/project/wheel/): This library is the reference implementation of the Python wheel packaging standard, as defined in PEP 427.
  - MIT License
- [pytest](https://pypi.org/project/pytest/): The pytest framework makes it easy to write small tests, yet scales to support complex functional testing for applications and libraries.
  - MIT License

### IR

The project uses `module.ll` produced from a minimal Q# compilation in `module.qs` and compiled with `llvm-as-11` to create `module.bc`. This module contains the `QIR` generated.

This should allow us to load the QIR and replace the entrypoint method body with the circuit translation.

## Building

This repo includes a dev container which will install all base requirements. If you with to build locally without a dev container, you'll need to install:

- LLVM 11 ([Ubuntu](https://apt.llvm.org/), Windows)
- Rust > 1.48
- Python 3.6+

## Dev workflow

### Building and Running Tests

In order to run the python tests, the module needs to be installed. This setup will generate and install an egg. The `./build_wheels.sh` script can also be run if you wish to build and install a wheel.

If this is the first time you've loaded the code we need to install the python dev packages:

```bash
python3 -m pip install -r requirements-dev.txt
```

After which we can build everything and install the module locally:

```bash
python3 setup.py install --user
```

Once installed, the tests can be run:

```bash
pytest -v tests
```

## TODO

 - Currently the build only works for the single Python version installed. Future work may include:
   - Creating ABI3 compatable wheels which can be installed across Python 3.5+ to minimize the number of builds which need to be created
   - Create Windows and Mac (x64 and aarch64) scripts