# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (-not (Get-Command rustup -ErrorAction SilentlyContinue)) {
    if ($IsWindows -or $PSVersionTable.PSEdition -eq "Desktop") {
        Invoke-WebRequest "https://win.rustup.rs" -OutFile rustup-init.exe
        Unblock-File rustup-init.exe;
        ./rustup-init.exe -y
    } elseif ($IsLinux -or $IsMacOS) {
        Invoke-WebRequest "https://sh.rustup.rs" | Select-Object -ExpandProperty Content | sh -s -- -y;
    } else {
        Write-Error "Host operating system not recognized as being Windows, Linux, or macOS; please download Rust manually from https://rustup.rs/."
    }

    if (-not (Get-Command rustup -ErrorAction SilentlyContinue)) {
        Write-Error "After running rustup-init, rustup was not available. Please check logs above to see if something went wrong.";
        exit -1;
    }
}

rustup component add rustfmt clippy
