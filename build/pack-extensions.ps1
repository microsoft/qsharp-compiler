# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"
$all_ok = $True

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

    Write-Host "##[info]Building $project ($action)..."
    if ("" -ne "$Env:ASSEMBLY_CONSTANTS") {
        $args = @("/property:DefineConstants=$Env:ASSEMBLY_CONSTANTS");
    }  else {
        $args = @();
    }

    # Make sure the LanguageServer is built on its own:
    dotnet build (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        @args `
        /property:Version=$Env:ASSEMBLY_VERSION `
        /property:InformationalVersion=$Env:SEMVER_VERSION

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
    if (Get-Command npx -ErrorAction SilentlyContinue) {
        Try {
            npx vsce package

            if ($LastExitCode -ne 0) {
                throw;
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to pack VS Code extension."
            $Script:all_ok = $False
        }
    } else {
        Write-Host "##vso[task.logissue type=warning;]npx not installed. Will skip creation of VS Code extension package"
    }
    Pop-Location
}

##
# VisualStudioExtension
##
function Pack-VS() {
    Write-Host "##[info]Packing VisualStudio extension..."
    Push-Location (Join-Path $PSScriptRoot '..\src\VisualStudioExtension\QSharpVsix')
    if (Get-Command msbuild -ErrorAction SilentlyContinue) {
        Try {
            msbuild QSharpVsix.csproj `
                /t:CreateVsixContainer `
                /property:Configuration=$Env:BUILD_CONFIGURATION `
                /property:AssemblyVersion=$Env:ASSEMBLY_VERSION `
                /property:InformationalVersion=$Env:SEMVER_VERSION

            if  ($LastExitCode -ne 0) {
                throw
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to pack VS extension."
            $Script:all_ok = $False
        }
    } else {
        Write-Host "msbuild not installed. Will skip creation of VisualStudio extension package"
    }
    Pop-Location
}


################################
# Start main execution:

$all_ok = $True

Pack-SelfContained `
    -Project "../src/QsCompiler/LanguageServer/LanguageServer.csproj" `
    -PackageData "../src/VSCodeExtension/package.json"

Pack-VSCode
Pack-VS

if (-not $all_ok) {
    throw "Packing failed. Check the logs."
    exit 1
} else {
    exit 0
}
