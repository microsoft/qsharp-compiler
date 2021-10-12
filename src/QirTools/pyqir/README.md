# Functional Summary

Python API for generating QIR modules.

## IR

The project uses `module.ll` produced from a minimal Q# compilation in `module.qs` and compiled with `llvm-as-11` to create `module.bc`. This module contains the `QIR` generated.

This allows the loading of base QIR profile from which the entrypoint is replaced with a method body containing the circuit translation.

# Building

## Local Environment

If you wish to set up your own local environment manually, follow one of the sections below. If you have [PowerShell](https://github.com/PowerShell/PowerShell) installed, most can be automated by running `bootstrap.ps1` in the repo root. This will install Rust and Python3 onto your system. You will likely be prompted to approve actions taken for `sudo` on Linux or the [UAC](https://docs.microsoft.com/en-us/windows/security/identity-protection/user-account-control/user-account-control-overview) on Windows.

### Linux (Ubuntu)

Install python and libs:

```bash
sudo apt-get install -y --no-install-recommends python3-dev python3-pip
python3 -m pip install --user -U pip
python3 -m pip install --user maturin tox
```

Install Rust from [rustup](https://rustup.rs/).

### Windows

Install Python 3.6+ from one of the following and make sure it is added to the path.
- [Windows store](https://docs.microsoft.com/en-us/windows/python/beginners#install-python)
- [Miniconda](https://docs.conda.io/en/latest/miniconda.html#latest-miniconda-installer-links)
- [Python.org](https://www.python.org/downloads/)

In a command prompt:

```bash
python -m pip install --user maturin tox
```

Install Rust from [rustup](https://rustup.rs/).

### MacOS

Install Python 3.6+ from [Python.org](https://www.python.org/downloads/macos/).

or brew:
```
brew install 'python@3.9'
```

Install Rust from [rustup](https://rustup.rs/).

## Development

Running `build.ps1` will initialize your local environment and build the solution.

Build commands:
- `maturin build`: Build the crate into python packages
  - `maturin build --release`: Build and pass --release to cargo
  - `maturin build --help`: to view more options
- `maturin develop`: Installs the crate as module in the current virtualenv

### Environment Variables

- `AQ_LLVM_PACKAGE_GIT_VERSION`
  - `git rev-parse --short HEAD` of the LLVM submodule
- `AQ_LLVM_EXTERNAL_DIR`
  - Path to where LLVM is already installed by user.
- `AQ_DOWNLOAD_LLVM`
  - Indicator to whether the build should download LLVM cached builds.
  - Build will download LLVM if needed unless this variable is defined and set to `false`
- `AQ_LLVM_BUILDS_URL`
  - Url where LLVM builds will be downloaded.
  - Default: `https://msquantumpublic.blob.core.windows.net/llvm-builds`
- `AQ_CACHE_DIR`
  - Root insallation path for LLVM builds
  - Default if not specified:
    - Linux/Mac: `$HOME/.azure-quantum`
    - Windows: `$HOME\.azure-quantum`
  - CI:
    - `AQ_CACHE_DIR`: $(Pipeline.Workspace)/.cache/
- `LLVM_SYS_110_PREFIX`
  - Required by `llvm-sys` and will be set to the version of LLVM used for configuration.
  - Vesion dependent and will change as LLVM is updated. (`LLVM_SYS_120_PREFIX`, `LLVM_SYS_130_PREFIX`, etc)

### Top level Tox Usage

Currently the `LLVM_SYS_110_PREFIX` is passed into the `tox` environment for `llvm-sys` compilation. There is an issue with the `cc-rs` crate finding `cl.exe` on Windows right now so `passenv = *` is currently set until this can be resolved.

Two targets are available for tox:
- `python -m tox -e test`
  - Runs the python tests in an isolated environment
- `python -m tox -e pack`
  - Packages all wheels in an isolated environment

### Packaging

Note: For Windows, the packaging will look for python installations available and build for them. More information on [supporting multiple python versions on Windows](https://tox.readthedocs.io/en/latest/developers.html?highlight=windows#multiple-python-versions-on-windows)

For manylinux, this is controlled through the Docker image build in the `build.ps1`.

The builds target Python ABI 3.6.
