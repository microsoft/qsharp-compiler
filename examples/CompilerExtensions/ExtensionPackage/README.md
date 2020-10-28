# Creating a NuGet package containing a Q# compiler extension

This project contains a template for packaging a Q# compiler extension. For more information about Q# compiler extensions see [here](https://github.com/microsoft/qsharp-compiler/tree/main/src/QuantumSdk#extending-the-q-compiler). For more information on NuGet packages, see [here](https://docs.microsoft.com/en-us/nuget/what-is-nuget).

Prerequisites: [NuGet tools](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools)

To create the package, follow the following steps:
- From within the project directory, run `dotnet build`.
- From within the project directory, run `nuget pack`.
- Copy the created .nupkg file into you [local NuGet folder](https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds).
- You can now use that package like any other NuGet package. 

In order to use the created package as a Q# compiler extension when building a Q# project, add the following package reference to your project file:
```
    <PackageReference Include="CustomExtension.Package" Version="1.0.0" IsQscReference="true" />
```
The extension will only be included in the build process if`IsQscReference` is set to `true`. For more information, see this [readme](https://github.com/microsoft/qsharp-compiler/blob/main/src/QuantumSdk/README.md). 
