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

dir "C:\Program Files\LLVM\bin\"
clang.exe --version

cmake.exe --version

clang-format.exe --version

clang-tidy.exe --version

# Running CI
python manage runci
