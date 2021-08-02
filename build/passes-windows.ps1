# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Continue'

python --version
pip --version

# Installing requirements for CI
cd ../src/Passes/
pip install -r requirements.txt



# Installing Clang, CMake and LLVM
if (!(Get-Command clang -ErrorAction SilentlyContinue)) {
    & (Join-Path $env:CONDA scripts conda.exe) install -c conda-forge llvm-tools=11.1.0
    & (Join-Path $env:CONDA scripts conda.exe) install -c conda-forge llvmdev=11.1.0
}

# if (!(Get-Command cmake -ErrorAction SilentlyContinue)) {
#     choco install cmake
# }

refreshenv
$env:Path += ";C:\Program Files\LLVM\bin\"


dir "C:\Program Files\LLVM\"
clang.exe --version

cmake.exe --version

clang-format.exe --version

clang-tidy.exe --version

cmd.exe /c "call `"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvars64.bat`" && set > %temp%\vcvars.txt"

Get-Content "$env:temp\vcvars.txt" | Foreach-Object {
  if ($_ -match "^(.*?)=(.*)$") {
    Set-Content "env:\$($matches[1])" $matches[2]
  }
}


# Running CI
python manage runci
