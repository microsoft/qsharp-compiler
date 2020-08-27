# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

#!/usr/bin/env pwsh
#Requires -PSEdition Core

& "$PSScriptRoot/set-env.ps1"

if ($Env:ENABLE_VSIX -ne "false") {
    # The language server is only built if either the VS2019 or VS Code extension
    # is enabled.
    $VsixAssemblies = @(
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsCompilationManager.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsCore.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsDataStructures.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsLanguageServer.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsSyntaxProcessor.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsTextProcessor.dll",
        "./src/QsCompiler/LanguageServer/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsTransformations.dll"
    );

    # The VS2019 extension itself is only built if msbuild is present.
    if (Get-Command msbuild -ErrorAction SilentlyContinue) {
        $VsixAssemblies += @(
            "./src/VisualStudioExtension/QsharpVSIX/bin/$Env:BUILD_CONFIGURATION/Microsoft.Quantum.VisualStudio.Extension.dll"
        );
    }
} else {
    $VsixAssemblies = @();
}


@{
    Packages = @(
        "Microsoft.Quantum.Compiler",
        "Microsoft.Quantum.Compiler.CommandLine",
        "Microsoft.Quantum.ProjectTemplates",
        "Microsoft.Quantum.Sdk",
        "Microsoft.Quantum.DocumentationGenerator"
    );
    Assemblies = $VsixAssemblies + @(
        "./src/QsCompiler/DocumentationGenerator/bin/$Env:BUILD_CONFIGURATION/netstandard2.1/Microsoft.Quantum.DocumentationGenerator.dll",
        "./src/QsCompiler/CommandLineTool/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsCompiler.dll",
        "./src/QsCompiler/CommandLineTool/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/Microsoft.Quantum.QsDocumentationParser.dll",
        "./src/QsCompiler/CommandLineTool/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/qsc.dll",
        "./src/QuantumSdk/Tools/BuildConfiguration/bin/$Env:BUILD_CONFIGURATION/netcoreapp3.1/publish/Microsoft.Quantum.Sdk.BuildConfiguration.dll"        
    ) | ForEach-Object { Get-Item (Join-Path $PSScriptRoot ".." $_) };
} | Write-Output;
