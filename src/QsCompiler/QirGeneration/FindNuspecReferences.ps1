# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

########################################
# When creating a package with dotnet pack, nuget changes every ProjectReference to be itself
# a PackageReference without checking if that project has a corresponding package.
# This is problematic because we don't want to create a separate package the dlls in the QIR emission.
# On the other hand, when creating a package using nuget pack, nuget does not
# identifies PackageReferences defined in the project file, so all the dependencies need to be
# explicitly listed for the package to work.
#
# We don't want to hardcode the list of dependencies on the .nuspec, as they can quickly become out-of-sync.
# This script will find the PackageReferences recursively on the Compiler project and add them
# to its nuspec, so we can then create the package using nuget pack with the corresponding dependencies listed.
#
# nuget is tracking this problem at: https://github.com/NuGet/Home/issues/4491
########################################

# Start with the nuspec template
$nuspec = [xml](Get-Content "QirGeneration.nuspec.template")
$dep = $nuspec.CreateElement('dependencies', $nuspec.package.metadata.NamespaceURI)


# Recursively find PackageReferences on all ProjectReferences:
function Add-NuGetDependencyFromCsprojToNuspec($PathToCsproj)
{
    $csproj = [xml](Get-Content $PathToCsproj)

    # Find all PackageReferences nodes:
    $packageDependency = $csproj.Project.ItemGroup.PackageReference | Where-Object { $null -ne $_ }
    $packageDependency | ForEach-Object {
        # Identify package's id either from "Include" or "Update" attribute:
        $id = $_.Include
        if ($id -eq $null -or $id -eq "") {
            $id = $_.Update
        }

        # Check if package already added as dependency, only add if new:
        $added = $dep.dependency | Where { $_.id -eq $id }
        if (!$added -and $_.PrivateAssets -ne "All") {
            Write-Host "Adding $id"
            $onedependency = $dep.AppendChild($nuspec.CreateElement('dependency', $nuspec.package.metadata.NamespaceURI))
            $onedependency.SetAttribute('id', $id)
            $onedependency.SetAttribute('version', $_.Version)
        }
    }
}

# Find all package dependencies on QirGeneration.csproj,
# and add the compiler as a package dependency instead.
# The llvm bindings need to be published before including them in the package,
# and are hence already included by the template.
Add-NuGetDependencyFromCsprojToNuspec "QirGeneration.csproj" $dep
$dependency = $dep.AppendChild($nuspec.CreateElement('dependency', $nuspec.package.metadata.NamespaceURI))
$dependency.SetAttribute('id', 'Microsoft.Quantum.Compiler')
$dependency.SetAttribute('version', '$version$')


# Save into .nuspec file:
$nuspec.package.metadata.AppendChild($dep)
$nuspec.Save((Join-Path $PSScriptRoot QirGeneration.nuspec))
