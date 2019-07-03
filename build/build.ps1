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

    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
}

Write-Host "##[info]Building Q# compiler..."
Build-One 'build' '../QsCompiler.sln'

Write-Host "##[info]Publishing Q# compiler command line compiler..."
Build-One 'publish' '../src/QsCompiler/CommandLineTool/QsCommandLineTool.csproj'

Write-Host "##[info]Publishing Q# language server..."
Build-One 'publish' '../src/QsCompiler/LanguageServer/QsLanguageServer.csproj'


##
# Extensions
##
Write-Host "##[info]Building VS Code extension..."
Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
    npm install
    npm run compile
    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
Pop-Location

Write-Host "##[info]Building VisualStudio extension..."
Build-One 'build' '../VisualStudioExtension.sln'


if (-not $all_ok) 
{
    throw "Building failed. Check the logs."
}

