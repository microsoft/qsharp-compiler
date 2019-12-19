# The Microsoft Quantum Sdk #

## Content ##

The NuGet package Microsoft.Quantum.Sdk serves a .NET Core Sdk for developing quantum applications. 
It contains the properties and targets that define the compilation process for Q# projects, tools used as part of the build, as well as some project system support for Q# files. It in particular also provides the support for executing a compilation step defined in a package or project reference as part of the compilation process. See [this section](#extending-the-q#-compiler) for more details.

The Sdk includes all *.qs files within the project directory as well as the Q# standard libraries by default. No additional reference to `Microsoft.Quantum.Standard` is needed. 

## Using the Sdk ##

To use the Quantum Sdk simply list the NuGet package as Sdk at the top of your project file: 
```
<Project Sdk="Microsoft.Quantum.Sdk/0.10.1912.1803-alpha">
    ...
</Project>
```
See also the [MSBuild documentation](https://docs.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk?view=vs-2019).

If you would like to build the Microsoft.Quantum.Sdk NuGet package from source, first run the `bootstrap.ps1` script in the root of this repository and then run the `build/pack.ps1` script. You will find the built package in the generated `drops` folder under `nugets`. Simply add the package to your local NuGet folder to start using it. 

[comment]: # (TODO: add a section on specifying an execution target)

## Extending the Q# compiler ##

Any project that uses the Quantum Sdk can easily incorporate custom compilation steps into the build process. Syntax tree rewrite steps defined in a package or project reference can be executed as part of the Q# compilation process by marking the package or project as qsc reference:
```
    <ProjectReference Include="MyCustomCompilationStep.csproj" IsQscReference="true"/>
    ...
    <PackageReference Include="MyCustom.Qsharp.Compiler.Extensions" Version="1.0.0.0" IsQscReference="true"/>
```
A custom compilation step is defined by a class that implements the [IRewriteStep interface](https://github.com/microsoft/qsharp-compiler/blob/master/src/QsCompiler/Compiler/PluginInterface.cs). The output assembly of a project reference or any .NET Core library contained in a package reference marked as qsc reference is loaded during compilation and searched for classes implementing the `IRewriteStep` interface. Any such class is instantiated using the default constructor, and the implemented transformation is executed. 

The order in which these steps are executed can be configured by specifying their priority:
```
    <ProjectReference Include="MyCustomCompilationStep.csproj" IsQscReference="true" Priority="2"/>
    ...
    <PackageReference Include="MyCustom.Qsharp.Compiler.Extensions" Version="1.0.0.0" IsQscReference="true" Priority="1"/>
```
If no priority is specified, the priority for that reference is set to zero. 
Steps defined within packages or projects with higher priority are executed first. If several classes within a certain reference implement the `IRewriteStep` interface, then these steps are executed according to their priority specified as part of the interface. The priority defined for the project or package reference takes precedence, such that the priorities defined by the interface property are not compared across assemblies.   

[comment]: # (TODO: describe how to limit included rewrite steps to a particular execution target)

## Defined project properties

The Sdk defines the following properties for each project using it: 

- `QsharpLangVersion`:    
The version of the Q# language specification.

- `QuantumSdkVersion`:    
The NuGet version of the Sdk package.

The following properties can be configured to customize the build: 

- `QscExe`:    
The command to invoke the Q# compiler. The value set by default invokes the Q# compiler that is packaged as tool with the Sdk. The default value can be accessed via the `DefaultQscExe` property. 

- `QscVerbosity`:    
Defines the verbosity of the Q# compiler. Recognized values are: Quiet, Minimal, Normal, Detailed, and Diagnostic.

- `QsharpDocsGeneration`:    
Specified whether to generate yml documentation for the compiled Q# code. The default value is "false". 

- `QsharpDocsOutputPath`:    
Directory where any generated documentation will be saved. 

[comment]: # (TODO: document QscBuildConfigExe, QscBuildConfigOutputPath)


# Sdk Packages

To understand how the content in this package works it is useful to understand how the properties, item groups, and targets defined in the Sdk are combined with those defined by a specific project. 
The order of evaluation for properties and item groups is roughly the following: 

- Properties defined in *.props files of the Sdk
- Properties defined or included by the specific project file
- Properties defined in *.targets files of the Sdk
- Item groups defined in *.props files of the Sdk
- Item groups defined or included by the specific project file
- Item groups defined in *.targets files of the Sdk  

