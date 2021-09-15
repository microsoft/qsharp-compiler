# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Get-RepoRoot {
    git rev-parse --show-toplevel
}

. (Join-Path $PSScriptRoot "utils.ps1")

# Build

$srcRoot = Get-RepoRoot
$buildDir = Join-Path $srcRoot build
$llvmCmakeFile = Join-Path $buildDir llvm.cmake
$llvmRootDir = Join-Path $srcRoot external llvm-project
$llvmBuildDir = Join-Path $llvmRootDir build
$llvmDir = Join-Path $llvmRootDir llvm

if (Test-CommandExists sccache) {
    Write-Vso "Found sccache command"
    # Set cap and make sure dir is created
    if((Test-Path Env:\SCCACHE_DIR)) {
        mkdir $Env:SCCACHE_DIR | Out-Null
        $Env:SCCACHE_DIR = Resolve-Path $Env:SCCACHE_DIR
    }
    $Env:SCCACHE_CACHE_SIZE = "2G"
    & { sccache --start-server } -ErrorAction SilentlyContinue
} elseif (Test-CommandExists ccache) {
    Write-Vso "Found ccache command"

    if (-not (Test-Path env:\CCACHE_DIR)) {
        # CCACHE_DIR needs to be set, get the value from ccache
        $Env:CCACHE_DIR = exec { ccache -k cache_dir }
    }
    Assert (![string]::IsNullOrWhiteSpace($Env:CCACHE_DIR)) "CCACHE_DIR is not set"

    # Set cap and make sure dir is created
    if(!(Test-Path $Env:CCACHE_DIR)) {
        mkdir $Env:CCACHE_DIR | Out-Null
    }
    $Env:CCACHE_DIR = Resolve-Path $Env:CCACHE_DIR
    ccache -M 2G

    Write-Vso "Found ccache config:"
    ccache --show-config

    $cacheRoot = $Env:CCACHE_DIR

    Assert (Test-Path $cacheRoot) "$($cacheRoot) is missing"
    Write-Vso "Using CCACHE_DIR: $($Env:CCACHE_DIR)"
}
else {
    Write-Vso "Did not find ccache command"
}

Assert (Test-Path $llvmDir) "llvm-project submodule is missing"
Assert (![string]::IsNullOrWhiteSpace($Env:PKG_NAME)) "PKG_NAME is not set"

Assert (Test-CommandExists "cmake") "CMAKE not found"
Assert (Test-CommandExists "ninja") "Ninja-Build not found"
if($IsLinux) {
    Assert (Test-CommandExists "docker") "Docker not found"
}

if(!(Test-Path $llvmBuildDir)) {
    mkdir $llvmBuildDir | Out-Null
}

Write-Vso "Generating package: $($Env:PKG_NAME)"

if ($IsLinux) {
    function Build-ContainerImage {

        Write-Vso "Building container image manylinux-llvm-builder"
        exec -wd $buildDir {
            Get-Content manylinux.Dockerfile | docker build -t manylinux-llvm-builder --build-arg LLVM_BUILD_DIR="$llvmBuildDir" -
        }
    }

    function Invoke-ContainerImage {
        # Verify input files/folders and mounts exist
        Assert (Test-Path $llvmCmakeFile) "$($llvmCmakeFile) is missing"
        Assert (Test-Path $llvmDir) "$($llvmDir) is missing"
        Assert (Test-Path $llvmBuildDir) "$($llvmBuildDir) is missing"

        Assert (Test-Path $srcRoot) "$($srcRoot) is missing"

        Write-Vso "Running container image:"
        $command = "docker run --rm -t --user vsts -e PKG_NAME=$($Env:PKG_NAME) -e SOURCE_DIR=$srcRoot -e LLVM_CMAKEFILE=$llvmCmakeFile -e LLVM_DIR=$llvmDir -e LLVM_BUILD_DIR=$llvmBuildDir -e CCACHE_DIR=$cacheRoot -e CCACHE_CONFIGPATH=$cacheRoot -v $($srcRoot):$($srcRoot) -v $($cacheRoot):$($cacheRoot) manylinux-llvm-builder"
        if(Test-Path env:\CMAKE_INSTALL_PREFIX) {
            $command = "docker run --rm -t --user vsts -e CMAKE_INSTALL_PREFIX=$($Env:CMAKE_INSTALL_PREFIX) -e PKG_NAME=$($Env:PKG_NAME) -e SOURCE_DIR=$srcRoot -e LLVM_CMAKEFILE=$llvmCmakeFile -e LLVM_DIR=$llvmDir -e LLVM_BUILD_DIR=$llvmBuildDir -e CCACHE_DIR=$cacheRoot -e CCACHE_CONFIGPATH=$cacheRoot -v $($srcRoot):$($srcRoot) -v $($cacheRoot):$($cacheRoot) manylinux-llvm-builder"
        }

        Write-Vso $command
        exec {
            Invoke-Expression $command
        }
    }

    Build-ContainerImage
    Invoke-ContainerImage
}
else {

    if ($IsWindows) {
        # find VS root
        $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        $visualStudioPath = & $vswhere -prerelease -latest -property installationPath
        Write-Output "vs located at: $visualStudioPath"

        # Call vcvars64.bat and write the set calls to file
        cmd.exe /c "call `"$visualStudioPath\VC\Auxiliary\Build\vcvars64.bat`" && set > %temp%\vcvars.txt"

        # Read the set calls and set the corresponding pwsh env vars
        Get-Content "$Env:temp\vcvars.txt" | Foreach-Object {
            if ($_ -match "^(.*?)=(.*)$") {
                Set-Content "env:\$($matches[1])" $matches[2]
                Write-Host "setting env: $($matches[1]) = $($matches[2])"
            }
        }
    }

    exec -wd $llvmBuildDir {
        Write-Vso "Generating makefiles"
        cmake -G Ninja -C $llvmCmakeFile $llvmDir
    }

    exec -wd $llvmBuildDir {
        Write-Vso "ninja package" "command"
        ninja package
    }
}

exec -wd $llvmBuildDir {
    $package = Resolve-Path "$($Env:PKG_NAME)*"
    Assert (Test-Path $package) "Could not resolve package $package"
}

if((Test-Path Env:\INSTALL_LLVM_PACKAGE) -and ($true -eq $Env:INSTALL_LLVM_PACKAGE)) {
    Write-Vso "ninja install" "command"
    exec -wd $llvmBuildDir {
        ninja install
    }
}
