# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

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
    $Env:SCCACHE_CACHE_SIZE = "2G"
    Write-Vso "Starting sccache server"
    & { sccache --start-server } -ErrorAction SilentlyContinue
    Write-Vso "Started sccache server"
}
elseif (Test-CommandExists ccache) {
    Write-Vso "Found ccache command"

    if (-not (Test-Path env:\CCACHE_DIR)) {
        # CCACHE_DIR needs to be set, get the value from ccache
        $Env:CCACHE_DIR = exec { ccache -k cache_dir }
    }
    Assert (![string]::IsNullOrWhiteSpace($Env:CCACHE_DIR)) "CCACHE_DIR is not set"

    # Set cap and make sure dir is created
    if (!(Test-Path $Env:CCACHE_DIR)) {
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
if ($IsLinux) {
    Assert (Test-CommandExists "docker") "Docker not found"
}

if (!(Test-Path $llvmBuildDir)) {
    mkdir $llvmBuildDir | Out-Null
}

Write-Vso "Generating package: $($Env:PKG_NAME)"

if ($IsLinux) {
    function Build-ContainerImage {

        Write-Vso "Building container image manylinux-llvm-builder"
        exec -wd $buildDir {
            $userName = [Environment]::UserName
            $userId = $(id -u)
            $groupId = $(id -g)
            Write-Vso "Get-Content manylinux.Dockerfile | docker build -t manylinux-llvm-builder --build-arg USERNAME=$userName --build-arg USER_UID=$userId --build-arg USER_GID=$groupId --build-arg LLVM_BUILD_DIR=""$llvmBuildDir"" -" "command"
            Get-Content manylinux.Dockerfile | docker build -t manylinux-llvm-builder --build-arg USERNAME=$userName --build-arg USER_UID=$userId --build-arg USER_GID=$groupId --build-arg LLVM_BUILD_DIR="$llvmBuildDir" -
        }
    }

    function Invoke-ContainerImage {
        # Verify input files/folders and mounts exist
        Assert (Test-Path $llvmCmakeFile) "$($llvmCmakeFile) is missing"
        Assert (Test-Path $llvmDir) "$($llvmDir) is missing"
        Assert (Test-Path $llvmBuildDir) "$($llvmBuildDir) is missing"

        Assert (Test-Path $srcRoot) "$($srcRoot) is missing"

        Write-Vso "Running container image:"
        $srcVolume = "$($srcRoot):$($srcRoot)"
        $cacheVolume = "$($cacheRoot):$($cacheRoot)"
        $userSpec = [Environment]::UserName

        if (Test-Path env:\CMAKE_INSTALL_PREFIX) {
            if (!(Test-Path $env:CMAKE_INSTALL_PREFIX)) {
                New-Item -ItemType Directory -Path $env:CMAKE_INSTALL_PREFIX -Force | Out-Null
            }
            $cmakeInstallVolume = "$($env:CMAKE_INSTALL_PREFIX):$($env:CMAKE_INSTALL_PREFIX)"
            Write-Vso "docker run --rm -t --user $userSpec -e PKG_NAME=$($Env:PKG_NAME) -e SOURCE_DIR=$srcRoot -e LLVM_CMAKEFILE=$llvmCmakeFile -e LLVM_DIR=$llvmDir -e LLVM_BUILD_DIR=$llvmBuildDir -e CCACHE_DIR=$cacheRoot -e CCACHE_CONFIGPATH=$cacheRoot -v $srcVolume -v $cacheVolume -v $cmakeInstallVolume -e LLVM_INSTALL_DIR=$Env:CMAKE_INSTALL_PREFIX -e CMAKE_FLAGS=""-D CMAKE_INSTALL_PREFIX=$($Env:CMAKE_INSTALL_PREFIX)"" manylinux-llvm-builder" "command"
            exec {
                docker run --rm -t --user $userSpec -e PKG_NAME=$($Env:PKG_NAME) -e SOURCE_DIR=$srcRoot -e LLVM_CMAKEFILE=$llvmCmakeFile -e LLVM_DIR=$llvmDir -e LLVM_BUILD_DIR=$llvmBuildDir -e CCACHE_DIR=$cacheRoot -e CCACHE_CONFIGPATH=$cacheRoot -v $srcVolume -v $cacheVolume -v $cmakeInstallVolume -e LLVM_INSTALL_DIR=$Env:CMAKE_INSTALL_PREFIX -e CMAKE_FLAGS="-D CMAKE_INSTALL_PREFIX=$($Env:CMAKE_INSTALL_PREFIX)" manylinux-llvm-builder
            }
        }
        else {
            Write-Vso "docker run --rm -t --user $userSpec -e PKG_NAME=$($Env:PKG_NAME) -e SOURCE_DIR=$srcRoot -e LLVM_CMAKEFILE=$llvmCmakeFile -e LLVM_DIR=$llvmDir -e LLVM_BUILD_DIR=$llvmBuildDir -e CCACHE_DIR=$cacheRoot -e CCACHE_CONFIGPATH=$cacheRoot -v $srcVolume -v $cacheVolume manylinux-llvm-builder" "command"
            exec {
                docker run --rm -t --user $userSpec -e PKG_NAME=$($Env:PKG_NAME) -e SOURCE_DIR=$srcRoot -e LLVM_CMAKEFILE=$llvmCmakeFile -e LLVM_DIR=$llvmDir -e LLVM_BUILD_DIR=$llvmBuildDir -e CCACHE_DIR=$cacheRoot -e CCACHE_CONFIGPATH=$cacheRoot -v $srcVolume -v $cacheVolume manylinux-llvm-builder
            }
        }
    }

    Build-ContainerImage
    Invoke-ContainerImage
}
else {
    if ($IsWindows) {
        . (Join-Path $PSScriptRoot "vcvars.ps1")
    }

    exec -wd $llvmBuildDir {
        Write-Vso "Generating makefiles"
        $flags = ""
        if (Test-Path env:\CMAKE_INSTALL_PREFIX) {
            $flags += "-D CMAKE_INSTALL_PREFIX=""$($Env:CMAKE_INSTALL_PREFIX)"""
        }
        cmake -G Ninja -C $llvmCmakeFile $flags $llvmDir
    }

    exec -wd $llvmBuildDir {
        Write-Vso "ninja package" "command"
        ninja package
    }

    if ((Test-Path Env:\INSTALL_LLVM_PACKAGE) -and ($true -eq $Env:INSTALL_LLVM_PACKAGE)) {
        Write-Vso "ninja install" "command"
        exec -wd $llvmBuildDir {
            ninja install
        }
    }
}

exec -wd $llvmBuildDir {
    $package = Resolve-Path "$($Env:PKG_NAME)*" -ErrorAction SilentlyContinue
    Assert ($null -ne $package) "Package is null"
    Assert (Test-Path $package) "Could not resolve package $package"
}
