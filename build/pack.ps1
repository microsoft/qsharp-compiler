# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

##
# Q# compiler
##
function Pack-One() {
    param(
        [string]$project, 
        [string]$include_references=""
    );

    nuget pack $project `
        -OutputDirectory $Env:NUGET_OUTDIR `
        -Properties Configuration=$Env:BUILD_CONFIGURATION `
        -Version $Env:NUGET_VERSION `
        -Verbosity detailed `
        $include_references

    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
}

Write-Host "##[info]Using nuget to create packages"
Pack-One '../src/QsCompiler/Compiler/QsCompiler.csproj' '-IncludeReferencedProjects'

##
# VSCode extension
##
Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
    npm install vsce
    vsce package
    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
Pop-Location


if (-not $all_ok) 
{
    throw "At least one project failed to pack. Check the logs."
}

