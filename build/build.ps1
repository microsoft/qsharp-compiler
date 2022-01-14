# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True
Write-Host "Assembly version: $Env:ASSEMBLY_VERSION"

if (($IsWindows) -or ((Test-Path Env:/AGENT_OS) -and ($Env:AGENT_OS.StartsWith("Win")))) {
    if (!(Get-Command clang        -ErrorAction SilentlyContinue) -or `
        (Test-Path Env:/AGENT_OS)) {
        choco install llvm --version=13.0.0 --allow-downgrade
        Write-Host "##vso[task.setvariable variable=PATH;]$($env:SystemDrive)\Program Files\LLVM\bin;$Env:PATH"
    }
    if (!(Get-Command ninja -ErrorAction SilentlyContinue)) {
        choco install ninja
    }
    if (!(Get-Command cmake -ErrorAction SilentlyContinue)) {
        choco install cmake
    }
    refreshenv
} elseif ($IsMacOS) {
    # temporary workaround for Bintray sunset
    # remove this after Homebrew is updated to 3.1.1 on MacOS image, see:
    # https://github.com/actions/virtual-environments/blob/main/images/macos/macos-10.15-Readme.md
    brew update
    brew install ninja
    if (!(Get-Command clang-format -ErrorAction SilentlyContinue)) {
        brew install clang-format
    }
} else {
    if (Get-Command sudo -ErrorAction SilentlyContinue) {
        sudo apt update
        sudo apt-get install -y ninja-build clang-13 clang-tidy-13 clang-format-13
    } else {
        apt update
        apt-get install -y ninja-build clang-13 clang-tidy-13 clang-format-13
    }
}

##
# Q# compiler and Sdk tools
##

function Build-One {
    param(
        [string]$project
    );

    Write-Host "##[info]Building $project ..."
    if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
        $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
    }  else {
        $args = @();
    }
    dotnet build (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        @args `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:InformationalVersion=$Env:SEMVER_VERSION `
        /property:TreatWarningsAsErrors=true

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to build $project."
        $script:all_ok = $False
    }
}

##
# VS Code Extension
##
function Build-VSCode() {
    Write-Host "##[info]Building VS Code extension..."

    Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
    if (Get-Command npm -ErrorAction SilentlyContinue) {
        $npmVersion = (npm --version);
        @{
            "npm" = $npmVersion;
            "node" = (node --version);
        } | Format-Table | Write-Output;

        # Check if npm is up to date enough.
        if ([semver]::new($npmVersion) -lt [semver]::new(7, 0, 0)) {
            $IsCI = "$Env:TF_BUILD" -ne "" -or "$Env:CI" -eq "true";
            $msg = "npm was version $npmVersion, but building the VS Code extension requires 7.0 or later.";
            if ($IsCI) {
                Write-Host "##[error]$msg";
            } else {
                Write-Error $msg;
            }
            throw;
        }

        Try {
            npm ci
            npm run compile

            if  ($LastExitCode -ne 0) {
                throw
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to build VS Code extension."
            $script:all_ok = $False
        }
    } else {
        Write-Host "##vso[task.logissue type=warning;]npm not installed. Will skip creation of VS Code extension"
    }
    Pop-Location
}


##
# VisualStudioExtension
##
function Build-VS() {
    Write-Host "##[info]Building VisualStudio extension..."
    Push-Location (Join-Path $PSScriptRoot '..')
    if (Get-Command nuget -ErrorAction SilentlyContinue) {
        Try {
            nuget restore VisualStudioExtension.sln

            if ($LastExitCode -ne 0) {
                throw
            }

            if (Get-Command msbuild -ErrorAction SilentlyContinue) {
                Try {
                    if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
                        $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
                    } else {
                        $args = @();
                    }
                    msbuild VisualStudioExtension.sln `
                        /property:Configuration=$Env:BUILD_CONFIGURATION `
                        @args `
                        /property:AssemblyVersion=$Env:ASSEMBLY_VERSION `
                        /property:InformationalVersion=$Env:SEMVER_VERSION `
                        /property:TreatWarningsAsErrors=true

                    if ($LastExitCode -ne 0) {
                        throw
                    }
                } Catch {
                    Write-Host "##vso[task.logissue type=error;]Failed to build VS extension."
                    $script:all_ok = $False
                }
            } else {
                Write-Host "msbuild not installed. Will skip building the VisualStudio extension"
            }
        } Catch {
            Write-Host "##vso[task.logissue type=warning;]Failed to restore VS extension solution."
        }
    } else {
         Write-Host "##vso[task.logissue type=warning;]nuget not installed. Will skip restoring and building the VisualStudio extension solution"
    }
    Pop-Location
}

################################
# Start main execution:

$all_ok = $True

Build-One '../QsCompiler.sln'
if ($IsWindows) {
    Build-One '../examples/QIR/QIR.sln'
}
Build-One '../src/QuantumSdk/Tools/Tools.sln'
Build-One '../src/Telemetry/Telemetry.sln'
Build-One '../QsFmt.sln'

if ($Env:ENABLE_VSIX -ne "false") {
    Build-VSCode
    Build-VS
} else {
    Write-Host "##vso[task.logissue type=warning;]VSIX building skipped due to ENABLE_VSIX variable."
}

# NB: In other repos, we check the manifest here. That can cause problems
#     in our case, however, as some assemblies are only produced during
#     packing and publishing. Thus, as an exception, we verify the manifest
#     only in pack.ps1.

if (-not $all_ok) {
    throw "Building failed. Check the logs."
    exit 1
} else {
    exit 0
}
