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

##
# Q# Language Server (self-contained)
##

$Runtimes = @{
    "win10-x64" = "win32";
    "linux-x64" = "linux";
    "osx-x64" = "darwin";
};

function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    $name = [System.IO.Path]::GetRandomFileName()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}

function Write-Hash() {
    param(
        [string]
        $Path,

        [string]
        $BlobPlatform,

        [string]
        $TargetPath
    );

    "Writing hash of $Path into $TargetPath..." | Write-Host;
    $packageData = Get-Content $TargetPath | ConvertFrom-Json;
    $packageData.blobs.$BlobPlatform.sha256 = Get-FileHash $Path | Select-Object -ExpandProperty Hash;
    # See https://stackoverflow.com/a/23738236 for why this works.
    $packageData.blobs.$BlobPlatform `
        | Add-Member `
            -Force `
            -MemberType NoteProperty `
            -Name "size" `
            -Value (Get-Item $Path | Select-Object -ExpandProperty Length);
    Write-Host "New blob data: $($packageData."blobs".$BlobPlatform | ConvertTo-Json)";
    $packageData `
        | ConvertTo-Json -Depth 32 `
        | Set-Content `
            -Path $TargetPath `
            -Encoding UTF8NoBom;
}

function Pack-SelfContained() {
    param(
        [string] $Project,

        [string] $PackageData = $null
    );

    Write-Host "##[info]Packing $Project as a self-contained deployment...";
    $Runtimes.GetEnumerator() | ForEach-Object {
        $DotNetRuntimeID = $_.Key;
        $NodePlatformID = $_.Value;
        $TargetDir = New-TemporaryDirectory;
        $BaseName = [System.IO.Path]::GetFileNameWithoutExtension((Join-Path $PSScriptRoot $Project));
        $ArchiveDir = Join-Path $Env:BLOBS_OUTDIR $BaseName;
        New-Item -ItemType Directory -Path $ArchiveDir -Force -ErrorAction SilentlyContinue;

        try {
            if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
                $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
            }  else {
                $args = @();
            }
            $ArchivePath = Join-Path $ArchiveDir "$BaseName-$DotNetRuntimeID-$Env:SEMVER_VERSION.zip";
            dotnet publish (Join-Path $PSScriptRoot $Project) `
                -c $Env:BUILD_CONFIGURATION `
                -v $Env:BUILD_VERBOSITY `
                --self-contained `
                --runtime $DotNetRuntimeID `
                --output $TargetDir `
                @args `
                /property:Version=$Env:ASSEMBLY_VERSION `
                /property:InformationalVersion=$Env:SEMVER_VERSION
            Write-Host "##[info]Writing self-contained deployment to $ArchivePath..."
            Compress-Archive `
                -Force `
                -Path (Join-Path $TargetDir *) `
                -DestinationPath $ArchivePath `
                -ErrorAction Continue;
            if ($null -ne $PackageData) {
                Write-Hash `
                    -Path $ArchivePath `
                    -BlobPlatform $NodePlatformID `
                    -TargetPath (Join-Path $PSScriptRoot $PackageData)
            }
        } catch {
            Write-Host "##vso[task.logissue type=error;]Failed to pack self-contained deployment: $_";
            $Script:all_ok = $false;
        } finally {
            Remove-Item -Recurse $TargetDir -ErrorAction Continue;
        }
    };
}

##
# VS Code Extension
##
function Pack-VSCode() {
    Write-Host "##[info]Packing VS Code extension..."
    Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
    if (Get-Command vsce -ErrorAction SilentlyContinue) {
        Try {
            vsce package
    
            if ($LastExitCode -ne 0) {
                throw;
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to pack VS Code extension."
            $all_ok = $False
        }
    } else {
        Write-Host "##vso[task.logissue type=warning;]vsce not installed. Will skip creation of VS Code extension package"
    }
    Pop-Location
}

##
# VisualStudioExtension
##
function Pack-VS() {
    Write-Host "##[info]Packing VisualStudio extension..."
    Push-Location (Join-Path $PSScriptRoot '..\src\VisualStudioExtension\QsharpVSIX')
    if (Get-Command msbuild -ErrorAction SilentlyContinue) {
        Try {
            msbuild QsharpVSIX.csproj `
                /t:CreateVsixContainer `
                /property:Configuration=$Env:BUILD_CONFIGURATION `
                /property:AssemblyVersion=$Env:ASSEMBLY_VERSION `
                /property:InformationalVersion=$Env:SEMVER_VERSION

            if  ($LastExitCode -ne 0) {
                throw
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to pack VS extension."
            $all_ok = $False
        }
    } else {    
        Write-Host "msbuild not installed. Will skip creation of VisualStudio extension package"
    }
    Pop-Location
}

################################
# Start main execution:

$all_ok = $True

Publish-One '../src/QsCompiler/CommandLineTool/CommandLineTool.csproj'
Publish-One '../src/QuantumSdk/Tools/BuildConfiguration/BuildConfiguration.csproj'

Pack-One '../src/QsCompiler/Compiler/Compiler.csproj' '-IncludeReferencedProjects'
Pack-One '../src/QsCompiler/CommandLineTool/CommandLineTool.csproj' '-IncludeReferencedProjects'
Pack-One '../src/QsLinter/QsLinter.csproj'
Pack-One '../src/ProjectTemplates/Microsoft.Quantum.ProjectTemplates.nuspec'
Pack-One '../src/QuantumSdk/QuantumSdk.nuspec'

if ($Env:ENABLE_VSIX -ne "false") {
    Pack-SelfContained `
        -Project "../src/QsCompiler/LanguageServer/LanguageServer.csproj" `
        -PackageData "../src/VSCodeExtension/package.json"

    Write-Host "Final package.json:"
    Get-Content "../src/VSCodeExtension/package.json" | Write-Host

    Pack-VSCode
    Pack-VS
} else {
    Write-Host "##vso[task.logissue type=warning;]VSIX packing skipped due to ENABLE_VSIX variable."
    return
}

if (-not $all_ok) {
    throw "Packing failed. Check the logs."
    exit 1
} else {
    exit 0
}
