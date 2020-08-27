# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

#!/usr/bin/env pwsh
#Requires -PSEdition Core

& "$PSScriptRoot/set-env.ps1"

@{
    Packages = @(
        "Microsoft.Quantum.Compiler",
        "Microsoft.Quantum.Compiler.CommandLine",
        "Microsoft.Quantum.ProjectTemplates",
        "Microsoft.Quantum.Sdk",
        "Microsoft.Quantum.DocumentationGenerator"
    );
    Assemblies = @(
        "./src/DocumentationGenerator/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.DocumentationGenerator.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsCompilationManager.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsCore.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsDataStructures.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsLanguageServer.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsSyntaxProcessor.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsTextProcessor.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsTransformations.dll"
    ) | ForEach-Object { Get-Item (Join-Path $PSScriptRoot ".." $_) };
} | Write-Output;
