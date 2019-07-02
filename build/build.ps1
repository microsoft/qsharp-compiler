# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

##
# Q# compiler projects
##
function Build-One {
    param(
        [string]$action,
        [string]$project
    );

    dotnet $action (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    return ($LastExitCode -eq 0)
}

Write-Host "##[info]Building Q# compiler..."
$all_ok = (Build-One 'build' '../QsCompiler.sln') -and $all_ok

Write-Host "##[info]Publishing Q# compiler app..."
$all_ok = (Build-One 'publish' '../src/QsCompiler/CommandLineTool/QsCommandLineTool.csproj') -and $all_ok

Write-Host "##[info]Publishing Q# Language Server..."
$all_ok = (Build-One 'publish' '../src/QsCompiler/LanguageServer/QsLanguageServer.csproj') -and $all_ok


##
# VSCode extension
##
Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
    ..\..\build\setup.ps1
    npm install
    npm run compile
    $all_ok = ($LastExitCode -eq 0) -and $all_ok
Pop-Location


if (-not $all_ok) 
{
    throw "At least one project failed to compile. Check the logs."
}

