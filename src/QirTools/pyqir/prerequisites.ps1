# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Test-RustInstalled {
    $null -ne (Get-Command rustup -ErrorAction SilentlyContinue)
}

function Install-Rust {
    if (Test-RustInstalled) {
        Write-Host "Rust already installed"
    }
    else {
        if ($IsWindows -or $PSVersionTable.PSEdition -eq "Desktop") {
            Invoke-WebRequest "https://win.rustup.rs" -OutFile rustup-init.exe
            Unblock-File rustup-init.exe;
            & ./rustup-init.exe -y
            Remove-Item rustup-init.exe
        }
        elseif ($IsLinux -or $IsMacOS) {
            Invoke-WebRequest "https://sh.rustup.rs" | Select-Object -ExpandProperty Content | sh -s -- -y;
        }
        else {
            Write-Error "Host operating system not recognized as being Windows, Linux, or macOS; please download Rust manually from https://rustup.rs/."
        }

        if (-not (Get-Command rustup -ErrorAction SilentlyContinue)) {
            Write-Error "After running rustup-init, rustup was not available. Please check logs above to see if something went wrong.";
            exit -1;
        }
    }
}

function Install-RustPackages {
    Write-Host "Installing rustfmt and clippy"
    rustup component add rustfmt clippy
}

function Get-Python3Command {
    # First check if python3 is installed as python
    # save the choice, but python3 still may not exist
    $python = "python3"
    if ($null -ne (Get-Command python -ErrorAction SilentlyContinue)) {
        $pythonIsPython3 = (python --version) -match "Python 3.*"
        if ($pythonIsPython3) {
            $python = "python"
        }
    }
    $python
}

function Test-PythonInstalled {
    $python = Get-Python3Command
    $null -ne (Get-Command $python -ErrorAction SilentlyContinue)
}

function Install-Python {
    $python = Get-Python3Command
    if (Test-PythonInstalled) {
        Write-Host "Python3 already installed"
    }
    else {
        if ($IsWindows) {
            $exe = 'python-3.9.7-amd64.exe'
            $expectedHash = 'cc3eabc1f9d6c703d1d2a4e7c041bc1d'
            Invoke-WebRequest "https://www.python.org/ftp/python/3.9.7/$exe" -OutFile $exe
            $calculatedHash = (Get-FileHash -Path $exe -Algorithm MD5).Hash
            if ($expectedHash -ne $calculatedHash) {
                Write-Error "The calculated hash for the python3 installation file did not match the expected value.";
                exit -1
            }
            Unblock-File $exe
            & ./$exe
            Remove-Item $exe
        }
        elseif ($IsLinuxS) {
            sudo apt-get install -y --no-install-recommends python3-dev python3-pip
        }
        elseif ($IsMacOS) {
            brew install 'python@3.9'
        }
        else {
            Write-Error "Host operating system not recognized as being Windows, Linux, or macOS; please download Rust manually from https://rustup.rs/."
        }

        if (-not (Get-Command $python -ErrorAction SilentlyContinue)) {
            Write-Error "After running python installers, python3 was not available. Please check logs above to see if something went wrong.";
            exit -1;
        }
    }
}

function Install-PythonPackages {
    Write-Host "Installing Python dev packages pip, maturin, and tox"
    $python = Get-Python3Command
    & $python -m pip install --user -U pip
    & $python -m pip install --user maturin tox
}

Install-Rust
Install-RustPackages

Install-Python
Install-PythonPackages
