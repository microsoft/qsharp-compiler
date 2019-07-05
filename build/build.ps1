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

    Write-Host "##[info]Building $project ($action)..."
    dotnet $action (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
}

Build-One 'build' '../QsCompiler.sln'
Build-One 'publish' '../src/QsCompiler/CommandLineTool/QsCommandLineTool.csproj'
Build-One 'publish' '../src/QsCompiler/LanguageServer/QsLanguageServer.csproj'


##
# VS Code Extension
##
Write-Host "##[info]Building VS Code extension..."
Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
Try {
    npm install
    npm run compile
    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
} Catch {
    Write-Host "##vso[task.logissue type=warning;]npm not installed. Will skip creation of VS Code extension"
}
Pop-Location

##
# VisualStudioExtension
##
Write-Host "##[info]Building VisualStudio extension..."
Push-Location (Join-Path $PSScriptRoot '..')
Try {
    nuget restore VisualStudioExtension.sln
    msbuild VisualStudioExtension.sln `
        /property:Configuration=$Env:BUILD_CONFIGURATION `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:AssemblyVersion=$Env:ASSEMBLY_VERSION
    $script:all_ok = ($LastExitCode -eq 0) -and $script:all_ok
} Catch {
    Write-Host "##vso[task.logissue type=warning;]nuget or msbuild not installed. Will skip building the VisualStudio extension"
}
Pop-Location


if (-not $all_ok) 
{
    throw "Building failed. Check the logs."
}

