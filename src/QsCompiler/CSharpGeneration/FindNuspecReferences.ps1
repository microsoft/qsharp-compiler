# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

########################################
# When creating a package with dotnet pack, nuget changes every ProjectReference to be itself
# a PackageReference (without cheking if that project has a corresponding package).
# This is problematic because we currently don't want to create a package for every dll 
# in the compiler.
# On the other hand, when creating a package using nuget pack, nuget does not
# identify PackageReferences defined in the csproj, so all the dependencies (like
# FParsec or F#) are not listed and the package doesn't work.
#
# We don't want to hardcode the list of dependencies on the .nuspec, as they can
# quickly become out-of-sync.
# This script will find the PackageReferences recursively on the QsCompiler project and add them
# to its nuspec, so we can then create the package using nuget pack with the corresponding
# dependencies listed.
#
# nuget is tracking this problem at: https://github.com/NuGet/Home/issues/4491
########################################

using namespace System.IO

$target = Join-Path $PSScriptRoot 'Microsoft.Quantum.CSharpGeneration.nuspec'
if (Test-Path $target) {
    Write-Host "$target exists. Skipping generating new one."
    exit
}

$nuspec = [Xml](Get-Content (Join-Path $PSScriptRoot 'Microsoft.Quantum.CSharpGeneration.nuspec.template'))
$dependencies = $nuspec.CreateElement('dependencies', $nuspec.package.metadata.NamespaceURI)

# Adds a dependency to the dependencies element if it does not already exist.
function Add-Dependency($Id, $Version) {
    if (-not ($dependencies.dependency | Where-Object { $_.id -eq $Id })) {
        Write-Host "Adding dependency $Id."
        $dependency = $nuspec.CreateElement('dependency', $nuspec.package.metadata.NamespaceURI)
        $dependency.SetAttribute('id', $Id)
        $dependency.SetAttribute('version', $Version)
        $dependencies.AppendChild($dependency)
    }
}

# Recursively find PackageReferences on all ProjectReferences.
function Add-PackageReferenceDependencies($ProjectFileName) {
    $project = [Xml](Get-Content $ProjectFileName)

    # Add all package references as dependencies.
    $project.Project.ItemGroup.PackageReference | Where-Object { $null -ne $_ } | ForEach-Object {
        $id = if ($_.Include) { $_.Include } else { $_.Update }
        Add-Dependency $id $_.Version
    }

    # Recursively add dependencies from all project references.
    $project.Project.ItemGroup.ProjectReference | Where-Object { $null -ne $_ } | ForEach-Object {
        Add-PackageReferenceDependencies $_.Include
    }
}

# Add dependencies for the projects included in this NuGet package.
Add-PackageReferenceDependencies 'Microsoft.Quantum.CSharpGeneration.fsproj'
$dependency = $dependencies.AppendChild($nuspec.CreateElement('dependency', $nuspec.package.metadata.NamespaceURI))
$dependency.SetAttribute('id', 'Microsoft.Quantum.Compiler')
$dependency.SetAttribute('version', '$version$')

$nuspec.package.metadata.AppendChild($dependencies)
$nuspec.Save([Path]::Combine((Get-Location), $target))
