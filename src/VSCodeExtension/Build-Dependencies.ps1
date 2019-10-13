# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

param(
    [string]
    $Configuration = $null,

    [switch]
    $Force
)

$TargetMoniker = "netcoreapp3.0";
$LanguageServerRoot = Resolve-Path "../QsCompiler/LanguageServer/";
$RepoRoot = Resolve-Path "../../"

# If we're not given a configuration, try to populate from an environment variable.
if ($Configuration -eq $null -or $Configuration.Trim().Length -eq 0) {
    if ($Env:BuildConfiguration -eq $null) {
        # Default to Debug, so that we behave in the same
        # manner as the `dotnet publish` tool.
        $Configuration = "Debug";
    } else {
        $Configuration = $Env:BuildConfiguration;
    }
}
Write-Host "Using configuration $Configuration."

$PublishRoot = Join-Path $LanguageServerRoot "bin/$Configuration/$TargetMoniker/publish";
$LanguageServerAssembly = Join-Path $PublishRoot "Microsoft.Quantum.QsLanguageServer.dll";


$binDir = (Join-Path $PSScriptRoot "bin");

if ((Test-Path -Type Leaf -Path $LanguageServerAssembly) -and -not $Force) {
    Write-Host "QsLanguageServer.dll already found at $LanguageServerAssembly. Skipping its compilation."
    $publishExitCode = 0
} else {
    # Run publish to place language server and dependency DLLs into the publish
    # directory.
    Write-Host "Starting dotnet publish on QsLanguageServer..."
    Push-Location -LiteralPath $LanguageServerRoot;
        $dotnetInvocation = "dotnet publish -c $Configuration -f $TargetMoniker";
        Write-Host "Calling ``$dotnetInvocation``...";
        Invoke-Expression $dotnetInvocation;
        $publishExitCode = $LASTEXITCODE;
    Pop-Location
}

# Check that the language server DLL was built successfully.
if (
    $publishExitCode -ne 0 -or -not (Test-Path -Type Leaf -Path $LanguageServerAssembly)
) {
    Write-Error "dotnet publish did not produce a language server DLL in $PublishRoot.";
    exit -1;
}

# Make sure that the destination directory is empty so that we always
# place the latest version into the extension directory.
if (Get-Item -Path $binDir -ErrorAction SilentlyContinue) {
    Remove-Item -Recurse -Force $binDir
}
New-Item -Type Directory -Force -Path $binDir | Out-Null
Copy-Item -Recurse -Force -Path (Join-Path $PublishRoot "*") -Destination $binDir -Verbose

# Copy the third party notice
Write-Host "$(Join-Path $RepoRoot "NOTICE.txt")"
Copy-Item -Force -Path (Join-Path $RepoRoot "NOTICE.txt") -Destination $PSScriptRoot -Verbose
