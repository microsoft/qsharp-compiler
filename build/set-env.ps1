# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

Write-Host "Setting up build environment variables"

If ($Env:BUILD_CONFIGURATION -eq $null) { $Env:BUILD_CONFIGURATION ="Debug" }
If ($Env:BUILD_VERBOSITY -eq $null) { $Env:BUILD_VERBOSITY ="m" }
If ($Env:ASSEMBLY_VERSION -eq $null) { $Env:ASSEMBLY_VERSION ="0.0.1.0" }
If ($Env:NUGET_VERSION -eq $null) { $Env:NUGET_VERSION ="$Env:ASSEMBLY_VERSION-alpha" }
If ($Env:DROPS_DIR -eq $null) { $Env:DROPS_DIR =  [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\drops")) }
If ($Env:NUGET_OUTDIR -eq $null) { $Env:NUGET_OUTDIR = (Join-Path $Env:DROPS_DIR "nugets") }
If ($Env:VSIX_OUTDIR -eq $null) { $Env:VSIX_OUTDIR = (Join-Path $Env:DROPS_DIR "vsix") }
If ($Env:BLOBS_OUTDIR -eq $null) { $Env:BLOBS_OUTDIR = (Join-Path $Env:DROPS_DIR "blobs") }
