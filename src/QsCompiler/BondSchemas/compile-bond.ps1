# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
    .SYNOPSIS
        Compiles Bond schemas (*.bond).

    .DESCRIPTION
        Compiles Bond schemas (*.bond) present in the parent directory of this script and all its subdirectories.

    .NOTES
        Assumes Bond already installed through nuget and available at the "\.nuget\packages\bond.csharp\" path relative to the user's base directory.
        E.g. "C:\Users\johndoe\.nuget\packages\bond.csharp\"
#>

# Global constants.
Set-Variable -Name BondVersion -Value "9.0.3" -Option Constant

function Get-BondResources
{
    param(
        [string]$version
    );

    $bondCompiler = Join-Path $env:UserProfile "\.nuget\packages\bond.csharp\${version}\tools\gbc.exe"
    if (!(Test-Path $bondCompiler -PathType Leaf))
    {
        Write-Host "Bond compiler could not be found at '${bondCompiler}'"
        $bondCompiler = $null
    }

    $bondImportDir = Join-Path $env:UserProfile "\.nuget\packages\bond.csharp\${version}\tools\inc"
    if (!(Test-Path $bondImportDir))
    {
        Write-Host "Bond import directory could not be found at '${bondImportDir}'"
        $bondImportDir = $null
    }

    return $bondCompiler, $bondImportDir
}

function Compile-BondToCSharp {
    param(
        [string]$Schema,
        [string]$Compiler,
        [string]$ImportDir
    );

    $schemaFullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Schema)
    $schemaName = [System.Io.Path]::GetFileNameWithoutExtension($schemaFullPath)
    $outputDirectory = (Get-Item $schemaFullPath).Directory.FullName
    $compileCommand =
        "$Compiler" +
        " c#" +
        " --namespace=bond=Bond" +
        " --import-dir=`"" + $ImportDir + "`"" +
        " --output-dir=`"" + $outputDirectory + "`"" +
        " $schemaFullPath"

    Write-Host "Bond compiler command: $compileCommand"
    iex $compileCommand

    $generatedCSharpFileName = "$schemaName" + "_types.cs"
    $generatedCSharpFilePath = Join-Path $outputDirectory $generatedCSharpFileName
    $newGeneratedCSharpFileName = "${schemaName}.cs"
    $newGeneratedCSharpFilePath = Join-Path $outputDirectory $newGeneratedCSharpFileName
    if (Test-Path $newGeneratedCSharpFilePath)
    {
        Write-Host "Removing existing file `"$newGeneratedCSharpFilePath`""
        Remove-Item $newGeneratedCSharpFilePath
    }

    Write-Host "Renaming generated C# file: `"$generatedCSharpFilePath`" -> `"$newGeneratedCSharpFilePath`""
    Rename-Item -Path $generatedCSharpFilePath -NewName $newGeneratedCSharpFileName -Force
}

Write-Host "Using Bond version: $BondVersion"

# Get the path to the Bond compiler (gbc.exe) and the Bond import directory.
$bondCompiler, $bondImportDir = Get-BondResources -Version $BondVersion
if (($bondCompiler -eq $null) -or ($bondImportDir -eq $null))
{
    Write-Error "Bond resources not found"
    exit 1
}

Write-Host "Using Bond compiler: `"$bondCompiler`""
Write-Host "Using Bond import directory: `"$bondImportDir`""
Write-Host "`n"

# Go through all *.bond files recursively and compile them.
Get-ChildItem -Path $PSScriptRoot -Filter *.bond -Recurse -File | ForEach-Object {
    $bondSchema = $_.FullName
    Write-Host "Compiling $bondSchema ..."
    Compile-BondToCSharp -Schema $bondSchema -Compiler $bondCompiler -ImportDir $bondImportDir
    Write-Host "`n"
}
