# This script is useful for testing integration with Azure Notebooks.
# It will replace the language server the Azure Notebooks setup script has
# downloaded and extracted with your local version of the language server.

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/set-env.ps1"

# Based on your personal setup, you may need to change these two lines
$DotNetRuntimeID = "win10-x64"
$TargetDir = Join-Path $PSScriptRoot "../../AzureNotebooks/src/service/aznb/NotebookLanguageServer/dist/qsharp"
$Project = "../src/QsCompiler/LanguageServer/LanguageServer.csproj"

if (Test-Path -Path $TargetDir) {
    Remove-Item -Recurse $TargetDir
}

# These two dotnet commands are taken from the pack-extensions.ps1 script

dotnet build (Join-Path $PSScriptRoot $Project) `
    -c $Env:BUILD_CONFIGURATION `
    -v $Env:BUILD_VERBOSITY `
    /property:Version=$Env:ASSEMBLY_VERSION `
    /property:InformationalVersion=$Env:SEMVER_VERSION

dotnet publish (Join-Path $PSScriptRoot $Project) `
    -c $Env:BUILD_CONFIGURATION `
    -v $Env:BUILD_VERBOSITY `
    --self-contained `
    --runtime $DotNetRuntimeID `
    --output $TargetDir `
    /property:Version=$Env:ASSEMBLY_VERSION `
    /property:InformationalVersion=$Env:SEMVER_VERSION
