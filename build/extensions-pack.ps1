param(
    [string]
    $Configuration = "Debug",

    [string]
    $AssemblyVersion = "0.0.0.1",

    [string]
    $OutDir = "."
);

$ErrorActionPreference = 'Stop'

###
# Build VisualStudio Extension
##
Write-Host "##[info]Packaging VisualStudioExtension"
msbuild ..\VisualStudioExtension.sln `
    /t:CreateVsixContainer `
    /property:Configuration=$Configuration `
    /property:AssemblyVersion=$AssemblyVersion 

###
# Build VSCode Extension
##
Write-Host "##[info]Packaging VSCodeExtension"
pushd ..\src\VSCodeExtension\
npm install vsce
vsce package
popd


###
# Copying VSIX to output folder.
##
Write-Host "##[info]Copying VSIX to output folder $OutDir"
If(!(Test-Path $OutDir))
{
    New-Item -ItemType Directory -Force -Path $OutDir
}
move -Force ..\src\VisualStudioExtension\QsharpVSIX\bin\$Configuration\*.vsix $OutDir
move -Force ..\src\VSCodeExtension\*.vsix $OutDir

