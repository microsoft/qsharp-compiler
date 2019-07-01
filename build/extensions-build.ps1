param(
    [string]
    $Configuration = "Debug",

    [string]
    $AssemblyVersion = "0.0.0.1",

    [string]
    $AssemblyConstants = ""

);

$ErrorActionPreference = 'Stop'

###
# Build VisualStudio Extension
##
nuget restore ..\VisualStudioExtension.sln
msbuild ..\VisualStudioExtension.sln `
    /property:Configuration=$Configuration `
    /property:DefineConstants=$AssemblyConstants `
    /property:AssemblyVersion=$AssemblyVersion `


###
# Build VSCode Extension
##
pushd ..\src\VSCodeExtension\
npm install
npm run compile
popd

