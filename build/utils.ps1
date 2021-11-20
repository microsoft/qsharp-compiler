# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (Test-Path function:\exec) {
    # already included
    return
}

if (!(Test-Path function:\Get-RepoRoot)) {
    # git revparse uses cwd. E2E builds use a different
    # working dir, so we pin it to out repo (submodule in E2E)
    function Get-RepoRoot {
        exec -wd $PSScriptRoot {
            git rev-parse --show-toplevel
        }
    }
}

# Fix temp path for non-windows platforms if missing
if (!(Test-Path env:\TEMP)) {
    $env:TEMP = [System.IO.Path]::GetTempPath()
}

# Test whether we build the compiler components.
function Test-BuildCompiler {
    # By default we want to build the compiler
    # If no env var is defined we default to true
    if (!(Test-Path env:\ENABLE_COMPILER)) {
        $true
    }
    else {
        # The env var exists, we use its value
        # Coerce falsy values and negate to keep the defalt $true
        $env:ENABLE_COMPILER -ne $false
    }
}

# Test whether we build the VSIX components.
function Test-BuildVsix {
    ($Env:ENABLE_VSIX -ne "false") -and $IsWindows
}

# Test whether we build the LLVM components.
function Test-BuildLlvmComponents {
    ((Test-Path env:\ENABLE_LLVM_BUILDS) -and ($env:ENABLE_LLVM_BUILDS -eq $true))
}

####
# Utilities
####

# executes the sciptblock. If the working directory is specitied, the block is
# exeucted in that directory. If the $LASTEXITCODE from the block is not 0,
# an exception is thrown.
function exec {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$cmd,
        [string]$errorMessage = "Failed to execute command",
        [Alias("wd")]
        [string]$workingDirectory = $null
    )
    try {

        if ($workingDirectory) {
            Push-Location -Path $workingDirectory
        }

        $global:lastexitcode = 0
        & $cmd
        if ($global:lastexitcode -ne 0) {
            throw "exec: $errorMessage"
        }
    }
    finally {
        if ($workingDirectory) {
            Pop-Location
        }
    }
}

# tests whether a condition is true. Throws an exception with the specified error message
# if the condition check fails.
function Assert {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $conditionToCheck,

        [Parameter(Mandatory = $true)]
        [string]$failureMessage
    )

    if (-not $conditionToCheck) {
        throw ('Assert: {0}' -f $failureMessage)
    }
}

# returns true if the script is running on a build agent, false otherwise
function Test-CI {
    if (Test-Path env:\TF_BUILD) {
        $true
    }
    elseif ((Test-Path env:\CI)) {
        $env:CI -eq $true
    }
    else {
        $false
    }
}

# Writes an Azure DevOps message with default debug severity
function Write-Vso {
    param (
        [Parameter(Mandatory = $true)]
        [string]$message,
        [Parameter(Mandatory = $false)]
        [ValidateSet("group", "warning", "error", "section", "debug", "command", "endgroup")]
        [string]$severity = "debug"
    )
    Write-Host "##[$severity]$message"
}

# Returns true if a command with the specified name exists.
function Test-CommandExists($name) {
    $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
}

# Returns true if the current environment is a dev container.
function Test-InDevContainer {
    $IsLinux -and (Test-Path env:\IN_DEV_CONTAINER)
}

# Updates the cargo package version with the version specified.
function Restore-CargoTomlWithVersionInfo ($inputFile, $outputFile, $version) {
    $outFile = New-Item -ItemType File -Path $outputFile
    $inPackageSection = $false
    switch -regex -file $inputFile {
        "^\[(.+)\]" {
            # Section
            $section = $matches[1]
            $inPackageSection = $section -eq "package"
            Add-Content -Path $outFile -Value $_
        }
        "(.+?)\s*=(.*)" {
            # Key/Value
            $key, $value = $matches[1..2]
            if ($inPackageSection -and ($key -eq "version")) {
                $value = "version = ""$($version)"""
                Add-Content -Path $outFile -Value $value
            }
            else {
                Add-Content -Path $outFile -Value $_
            }
        }
        default {
            Add-Content -Path $outFile -Value $_
        }
    }
}

# Copies the default config.toml and sets the [env] config
# section to specify the variables needed for llvm-sys/inkwell
# This allows us to not need the user to specify env vars to build.
function Restore-ConfigTomlWithLlvmInfo {
    $cargoPath = Resolve-Path (Join-Path (Get-RepoRoot) '.cargo')
    $configTemplatePath = Join-Path $cargoPath config.toml.template
    $configPath = Join-Path $cargoPath config.toml

    # remove the old file if it exists.
    if (Test-Path $configPath) {
        Remove-Item $configPath
    }

    # ensure the output folder is there, `mkdir -p` equivalent
    New-Item -ItemType Directory -Path $cargoPath -Force | Out-Null

    # copy the template
    Copy-Item $configTemplatePath $configPath

    # append the env vars to the new config
    $installationDirectory = Resolve-InstallationDirectory
    Add-Content -Path $configPath -Value "[env]"
    Add-Content -Path $configPath -Value "LLVM_SYS_110_PREFIX = '$installationDirectory'"
}

function Get-LlvmSubmoduleSha {
    $status = Get-LlvmSubmoduleStatus
    $sha = $status.Substring(1, 9)
    $sha
}

function Get-LlvmSubmoduleStatus {
    Write-Vso "Detected submodules: $(git submodule status --cached)"
    $statusResult = exec -wd (Get-RepoRoot) { git submodule status --cached }
    # on all platforms, the status uses '/' in the module path.
    $status = $statusResult.Split([Environment]::NewLine) | ? { $_.Contains("external/llvm-project") } | Select-Object -First 1
    $status
}

function Test-LlvmSubmoduleInitialized {
    $status = Get-LlvmSubmoduleStatus
    if ($status.Substring(0, 1) -eq "-") {
        Write-Vso "LLVM Submodule Uninitialized"
        return $false
    }
    else {
        Write-Vso "LLVM Submodule Initialized"
        return $true
    }
}

# Gets the LLVM package triple for the current platform
function Get-TargetTriple {
    $triple = "unknown"
    if ($IsWindows) {
        $triple = "x86_64-pc-windows-msvc-static"
    }
    elseif ($IsLinux) {
        $triple = "x86_64-unknown-linux-gnu"
    }
    elseif ($IsMacOS) {
        $triple = "x86_64-apple-darwin"
    }
    $triple
}

# This method should be able to be removed when Rust 1.56 is released
# which contains the feature for env sections in the .cargo/config.toml
function Use-LlvmInstallation {
    param (
        [string]$path
    )
    Write-Vso "LLVM installation set to: $path"
    $env:LLVM_SYS_110_PREFIX = $path
    $binPath = Join-Path $path "bin"
    if ($IsWindows) {
        $env:PATH = "$($binPath);$($env:PATH)"
    }
    else {
        $env:PATH = "$($binPath):$($env:PATH)"
    }
}

# Gets the LLVM version git hash
# on the CI this will come as an env var
function Get-LlvmSha {
    # Sometimes the CI fails to initilize AQ_LLVM_PACKAGE_GIT_VERSION correctly
    # so we need to make sure it isn't empty.
    if ((Test-Path env:\AQ_LLVM_PACKAGE_GIT_VERSION) -and ![string]::IsNullOrWhiteSpace($Env:AQ_LLVM_PACKAGE_GIT_VERSION)) {
        Write-Vso "Use environment submodule version: $($env:AQ_LLVM_PACKAGE_GIT_VERSION)"
        $env:AQ_LLVM_PACKAGE_GIT_VERSION
    }
    else {
        $sha = exec { Get-LlvmSubmoduleSha }
        Write-Vso "Use cached submodule version: $sha"
        $sha
    }
}

function Get-PackageName {
    $sha = Get-LlvmSha
    $TARGET_TRIPLE = Get-TargetTriple
    $packageName = "aq-llvm-$($TARGET_TRIPLE)-$($sha)"
    $packageName
}

function Get-DefaultInstallDirectory {
    if (Test-Path env:\AQ_CACHE_DIR) {
        $env:AQ_CACHE_DIR
    }
    else {
        Join-Path "$HOME" ".azure-quantum"
    }
}

function Get-AqCacheDirectory {
    $aqCacheDirectory = (Get-DefaultInstallDirectory)
    if (!(Test-Path $aqCacheDirectory)) {
        mkdir $aqCacheDirectory | Out-Null
    }
    Resolve-Path $aqCacheDirectory
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

function Resolve-InstallationDirectory {
    if (Test-Path env:\AQ_LLVM_EXTERNAL_DIR) {
        return $env:AQ_LLVM_EXTERNAL_DIR
    }
    else {
        $packageName = Get-PackageName

        $packagePath = Get-InstallationDirectory $packageName
        return $packagePath
    }
}
