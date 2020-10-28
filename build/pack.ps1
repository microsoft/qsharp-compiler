# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True
Write-Host "Assembly version: $Env:ASSEMBLY_VERSION"

##
# Q# compiler
##
function Publish-One {
    param(
        [string]$project
    );

    Write-Host "##[info]Publishing $project ..."
    if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
        $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
    }  else {
        $args = @();
    }
    dotnet publish (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        --no-build `
        @args `
        /property:Version=$Env:ASSEMBLY_VERSION

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to publish $project."
        $script:all_ok = $False
    }
}

function Pack-One() {
    param(
        [string]$project, 
        [string]$include_references=""
    );

    Write-Host "##[info]Packing '$project'..."
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

function Pack-Dotnet() {
    Param($project, $option1 = "", $option2 = "", $option3 = "")
    if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
        $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
    }  else {
        $args = @();
    }
    dotnet pack (Join-Path $PSScriptRoot $project) `
        -o $Env:NUGET_OUTDIR `
        -c $Env:BUILD_CONFIGURATION `
        -v detailed `
        --no-build `
        @args `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:PackageVersion=$Env:NUGET_VERSION `
        $option1 `
        $option2 `
        $option3

    if ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to pack $project."
        $script:all_ok = $False
    }
}


################################
# Start main execution:

$all_ok = $True

Publish-One '../src/QsCompiler/CommandLineTool/CommandLineTool.csproj'
Publish-One '../src/QuantumSdk/Tools/BuildConfiguration/BuildConfiguration.csproj'

Pack-One '../src/QsCompiler/Compiler/Compiler.csproj' '-IncludeReferencedProjects'
Pack-One '../src/QsCompiler/CommandLineTool/CommandLineTool.csproj' '-IncludeReferencedProjects'
Pack-Dotnet '../src/Documentation/DocumentationGenerator/DocumentationGenerator.csproj'
Pack-One '../src/ProjectTemplates/Microsoft.Quantum.ProjectTemplates.nuspec'
Pack-One '../src/QuantumSdk/QuantumSdk.nuspec'

if ($Env:ENABLE_VSIX -ne "false") {
    & "$PSScriptRoot/pack-extensions.ps1"
} else {
    Write-Host "##vso[task.logissue type=warning;]VSIX packing skipped due to ENABLE_VSIX variable."
}

# Copy documentation summarization tool into docs drop.
# Note that we only copy this tool when DOCS_OUTDIR is set (that is, when we're
# collecting docs in a build artifact).
if ("$Env:DOCS_OUTDIR".Trim() -ne "") {
    Push-Location (Join-Path $PSScriptRoot "../src/Documentation/Summarizer")
        Copy-Item -Path *.py, *.txt -Destination $Env:DOCS_OUTDIR
    Pop-Location
}

if (-not $all_ok) {
    throw "Packing failed. Check the logs."
    exit 1
} else {
    exit 0
}
