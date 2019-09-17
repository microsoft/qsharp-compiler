# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

##
# Q# compiler
##
function Pack-One() {
    param(
        [string]$project, 
        [string]$include_references=""
    );

    Write-Host "##[info]Packing $project..."
    nuget pack (Join-Path $PSScriptRoot $project) `
        -OutputDirectory $Env:NUGET_OUTDIR `
        -Properties Configuration=$Env:BUILD_CONFIGURATION `
        -Version $Env:NUGET_VERSION `
        -Verbosity detailed `
        $include_references

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to pack $project."
        $script:all_ok = $False
    }
}


Pack-One '../src/QsCompiler/Compiler/QsCompiler.csproj' '-IncludeReferencedProjects'
Pack-One '../src/QsCompiler/CommandLineTool/QsCommandLineTool.csproj' '-IncludeReferencedProjects'

##
# Q# Language Server (self-contained)
##

$Runtimes = @("win10-x64", "linux-x64", "osx-x64");

function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    $name = [System.IO.Path]::GetRandomFileName()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}

function Pack-SelfContained() {
    param(
        [string] $Project
    );

    Write-Host "##[info]Packing $Project as a self-contained deployment...";
    $Runtimes | ForEach-Object {
        $TargetDir = New-TemporaryDirectory;
        $BaseName = [System.IO.Path]::GetFileNameWithoutExtension($Project);
        $ArchiveDir = Join-Path $Env:BLOBS_OUTDIR $BaseName;
        New-Item -ItemType Directory -Path $ArchiveDir -Force -ErrorAction SilentlyContinue;

        try {
            $ArchivePath = Join-Path $ArchiveDir "$BaseName-$_-$Env:ASSEMBLY_VERSION.zip";
            dotnet publish  `
                $Project `
                --self-contained `
                --runtime $_ `
                --output $TargetDir;
            Write-Host "##[info]Writing self-contained deployment to $ArchivePath..."
            Compress-Archive `
                -Path (Join-Path $TargetDir *) `
                -DestinationPath $ArchivePath;
        } catch {
            $Script:all_ok = $false;
        } finally {
            Remove-Item -Recurse $TargetDir -ErrorAction Continue;
        }
    };
}

Pack-SelfContained "../src/QsCompiler/LanguageServer/QsLanguageServer.csproj"

##
# VS Code Extension
##
Write-Host "##[info]Packing VS Code extension..."
Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
if (Get-Command vsce -ErrorAction SilentlyContinue) {
    Try {
        vsce package

        if  ($LastExitCode -ne 0) {
            throw
        }        
    } Catch {
        Write-Host "##vso[task.logissue type=error;]Failed to pack VS Code extension."
        $all_ok = $False
    }
} else {    
    Write-Host "##vso[task.logissue type=warning;]vsce not installed. Will skip creation of VS Code extension package"
}
Pop-Location

##
# VisualStudioExtension
##
Write-Host "##[info]Packing VisualStudio extension..."
Push-Location (Join-Path $PSScriptRoot '..\src\VisualStudioExtension\QsharpVSIX')
if (Get-Command msbuild -ErrorAction SilentlyContinue) {
    Try {
        msbuild QsharpVSIX.csproj `
            /t:CreateVsixContainer `
            /property:Configuration=$Env:BUILD_CONFIGURATION `
            /property:AssemblyVersion=$Env:ASSEMBLY_VERSION

        if  ($LastExitCode -ne 0) {
            throw
        }
    } Catch {
        Write-Host "##vso[task.logissue type=error;]Failed to pack VS extension."
        $all_ok = $False
    }
} else {    
    Write-Host "##vso[task.logissue type=warning;]msbuild not installed. Will skip creation of VisualStudio extension package"
}
Pop-Location

if (-not $all_ok) 
{
    throw "Packing failed. Check the logs."
}

