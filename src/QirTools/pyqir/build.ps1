# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (!(Test-Path function:\Get-RepoRoot)) {
    # git revparse uses cwd. E2E builds use a different
    # working dir, so we pin it to out repo (submodule in E2E)
    $should_push = (Get-Location) -ne $PSScriptRoot
    try {
        if ($should_push) { Push-Location -Path $PSScriptRoot }
        . (Join-Path (git rev-parse --show-toplevel) build "utils.ps1")
    }
    finally {
        if ($should_push) {
            Pop-Location
        }
    }
}

function Use-ExternalLlvmInstallation {
    Write-Vso "Using LLVM installation specified by AQ_LLVM_EXTERNAL_DIR"
    Assert (Test-Path $env:AQ_LLVM_EXTERNAL_DIR) "AQ_LLVM_EXTERNAL_DIR folder does not exist"
    Use-LlvmInstallation $env:AQ_LLVM_EXTERNAL_DIR
}

function Test-AllowedToDownloadLlvm {
    # If AQ_DOWNLOAD_LLVM isn't set, we allow for download
    # If it is set, then we use its value
    !((Test-Path env:\AQ_DOWNLOAD_LLVM) -and ($env:AQ_DOWNLOAD_LLVM -eq $false))
}

function Get-LlvmDownloadBaseUrl {
    if (Test-Path env:\AQ_LLVM_BUILDS_URL) {
        $env:AQ_LLVM_BUILDS_URL
    }
    else
    { "https://msquantumpublic.blob.core.windows.net/llvm-builds" }
}

function Get-PackageExt {
    $extension = ".tar.gz"
    if ($IsWindows) {
        $extension = ".zip"
    }
    $extension
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

function Install-LlvmFromBuildArtifacts {
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $packagePath
    )

    $outFile = Join-Path $($env:TEMP) (Get-LlvmArchiveFileName)
    if ((Test-Path $outFile)) {
        Remove-Item $outFile
    }

    $archiveUrl = Get-LlvmArchiveUrl
    Write-Vso "Dowloading $archiveUrl to $outFile"
    Invoke-WebRequest -Uri $archiveUrl -OutFile $outFile

    $shaFile = Join-Path $($env:TEMP) (Get-LlvmArchiveShaFileName)
    if ((Test-Path $shaFile)) {
        Remove-Item $shaFile
    }

    $sha256Url = Get-LlvmArchiveShaUrl
    Write-Vso "Dowloading $sha256Url to $shaFile"
    Invoke-WebRequest -Uri $sha256Url -OutFile $shaFile
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
    if (!(Test-LlvmSubmoduleInitialized)) {
        Write-Vso "llvm-project submodule isn't initialized"
        Write-Vso "Initializing submodules: git submodule init"
        exec -wd (Get-RepoRoot) { git submodule init }
        Write-Vso "Updating submodules: git submodule update --depth 1 --recursive"
        exec -wd (Get-RepoRoot) { git submodule update --recommend-shallow --single-branch --recursive }
    }
    Assert (Test-LlvmSubmoduleInitialized) "Failed to read initialized llvm-project submodule"
}

function Initialize-Environment {
    # if an external LLVM is specified, make sure it exist and
    # skip further bootstapping
    if (Test-Path env:\AQ_LLVM_EXTERNAL_DIR) {
        Use-ExternalLlvmInstallation
    }
    else {
        $AQ_LLVM_PACKAGE_GIT_VERSION = Get-LlvmSha
        Write-Vso "llvm-project sha: $AQ_LLVM_PACKAGE_GIT_VERSION"
        $packageName = Get-PackageName

        $packagePath = Get-InstallationDirectory $packageName
        if (Test-Path $packagePath) {
            Write-Vso "LLVM target $($AQ_LLVM_PACKAGE_GIT_VERSION) is already installed."
            # LLVM is already downloaded
            Use-LlvmInstallation $packagePath
        }
        else {
            Write-Vso "LLVM target $($AQ_LLVM_PACKAGE_GIT_VERSION) is not installed."
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


# Only run the nested ManyLinux container
# build on Linux while not in a dev container
function Test-RunInContainer {
    if ($IsLinux) {
        # If we are in a dev container, our workspace is already
        # mounted into the container. If we try to mount our 'local' workspace
        # into a nested container it will silently fail to mount.
        !(Test-InDevContainer)
    }
    else {
        $false
    }
}

function Build-PyQIR {
    $buildPath = Resolve-Path (Join-Path $PSScriptRoot '..')
    $srcPath = (Get-RepoRoot)
    $installationDirectory = Resolve-InstallationDirectory

    if (Test-RunInContainer) {
        function Build-ContainerImage {
            Write-Vso "Building container image manylinux-llvm-builder"
            exec -wd (Join-Path $buildPath pyqir eng) {
                Get-Content manylinux.Dockerfile | docker build -t manylinux2014_x86_64_maturin -
            }
        }
        function Invoke-ContainerImage {
            Write-Vso "Running container image:"
            $ioVolume = "$($srcPath):/io"
            $llvmVolume = "$($installationDirectory):/usr/lib/llvm"

            Write-Vso "docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/src/QirTools/pyqir manylinux2014_x86_64_maturin /usr/bin/maturin build --release" "command"
            exec {
                docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/src/QirTools/pyqir manylinux2014_x86_64_maturin /usr/bin/maturin build --release
            }

            Write-Vso "docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/src/QirTools/pyqir manylinux2014_x86_64_maturin python -m tox -e test" "command"
            exec {
                docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io/src/QirTools/pyqir manylinux2014_x86_64_maturin python -m tox -e test
            }

            Write-Vso "docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io manylinux2014_x86_64_maturin cargo test --package qirlib --lib -vv -- --nocapture" "command"
            exec {
                docker run --rm -v $ioVolume -v $llvmVolume -e LLVM_SYS_110_PREFIX=/usr/lib/llvm -w /io manylinux2014_x86_64_maturin cargo test --package qirlib --lib -vv -- --nocapture
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

        Write-Vso "& cargo test --package qirlib --lib -vv -- --nocapture" "command"
        exec -wd $srcPath { & cargo test --package qirlib --lib -vv -- --nocapture }
    }
}

Test-Prerequisites
Initialize-Environment
Build-PyQIR
