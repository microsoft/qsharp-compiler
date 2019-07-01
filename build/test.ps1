# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

.\set-env.ps1

function Test-One {
    Param($project)

    dotnet test $project `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        --logger trx `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    if ($LastExitCode -ne 0) { throw "Cannot test $project." }
}

Write-Host "##[info]Testing C# code generation"
Test-One '../QsCompiler.sln'

