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

#### Prerequisites

##### Windows **Experimental**

Install [miniconda](https://docs.conda.io/en/latest/miniconda.html#latest-miniconda-installer-links).

In an Administrator command prompt:
```bash
python -m pip install maturin
```

In a command prompt:

```bash
conda install -y -c conda-forge llvm-tools=11.1.0 llvmdev=11.1.0 clang=11.1.0 cmake=3.20.4 ninja=1.10.2
python -m pip install -U --user tox
```

##### Linux
Install LLVM via [LLVM's apt source](https://apt.llvm.org/) or via conda/miniconda like on Windows.

```bash
python -m pip install --user maturin tox
```

#### Development
If this is the first time you've loaded the code we need to install the python dev packages:

Set the `LLVM_SYS_110_PREFIX` environment variable to point to your LLVM installation. The defaults are:
- Windows (miniconda): `C:\Users\iadavis\Miniconda3\Library\`
- Debian: `/usr/lib/llvm-11`

```bash
python3 -m pip install -r requirements-dev.txt
```

After which we can build everything and install the module locally:

```bash
python3 setup.py install --user
```

#### Top level Tox Usage

Currently the `LLVM_SYS_110_PREFIX` is passed into the `tox` environment for `llvm-sys` compilation. There is an issue with the `cc-rs` crate finding `cl.exe` on Windows right now so `passenv = *` is currently set until this can be resolved.

Two targets are available for tox:
- `python -m tox -e test`
  - Runs the python tests in an isolated environment
- `python -m tox -e pack`
  - Packages all wheels in an isolated environment

### Packaging

Note: For Windows, the packaging will look for python installations available and build for them. More information on [supporting multiple python versions on Windows](https://tox.readthedocs.io/en/latest/developers.html?highlight=windows#multiple-python-versions-on-windows)

For manylinux, this is controlled through the Docker image build.

```bash
docker build -t manylinux2014_x86_64_llvm11 --target manylinux2014_x86_64_llvm -f qirlib\manylinux.Dockerfile .

docker build -t manylinux2014_x86_64_llvm11_maturin -f qirlib\manylinux.Dockerfile .

docker run --rm -v $(pwd):/io -e LLVM_SYS_110_PREFIX manylinux2010_x86_64_llvm11_maturin build --release
```

## TODO

 - Future work may include:
   - Creating ABI3 compatable wheels which can be installed across Python 3.5+ to minimize the number of builds which need to be created
   - Create Windows and Mac (x64 and aarch64) scripts
