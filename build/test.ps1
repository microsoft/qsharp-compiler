# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

function Test-One {
    Param(
        [string]$project
    );

    dotnet test $project `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        --logger trx `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    return ($LastExitCode -ne 0)
}

Write-Host "##[info]Testing Q# compiler..."
$all_ok = (Test-One '../QsCompiler.sln') -and $all_ok


if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}

