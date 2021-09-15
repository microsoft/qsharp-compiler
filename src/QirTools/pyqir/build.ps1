# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Get-RepoRoot {
    git rev-parse --show-toplevel
}

. (Join-Path (Get-RepoRoot) build "utils.ps1")

function Get-CommitHash {
    # rev-parse changes length based on your git settings, use length = 9
    # to match azure devops
    exec { git rev-parse --short=9 HEAD }
}

function Get-TargetTriple {
    $triple = "unknown"
    if ($IsWindows) {
        $triple = "x86_64-pc-windows-msvc-static"
    }
    elseif ($IsLinux) {
        $triple = "x86_64-unknown-linux-gnu"
    }
    elseif($IsMacOS) {
        $triple = "x86_64-apple-darwin"
    }
    $triple
}

function Use-LlvmInstallation {
    param (
        [string]$path
    )
    Write-Vso "LLVM installation set to: $path"
    $env:AQ_LLVM_INSTALL_DIR = $path
    $env:LLVM_SYS_110_PREFIX = $path
}

function Use-ExternalLlvmInstallation {
    Write-Vso "Using LLVM installation specified by AQ_LLVM_EXTERNAL_DIR"
    Assert (Test-Path $env:AQ_LLVM_EXTERNAL_DIR) "AQ_LLVM_EXTERNAL_DIR folder does not exist"
    Use-LlvmInstallation $env:AQ_LLVM_EXTERNAL_DIR
}

function Get-LlvmSha {
    if (Test-Path env:\AQ_LLVM_PACKAGE_GIT_VERSION) {
        $env:AQ_LLVM_PACKAGE_GIT_VERSION
    }
    else {
        $srcRoot = Get-RepoRoot
        if (Test-Path env:\BUILD_SOURCESDIRECTORY) {
            Write-Vso "Build.SourcesDirectory: $($env:BUILD_SOURCESDIRECTORY)"
            $srcRoot = Resolve-Path $($env:BUILD_SOURCESDIRECTORY)
        }
        $llvmDir = Join-Path $srcRoot external llvm-project

        Assert (Test-Path $llvmDir) "llvm-project submodule is missing"
        $sha = exec -wd $llvmDir { Get-CommitHash }
        $sha
    }
}

function Get-PackageName {
    $sha = Get-LlvmSha
    $TARGET_TRIPLE = Get-TargetTriple
    $packageName = "aq-llvm-$($TARGET_TRIPLE)-$($sha)"
    $packageName
}

function Get-PackageExt {
    $extension = ".tar.gz"
    if ($IsWindows) {
        $extension = ".zip"
    }
    $extension
}

function Test-AllowedToDownloadLlvm {
    # If AQ_DOWNLOAD_LLVM isn't set, we allow for download
    # If it is set, then we use its value
    !((Test-Path env:\AQ_DOWNLOAD_LLVM) -and ($env:AQ_DOWNLOAD_LLVM -eq $false))
}

function Get-AqCacheDirectory {
    $aqCacheDirectory = (Get-DefaultInstallDirectory)
    if (!(Test-Path $aqCacheDirectory)) {
        mkdir $aqCacheDirectory | Out-Null
    }
    Resolve-Path $aqCacheDirectory
}

function Get-LlvmDownloadBaseUrl {
    if (Test-Path env:\AQ_LLVM_BUILDS_URL) {
        $env:AQ_LLVM_BUILDS_URL
    }
    else
    { "https://msquantumpublic.blob.core.windows.net/llvm-builds" }
}

function Get-LlvmArchiveUrl {
    $extension = Get-PackageExt
    $baseUrl = Get-LlvmDownloadBaseUrl
    "$baseUrl/$($packageName)$extension"
}

function Get-LlvmArchiveShaUrl {
    $extension = Get-PackageExt
    $baseUrl = Get-LlvmDownloadBaseUrl
    "$baseUrl/$($packageName)$extension.sha256"
}

function Get-LlvmArchiveFileName {
    $packageName = Get-PackageName
    $extension = Get-PackageExt
    "$($packageName)$extension"
}

function Get-LlvmArchiveShaFileName {
    $filename = Get-LlvmArchiveFileName
    "$filename.sha256"
}

function Get-DefaultInstallDirectory {
    if(Test-Path env:\AQ_CACHE_DIR) {
        $env:AQ_CACHE_DIR
    } else {
        Join-Path "$HOME" ".azure-quantum"
    }
}

function Get-InstallationDirectory {
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $packageName
    )
    $aqCacheDirectory = Get-AqCacheDirectory
    $packagePath = Join-Path $aqCacheDirectory $packageName
    $packagePath
}

function Install-LlvmFromBuildArtifacts {
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $packagePath
    )

    $outFile = Join-Path $($env:TEMP) (Get-LlvmArchiveFileName)
    if (!(Test-Path $outFile)) {
        $archiveUrl = Get-LlvmArchiveUrl
        Write-Vso "Dowloading $archiveUrl to $outFile"
        Invoke-WebRequest -Uri $archiveUrl -OutFile $outFile
    }

    $shaFile = Join-Path $($env:TEMP) (Get-LlvmArchiveShaFileName)
    if (!(Test-Path $shaFile)) {
        $sha256Url = Get-LlvmArchiveShaUrl
        Write-Vso "Dowloading $sha256Url to $shaFile"
        Invoke-WebRequest -Uri $sha256Url -OutFile $shaFile
    }

    Write-Vso "Calculating hash for $outFile"
    $calculatedHash = (Get-FileHash -Path $outFile -Algorithm SHA256).Hash

    Write-Vso "Reading hash from $shaFile"
    $expectedHash = (Get-Content -Path $shaFile)

    Assert ("$calculatedHash" -eq "$expectedHash") "The calculated hash $calculatedHash did not match the expected hash $expectedHash"

    $packagesRoot = Get-AqCacheDirectory
    if ($IsWindows) {
        Expand-Archive -Path $outFile -DestinationPath $packagesRoot
    }
    else {
        tar -zxvf $outFile -C $packagesRoot
    }

    $packageName = Get-PackageName
    $packagePath = Get-InstallationDirectory $packageName
    Use-LlvmInstallation $packagePath
}

function Install-LlvmFromSource {
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $packagePath
    )
    $Env:PKG_NAME = Get-PackageName
    $Env:CMAKE_INSTALL_PREFIX = $packagePath
    $Env:INSTALL_LLVM_PACKAGE = $true
    . (Join-Path (Get-RepoRoot) "build" "llvm.ps1")
    Use-LlvmInstallation $packagePath
}

function Test-Prerequisites {
    function Test-SubmoduleInitialized {
        $repoSha = exec -wd (Get-RepoRoot) { Get-CommitHash }
        $submoduleSha = Get-LlvmSha
        $repoSha -ne $submoduleSha
    }

    if (!(Test-SubmoduleInitialized)) {
        Write-Vso "llvm-project submodule isn't initialized"
        Write-Vso "Initializing submodules: git submodule init"
        exec -wd (Get-RepoRoot) { git submodule init }
        Write-Vso "Updating submodules: git submodule update --depth 1 --recursive"
        exec -wd (Get-RepoRoot) { git submodule update --depth 1 --recursive }
    }
    Assert (Test-SubmoduleInitialized) "Failed to read initialized llvm-project submodule"
}

function Initialize-Environment {
    # if an external LLVM is specified, make sure it exist and
    # skip further bootstapping
    if (Test-Path env:\AQ_LLVM_EXTERNAL_DIR) {
        Use-ExternalLlvmInstallation
    }
    else {
        $env:AQ_LLVM_PACKAGE_GIT_VERSION = Get-LlvmSha
        Write-Vso "llvm-project sha: $env:AQ_LLVM_PACKAGE_GIT_VERSION"
        $packageName = Get-PackageName

        $packagePath = Get-InstallationDirectory $packageName
        if (Test-Path $packagePath) {
            Write-Vso "LLVM target $($env:AQ_LLVM_PACKAGE_GIT_VERSION) is already installed."
            # LLVM is already downloaded
            Use-LlvmInstallation $packagePath
        }
        else {
            Write-Vso "LLVM target $($env:AQ_LLVM_PACKAGE_GIT_VERSION) is not installed."
            if (Test-AllowedToDownloadLlvm) {
                Write-Vso "Downloading LLVM target $packageName "
                Install-LlvmFromBuildArtifacts $packagePath
            }
            else {
                Write-Vso "Downloading LLVM Disabled, building from source."
                # We don't have an external LLVM installation specified
                # We are not downloading LLVM
                # So we need to build it.
                Install-LlvmFromSource $packagePath
            }
        }
    }
}

function Test-InDevContainer {
    $IsLinux -and (Test-Path env:\IN_DEV_CONTAINER)
}

# Only run the nested ManyLinux container
# build on Linux while not in a dev container
function Test-RunInContainer {
    if($IsLinux) {
        # If we are in a dev container, our workspace is already
        # mounted into the container. If we try to mount our 'local' workspace
        # into a nested container it will silently fail to mount.
        !(Test-InDevContainer)
    } else {
        $false
    }
}

function Build-PyQIR {
    $buildPath = Resolve-Path (Join-Path $PSScriptRoot '..')
    if (Test-RunInContainer) {
        function Build-ContainerImage {
            Write-Vso "Building container image manylinux-llvm-builder"
            exec -wd (Join-Path $buildPath pyqir eng) {
                Get-Content manylinux.Dockerfile | docker build -t manylinux2014_x86_64_maturin -
            }
        }
        function Invoke-ContainerImage {
            Write-Vso "Running container image:"
            $ioVolume = "$($buildPath):/io"
            $llvmVolume = "$($env:AQ_LLVM_INSTALL_DIR):/usr/lib/llvm"

            Write-Vso "docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/pyqir manylinux2014_x86_64_maturin /usr/bin/maturin build --release" "command"
            exec {
                docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/pyqir manylinux2014_x86_64_maturin /usr/bin/maturin build --release
            }

            Write-Vso "docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/pyqir manylinux2014_x86_64_maturin python -m tox -e test" "command"
            exec {
                docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/pyqir manylinux2014_x86_64_maturin python -m tox -e test
            }

            Write-Vso "docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io manylinux2014_x86_64_maturin cargo test --package pyqir --package qirlib --lib -- --nocapture" "command"
            exec {
                docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io manylinux2014_x86_64_maturin cargo test --package pyqir --package qirlib --lib -- --nocapture
            }
        }

        Build-ContainerImage
        Invoke-ContainerImage
    }
    else {
        exec -wd (Join-Path $buildPath pyqir) {
            $python = "python3"
            if ($null -ne (Get-Command python -ErrorAction SilentlyContinue)) {
                $pythonIsPython3 = (python --version) -match "Python 3.*"
                if ($pythonIsPython3) {
                    $python = "python"
                }
            }

            Write-Vso "& $python -m pip install --user -U pip" "command"
            exec { & $python -m pip install --user -U pip }

            Write-Vso "& $python -m pip install --user maturin tox" "command"
            exec { & $python -m pip install --user maturin tox }

            Write-Vso "& $python -m tox -e test" "command"
            exec { & $python -m tox -e test }

            Write-Vso "& $python -m tox -e pack" "command"
            exec { & $python -m tox -e pack }
        }

        Write-Vso "& cargo test --package pyqir --package qirlib --lib -- --nocapture" "command"
        exec { & cargo test --package pyqir --package qirlib --lib -- --nocapture }
    }
}

Test-Prerequisites
Initialize-Environment
Build-PyQIR
