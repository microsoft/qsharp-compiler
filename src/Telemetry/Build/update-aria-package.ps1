# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [String]$packageVersion
)

Push-Location $PSScriptRoot/../

$PackagePath = "$HOME/.nuget/packages/microsoft.applications.events.client/$PackageVersion/microsoft.applications.events.client.$($PackageVersion).nupkg"
$QdkAlphaSource = "https://ms-quantum.pkgs.visualstudio.com/_packaging/alpha/nuget/v3/index.json"
$OneSdkSource = "https://msasg.pkgs.visualstudio.com/_packaging/OneSDK/nuget/v3/index.json"

# Remove the old version and add the new version of the package
dotnet remove ./Library/Telemetry.csproj package Microsoft.Applications.Events.Client
dotnet add ./Library/Telemetry.csproj package Microsoft.Applications.Events.Client -v $PackageVersion -s $OneSdkSource  --interactive

# Push the new version to the QDK Alpha source
dotnet nuget push $PackagePath --source $QdkAlphaSource --interactive --api-key AzureDevOps --skip-duplicate

Pop-Location
