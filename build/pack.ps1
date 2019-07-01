# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

.\set-env.ps1

##
# Q# compiler
##
function Pack-One() {
    Param($project, $include_references="")
    nuget pack $project `
        -OutputDirectory $Env:NUGET_OUTDIR `
        -Properties Configuration=$Env:BUILD_CONFIGURATION `
        -Version $Env:NUGET_VERSION `
        -Verbosity detailed `
        $include_references

    if ($LastExitCode -ne 0) { throw "Cannot pack $project." }
}

Write-Host "##[info]Using nuget to create packages"
Pack-One '../src/QsCompiler/Compiler/QsCompiler.csproj' '-IncludeReferencedProjects'

##
# VSCode extension
##
pushd ../src/VSCodeExtension
npm install vsce
vsce package
popd