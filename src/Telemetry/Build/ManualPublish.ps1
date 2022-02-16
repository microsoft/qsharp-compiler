# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param(
  #[Parameter(Mandatory=$true)]
  [String]$PackageVersion,
  [String]$AssemblyVersion
)

Push-Location $PSScriptRoot/../

try
{
  $QdkPublicAlphaSource = "https://pkgs.dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_packaging/alpha/nuget/v3/index.json"

  if ((Get-PackageSource | Where-Object Name -eq "QdkPublicAlpha").Count -eq 0)
  {
    Register-PackageSource -ProviderName NuGet -Name QdkPublicAlpha -Location $QdkPublicAlphaSource | Out-Null
  }
  else
  {
    Set-PackageSource -ProviderName NuGet -Name QdkPublicAlpha -NewLocation $QdkPublicAlphaSource | Out-Null
  }

  $LatestVersion =
    Find-Package -Name Microsoft.Quantum.Telemetry -Source QdkPublicAlpha -AllVersions -AllowPrereleaseVersions `
    | Select-Object Version `
    | Select-String '(?<major>\d+)(\.)(?<minor>\d+)(\.)(?<patch>\d+)(-(?<channel>[A-Z]+))?'  `
    | ForEach-Object { $_.Matches.Captures } `
    | Select-Object @{ Name='Major'; Expression= { [int]$_.Groups["major"].Value } } `
                    ,@{ Name='Minor'; Expression={ [int]$_.Groups["minor"].Value } } `
                    ,@{ Name='Patch'; Expression={ [int]$_.Groups["patch"].Value } } `
                    ,@{ Name='Channel'; Expression={ $_.Groups["channel"].Value } } `
    | Where-Object Channel -eq "alpha" `
    | Sort-Object Major, Minor, Patch -Descending `
    | Select-Object -First 1

  $LatestVersion = $LatestVersion[0]
  $LatestPackageVersion = "$($LatestVersion.Major).$($LatestVersion.Minor).$($LatestVersion.Patch)-$($LatestVersion.Channel)"

  $NewVersion = $LatestVersion.PSObject.Copy()
  $NewVersion.Patch++;

  if ([String]::IsNullOrEmpty($PackageVersion))
  {
    $PackageVersion = "$($NewVersion.Major).$($NewVersion.Minor).$($NewVersion.Patch)-alpha"
  }

  if ([String]::IsNullOrEmpty($AssemblyVersion))
  {
    $AssemblyVersion = "$($NewVersion.Major).$($NewVersion.Minor).$($NewVersion.Patch).0"
  }

  Write-Output "Latest published package version: $($LatestPackageVersion)"
  Write-Output "New package version: $($PackageVersion)"
  Write-Output "New assembly version: $($AssemblyVersion)"
  Write-Output ""

  $PackagePath = "../../drops/nugets/Microsoft.Quantum.Telemetry.$PackageVersion.nupkg"

  Write-Output "Building package at: $($PackagePath)"
  Write-Output ""

  dotnet pack ./Library/Telemetry.csproj `
      -o ../../drops/nugets/ `
      -c Release `
      /property:Version=$AssemblyVersion `
      /property:PackageVersion=$PackageVersion

  Write-Output ""
  Write-Output "Publishing package to: $($QdkPublicAlphaSource)"
  Write-Output ""

  dotnet nuget push $PackagePath --source $QdkPublicAlphaSource --interactive --api-key AzureDevOps --skip-duplicate
}
finally
{
  Pop-Location
}

