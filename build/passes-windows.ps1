# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

python --version
pip --version

# Installing requirements for CI
cd ../src/Passes/
pip install -r requirements.txt

# Installing Clang, CMake and LLVM
if (!(Get-Command clang -ErrorAction SilentlyContinue)) {
    choco install llvm --version=11.1.0
}

if (!(Get-Command cmake -ErrorAction SilentlyContinue)) {
    choco install cmake
}

refreshenv
$env:Path += ";C:\Program Files\LLVM\bin\"


dir "C:\Program Files\LLVM\"
clang.exe --version

cmake.exe --version

clang-format.exe --version

clang-tidy.exe --version


$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vcvarspath = &$vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
Write-Output "vc tools located at: $vcvarspath"
cmd.exe /c "call `"$vcvarspath\VC\Auxiliary\Build\vcvars64.bat`"


# Running CI
python manage runci
