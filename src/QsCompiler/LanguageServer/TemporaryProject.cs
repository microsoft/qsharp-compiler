// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsLanguageServer
{
    internal static class TemporaryProject
    {
        internal static string GetFileContents(string compilationScope, string? sdkVersion = null) => $@"
            <Project Sdk=""Microsoft.Quantum.Sdk/{sdkVersion ?? "0.9999.2108.1725-alpha"}"">
                <PropertyGroup>
                    <TargetFramework>netstandard2.1</TargetFramework>
                </PropertyGroup>
                <ItemGroup>
                    <QSharpCompile Include=""{compilationScope}"" />
                </ItemGroup>
            </Project>";
    }
}
