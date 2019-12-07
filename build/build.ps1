# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"

##
# Q# compiler projects
##

function Build-One {
    param(
        [string]$action,
        [string]$project
    );

    Write-Host "##[command]Building $project ($action)..."
    dotnet $action (Join-Path $PSScriptRoot $project) `
        -c $Env:BUILD_CONFIGURATION `
        -v $Env:BUILD_VERBOSITY `
        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
        /property:Version=$Env:ASSEMBLY_VERSION

    if  ($LastExitCode -ne 0) {
        Write-Host "##vso[task.logissue type=error;]Failed to build $project."
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

    Write-Host "##[command]Packing $Project as a self-contained deployment...";
    $Runtimes.GetEnumerator() | ForEach-Object {
        $DotNetRuntimeID = $_.Key;
        $NodePlatformID = $_.Value;
        $TargetDir = New-TemporaryDirectory;
        $BaseName = [System.IO.Path]::GetFileNameWithoutExtension($Project);
        $ArchiveDir = Join-Path $Env:BLOBS_OUTDIR $BaseName;
        New-Item -ItemType Directory -Path $ArchiveDir -Force -ErrorAction SilentlyContinue;

        try {
            $ArchivePath = Join-Path $ArchiveDir "$BaseName-$DotNetRuntimeID-$Env:ASSEMBLY_VERSION.zip";
            dotnet publish (Join-Path $PSScriptRoot $Project) `
                -c $Env:BUILD_CONFIGURATION `
                -v $Env:BUILD_VERBOSITY `
                --self-contained `
                --runtime $DotNetRuntimeID `
                --output $TargetDir `
                /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
                /property:Version=$Env:ASSEMBLY_VERSION
            Write-Host "##[command]Writing self-contained deployment to $ArchivePath..."
            Compress-Archive `
                -Force `
                -Path (Join-Path $TargetDir *) `
                -DestinationPath $ArchivePath `
                -ErrorAction Continue;
            if ($null -ne $PackageData) {
                Write-Hash `
                    -Path $ArchivePath `
                    -BlobPlatform $NodePlatformID `
                    -TargetPath $PackageData
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
function Build-VSCode() {
    Write-Host "##[command]Building VS Code extension..."
    Push-Location (Join-Path $PSScriptRoot '../src/VSCodeExtension')
    if (Get-Command npm -ErrorAction SilentlyContinue) {
        Try {
            npm install
            npm run compile

            if  ($LastExitCode -ne 0) {
                throw
            }
        } Catch {
            Write-Host "##vso[task.logissue type=error;]Failed to build VS Code extension."
            $all_ok = $False
        }
    } else {
        Write-Host "##vso[task.logissue type=warning;]npm not installed. Will skip creation of VS Code extension"
    }
    Pop-Location
}


##
# VisualStudioExtension
##
function Build-VS() {
    Write-Host "##[command]Building VisualStudio extension..."
    Push-Location (Join-Path $PSScriptRoot '..')
    if (Get-Command nuget -ErrorAction SilentlyContinue) {
        Try {
            nuget restore VisualStudioExtension.sln

            if ($LastExitCode -ne 0) {
                throw
            }
            
            if (Get-Command msbuild -ErrorAction SilentlyContinue) {
                Try {
                    msbuild VisualStudioExtension.sln `
                        /property:Configuration=$Env:BUILD_CONFIGURATION `
                        /property:DefineConstants=$Env:ASSEMBLY_CONSTANTS `
                        /property:AssemblyVersion=$Env:ASSEMBLY_VERSION

                    if ($LastExitCode -ne 0) {
                        throw
                    }
                } Catch {
                    Write-Host "##vso[task.logissue type=error;]Failed to build VS extension."
                    $all_ok = $False
                }
            } else {
                Write-Host "msbuild not installed. Will skip building the VisualStudio extension"
            }
        } Catch {
            Write-Host "##vso[task.logissue type=warning;]Failed to restore VS extension solution."
        }
    } else {
        Write-Host "##vso[task.logissue type=warning;]nuget not installed. Will skip restoring and building the VisualStudio extension solution"
    }
    Pop-Location
}

################################
# Start main execution:

$all_ok = $True

Build-One 'build' '../QsCompiler.sln'
Build-One 'publish' '../src/QsCompiler/CommandLineTool/CommandLineTool.csproj'

if ($Env:ENABLE_VSIX -ne "false") {
    Pack-SelfContained `
        -Project "../src/QsCompiler/LanguageServer/LanguageServer.csproj" `
        -PackageData "../src/VSCodeExtension/package.json"

    Write-Host "Final package.json:"
    Get-Content "../src/VSCodeExtension/package.json" | Write-Host

    Build-VSCode
    Build-VS
} else {
    Write-Host "##vso[task.logissue type=warning;]VSIX building skipped due to ENABLE_VSIX variable."
    return
}

if (-not $all_ok) {
    throw "Building failed. Check the logs."
    exit 1
} else {
    exit 0
} 

