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

Write-Host "##[info]Packing Q# compiler..."
Pack-One '../src/QsCompiler/Compiler/QsCompiler.csproj' '-IncludeReferencedProjects'

if (-not $all_ok) 
{
    throw "Packaging failed. Check the logs."
}

