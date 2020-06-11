# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True
Write-Host "Assembly version: $Env:ASSEMBLY_VERSION"

##
# Q# compiler and Sdk tools
##

function Build-One {
    param(
        [string]$project
    );

    Write-Host "##[info]Building $project ..."
    dotnet build (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    if  ($LastExitCode -ne 0) {
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
        Try {
            npm install
            npm run compile
    
            if  ($LastExitCode -ne 0) {
                throw
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to build VS Code extension."
            $all_ok = $False
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
                    msbuild VisualStudioExtension.sln `
                        /property:Configuration=$Env:BUILD_CONFIGURATION `
                        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
                        /property:AssemblyVersion=$Env:ASSEMBLY_VERSION
    
                    if ($LastExitCode -ne 0) {
                        throw
                    }
                } Catch {
                    Write-Host "##vso[task.logissue type=error;]Failed to build VS extension."
                    $all_ok = $False
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
Build-One '../src/QuantumSdk/Tools/Tools.sln'

if ($Env:ENABLE_VSIX -ne "false") {
    Build-VSCode
    Build-VS
} else {
    Write-Host "##vso[task.logissue type=warning;]VSIX building skipped due to ENABLE_VSIX variable."
}

if (-not $all_ok) {
    throw "Building failed. Check the logs."
    exit 1
} else {
    exit 0
} 

