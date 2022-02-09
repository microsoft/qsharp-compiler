# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

. (Join-Path $PSScriptRoot .. build "set-env.ps1")
. (Join-Path $PSScriptRoot "utils.ps1")

Assert (Test-Path env:\PKG_NAME) "PKG_NAME was not defined"
Assert (Test-Path env:\LINUX_PKG_NAME) "LINUX_PKG_NAME was not defined"
Assert (Test-Path env:\WINDOWS_PKG_NAME) "WINDOWS_PKG_NAME was not defined"
Assert (Test-Path env:\DARWIN_PKG_NAME) "DARWIN_PKG_NAME was not defined"
Assert (Test-Path env:\BLOBS_OUTDIR) "BLOBS_OUTDIR was not defined"
Assert (Test-Path env:\BUILD_ARTIFACTSTAGINGDIRECTORY) "BUILD_ARTIFACTSTAGINGDIRECTORY was not defined"

$llvmArchiveFiles = @(
    "$($env:LINUX_PKG_NAME)/llvm-build/llvm-project/build/$($env:LINUX_PKG_NAME).tar.gz",
    "$($env:WINDOWS_PKG_NAME)/llvm-build/llvm-project/build/$($env:WINDOWS_PKG_NAME).zip",
    "$($env:DARWIN_PKG_NAME)/llvm-build/llvm-project/build/$($env:DARWIN_PKG_NAME).tar.gz"
)

for ($i = 0; $i -lt $llvmArchiveFiles.Count; $i++) {
    $file = $llvmArchiveFiles[$i]
    Write-AdoLog "Raw: $file"
    $path = Join-Path (Get-Location) '..' $file
    Write-AdoLog "Joined: $path"
    $resolvedPath = Resolve-Path $path
    Write-AdoLog "Resolved: $resolvedPath"
    $llvmArchiveFiles[$i] = $resolvedPath
}

$artifactsDir = $env:BLOBS_OUTDIR
if (!(Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Force $artifactsDir | Out-Null
}
$artifactsDir = Resolve-Path "$artifactsDir"

foreach ($file in $llvmArchiveFiles) {
    $file = (Get-Item $file)
    $extension = $file.Extension
    if (".zip" -eq $extension) {
        exec { 7z x $file }
    }
    elseif (".gz" -eq $extension) {
        $tarGz = $file
        Write-AdoLog "Processing $tarGz"
        exec { 7z x $tarGz }
        $tar = $tarGz.Name.TrimEnd($extension)
        Write-AdoLog "Processing $tar"
        exec { 7z x -aoa -ttar $tar }
    }
}

# Windows: bin/LLVM-C.dll
# Linux: lib/libLLVM-11.so
# Darwin: lib/libLLVM.dylib

$linuxPath = Resolve-Path (Join-Path  '.' "$($env:LINUX_PKG_NAME)" "lib" "libLLVM-[0-9][0-9].so")
Write-AdoLog "Test Linux Path: $linuxPath"
Test-Path $linuxPath

$windowsPath = Resolve-Path (Join-Path  '.' "$($env:WINDOWS_PKG_NAME)" "bin" "LLVM-C.dll")
Write-AdoLog "Test Windows Path: $windowsPath"
Test-Path $windowsPath

$darwinPath = Resolve-Path (Join-Path  '.' "$($env:DARWIN_PKG_NAME)" "lib" "libLLVM.dylib")
Write-AdoLog "Test Darwin Path: $darwinPath"
Test-Path $darwinPath

New-Item -ItemType Directory -Force $env:PKG_NAME | Out-Null
$packageDir = Resolve-Path "$($env:PKG_NAME)"

Write-AdoLog "Move-Item -Path $linuxPath -Destination $(Join-Path $packageDir 'libLLVM.so')"
Move-Item -Path $linuxPath -Destination (Join-Path $packageDir "libLLVM.so")

Write-AdoLog "Move-Item -Path $windowsPath -Destination $(Join-Path $packageDir 'libLLVM.dll')"
Move-Item -Path $windowsPath -Destination (Join-Path $packageDir "libLLVM.dll")

Write-AdoLog "Move-Item -Path $darwinPath -Destination $(Join-Path $packageDir 'libLLVM.dylib')"
Move-Item -Path $darwinPath -Destination (Join-Path $packageDir "libLLVM.dylib")

$libLlvmPackage = Join-Path "$($env:BUILD_ARTIFACTSTAGINGDIRECTORY)" "$($env:PKG_NAME).zip"
$compress = @{
    Path             = $packageDir
    CompressionLevel = "Fastest"
    DestinationPath  = $libLlvmPackage
}
Compress-Archive @compress

# Create LlvmBindings.Interop wrappers
Write-AdoLog "Creating Interop Bindings"
$includeDir = Join-Path  '.' "$($env:LINUX_PKG_NAME)" "include"
$generatedDir = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY generated
New-Item -ItemType Directory -Force $generatedDir | Out-Null
dotnet tool install --global ClangSharpPInvokeGenerator --version 13.0.0-beta1
ClangSharpPInvokeGenerator `
    "@$(Join-Path $PSScriptRoot GenerateLLVM.rsp)" `
    --output $generatedDir `
    --file-directory $includeDir `
    --include-directory $includeDir

New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot drops) | Out-Null

Write-AdoLog "Copy-Item -Path $(Join-Path $packageDir 'libLLVM.so') -Destination $(Join-Path $PSScriptRoot drops 'libLLVM.so')"
Copy-Item -Path $(Join-Path $packageDir 'libLLVM.so') -Destination (Join-Path $PSScriptRoot drops "libLLVM.so")

Write-AdoLog "Copy-Item -Path $(Join-Path $packageDir 'libLLVM.dll') -Destination $(Join-Path $PSScriptRoot drops 'libLLVM.dll')"
Copy-Item -Path $(Join-Path $packageDir 'libLLVM.dll') -Destination (Join-Path $PSScriptRoot drops "libLLVM.dll")

Write-AdoLog "Copy-Item -Path $(Join-Path $packageDir 'libLLVM.dylib') -Destination $(Join-Path $PSScriptRoot drops 'libLLVM.dylib')"
Copy-Item -Path $(Join-Path $packageDir 'libLLVM.dylib') -Destination (Join-Path $PSScriptRoot drops "libLLVM.dylib")

Write-AdoLog "Copy-Item -Recurse -Verbose -Path $generatedDir -Destination $(Join-Path $PSScriptRoot drops)"
Copy-Item -Recurse -Verbose -Path $generatedDir -Destination $(Join-Path $PSScriptRoot drops)

foreach ($file in $llvmArchiveFiles) {
    Move-Item -Path $file -Destination $artifactsDir
}

# Add the zipped binaries to the job's artifacts
Copy-Item -Path $libLlvmPackage -Destination $artifactsDir

$artifacts = Get-ChildItem -File $artifactsDir
foreach ($artifact in $artifacts) {
    $hash = (Get-FileHash -Path $artifact -Algorithm SHA256).Hash
    Set-Content -Path "$artifact.sha256" -Value $hash
}

Write-AdoLog "Artifacts for upload:"
$artifacts = Get-ChildItem -File $artifactsDir
foreach ($artifact in $artifacts) {
    Write-AdoLog $artifact
}
