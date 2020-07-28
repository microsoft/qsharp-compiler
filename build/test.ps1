# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True
Write-Host "Assembly version: $Env:ASSEMBLY_VERSION"

if ($Env:ENABLE_TESTS -eq "false") {
    Write-Host "##vso[task.logissue type=warning;]Tests skipped due to ENABLE_TESTS variable."
    return
}

function Test-One {
    Param(
        [string]$project
    );

    Write-Host "##[info]Testing $project..."

    if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
        $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
    }  else {
        $args = @();
    }
    dotnet test (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        --logger trx `
        @args `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:InformationalVersion=$Env:SEMVER_VERSION

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test $project."
        $script:all_ok = $False
    }
}

function Test-VSCode {
    Param([string] $path);

    Write-Host "##[info]Testing VSCode Extension inside $path..."    
    Push-Location (Join-Path $PSScriptRoot $path)
        npm run unittest
    Pop-Location

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to test VSCode inside $path"
        $script:all_ok = $False
    }
}

Test-One '../QsCompiler.sln'

if ($Env:ENABLE_VSIX -ne "false") {
    Test-VSCode '../src/VSCodeExtension'
} else {
    Write-Host "##vso[task.logissue type=warning;]VSIX building skipped due to ENABLE_VSIX variable."
}

if (-not $all_ok) 
{
    throw "Running tests failed. Check the logs."
}
