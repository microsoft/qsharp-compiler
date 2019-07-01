# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

.\set-env.ps1

##
# Q# compiler projects
##
function Build-One {
    Param($action, $project)

    dotnet $action $project `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    if ($LastExitCode -ne 0) { throw "Cannot $action $project." }
}

Write-Host "##[info]Build Q# compiler"
Build-One 'build' '../QsCompiler.sln'

Write-Host "##[info]Publish Q# compiler app"
Build-One 'publish' '../src/QsCompiler/CommandLineTool/QsCommandLineTool.csproj'

Write-Host "##[info]Publish Q# Language Server"
Build-One 'publish' '../src/QsCompiler/LanguageServer/QsLanguageServer.csproj'


##
# VSCode extension
##
pushd ../src/VSCodeExtension
npm install
npm run compile
popd

