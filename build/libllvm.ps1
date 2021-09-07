# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

. (Join-Path $PSScriptRoot "utils.ps1")

Assert (Test-Path env:\PKG_NAME) "PKG_NAME was not defined"
Assert (Test-Path env:\LINUX_PKG_NAME) "LINUX_PKG_NAME was not defined"
Assert (Test-Path env:\WINDOWS_PKG_NAME) "WINDOWS_PKG_NAME was not defined"
Assert (Test-Path env:\DARWIN_PKG_NAME) "DARWIN_PKG_NAME was not defined"
Assert (Test-Path env:\AQ_LLVM_PACKAGE_GIT_VERSION) "AQ_LLVM_PACKAGE_GIT_VERSION was not defined"
Assert (Test-Path env:\BLOBS_OUTDIR) "BLOBS_OUTDIR was not defined"
Assert (Test-Path env:\BUILD_ARTIFACTSTAGINGDIRECTORY) "BUILD_ARTIFACTSTAGINGDIRECTORY was not defined"

$llvmArchiveFiles = @(
    "$($env:LINUX_PKG_NAME)/external/llvm-project/build/$($env:LINUX_PKG_NAME).tar.gz",
    "$($env:WINDOWS_PKG_NAME)/external/llvm-project/build/$($env:WINDOWS_PKG_NAME).zip",
    "$($env:DARWIN_PKG_NAME)/external/llvm-project/build/$($env:DARWIN_PKG_NAME).tar.gz"
)

for ($i = 0; $i -lt $llvmArchiveFiles.Count; $i++) {
    $file = $llvmArchiveFiles[$i]
    Write-Vso "Raw: $file"
    $path = Join-Path (Get-Location) '..' $file
    Write-Vso "Joined: $path"
    $resolvedPath = Resolve-Path $path
    Write-Vso "Resolved: $resolvedPath"
    $llvmArchiveFiles[$i] = $resolvedPath
}

$artifactsDir = $env:BLOBS_OUTDIR
if (!(Test-Path $artifactsDir)) {
    mkdir $artifactsDir | Out-Null
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
        Write-Vso "Processing $tarGz"
        exec { 7z x $tarGz }
        $tar = $tarGz.Name.TrimEnd($extension)
        Write-Vso "Processing $tar"
        exec { 7z x -aoa -ttar $tar }
    }
}

# Windows: bin/LLVM-C.dll
# Linux: lib/libLLVM-11.so
# Darwin: lib/libLLVM.dylib

$linuxPath = Resolve-Path (Join-Path  '.' "$($env:LINUX_PKG_NAME)" "lib" "libLLVM-[0-9][0-9].so")
Write-Vso "Test Linux Path: $linuxPath"
Test-Path $linuxPath

$windowsPath = Resolve-Path (Join-Path  '.' "$($env:WINDOWS_PKG_NAME)" "bin" "LLVM-C.dll")
Write-Vso "Test Windows Path: $windowsPath"
Test-Path $windowsPath

$darwinPath = Resolve-Path (Join-Path  '.' "$($env:DARWIN_PKG_NAME)" "lib" "libLLVM.dylib")
Write-Vso "Test Darwin Path: $darwinPath"
Test-Path $darwinPath

mkdir $env:PKG_NAME | Out-Null
$packageDir = Resolve-Path "$($env:PKG_NAME)"

Write-Vso "Move-Item -Path $linuxPath -Destination $(Join-Path $packageDir 'libLLVM.so')"
Move-Item -Path $linuxPath -Destination (Join-Path $packageDir "libLLVM.so")

Write-Vso "Move-Item -Path $windowsPath -Destination $(Join-Path $packageDir 'libLLVM.dll')"
Move-Item -Path $windowsPath -Destination (Join-Path $packageDir "libLLVM.dll")

Write-Vso "Move-Item -Path $darwinPath -Destination $(Join-Path $packageDir 'libLLVM.dylib')"
Move-Item -Path $darwinPath -Destination (Join-Path $packageDir "libLLVM.dylib")

$libLlvmPackage = Join-Path "$($env:BUILD_ARTIFACTSTAGINGDIRECTORY)" "$($env:PKG_NAME).zip"
$compress = @{
    Path             = $packageDir
    CompressionLevel = "Fastest"
    DestinationPath  = $libLlvmPackage
}
Compress-Archive @compress

foreach ($file in $llvmArchiveFiles) {
    Move-Item -Path $file -Destination $artifactsDir
}

# Add it the job's artifacts
Copy-Item -Path $libLlvmPackage -Destination $artifactsDir

$artifacts = Get-ChildItem -File $artifactsDir
foreach ($artifact in $artifacts) {
    $hash = (Get-FileHash -Path $artifact -Algorithm SHA256).Hash
    Set-Content -Path "$artifact.sha256" -Value $hash
}

Write-Vso "Artifacts for upload:"
$artifacts = Get-ChildItem -File $artifactsDir
foreach ($artifact in $artifacts) {
    Write-Vso $artifact
}
