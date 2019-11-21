# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

Write-Host "##[info]PowerShell version: $($PSVersionTable.PSVersion)"

##
# Q# compiler
##
function Pack-One() {
    param(
        [string]$project, 
        [string]$include_references=""
    );

    Write-Host "##[info]Packing $project..."
    nuget pack (Join-Path $PSScriptRoot $project) `
        -OutputDirectory $Env:NUGET_OUTDIR `
        -Properties Configuration=$Env:BUILD_CONFIGURATION `
        -Version $Env:NUGET_VERSION `
        -Verbosity detailed `
        $include_references

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to pack $project."
        $script:all_ok = $False
    }
}

Pack-One '../src/QsCompiler/Compiler/Compiler.csproj' '-IncludeReferencedProjects'
Pack-One '../src/QsCompiler/CommandLineTool/CommandLineTool.csproj' '-IncludeReferencedProjects'
Pack-One '../src/ProjectTemplates/Microsoft.Quantum.ProjectTemplates.nuspec'
Pack-One '../src/QuantumSdk/QuantumSdk.nuspec'

##
# VS Code Extension
##
Write-Host "##[info]Packing VS Code extension..."
Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
if (Get-Command vsce -ErrorAction SilentlyContinue) {
    Try {
        vsce package

        if ($LastExitCode -ne 0) {
            throw;
        }
    } Catch {
        Write-Host "##vso[task.logissue type=error;]Failed to pack VS Code extension."
        $all_ok = $False
    }
} else {
    Write-Host "##vso[task.logissue type=warning;]vsce not installed. Will skip creation of VS Code extension package"
}
Pop-Location

##
# VisualStudioExtension
##
Write-Host "##[info]Packing VisualStudio extension..."
Push-Location (Join-Path $PSScriptRoot '..\src\VisualStudioExtension\QsharpVSIX')
if (Get-Command msbuild -ErrorAction SilentlyContinue) {
    Try {
        msbuild QsharpVSIX.csproj `
            /t:CreateVsixContainer `
            /property:Configuration=$Env:BUILD_CONFIGURATION `
            /property:AssemblyVersion=$Env:ASSEMBLY_VERSION

        if  ($LastExitCode -ne 0) {
            throw
        }
    } Catch {
        Write-Host "##vso[task.logissue type=error;]Failed to pack VS extension."
        $all_ok = $False
    }
} else {    
    Write-Host "##vso[task.logissue type=warning;]msbuild not installed. Will skip creation of VisualStudio extension package"
}
Pop-Location

if (-not $all_ok) 
{
    throw "Packing failed. Check the logs."
    exit 1
} else {
    exit 0
}

