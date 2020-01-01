# The Microsoft Quantum Sdk #

## Content ##

The NuGet package Microsoft.Quantum.Sdk serves a .NET Core Sdk for developing quantum applications. 
It contains the properties and targets that define the compilation process for Q# projects, tools used as part of the build, as well as some project system support for Q# files. It in particular also provides the support for executing a compilation step defined in a package or project reference as part of the compilation process. See [this section](#extending-the-q#-compiler) for more details.

The Sdk includes all \*.qs files within the project directory as well as the Q# standard libraries by default. No additional reference to `Microsoft.Quantum.Standard` is needed. For more details see the [section on defined properties](#defined-project-properties) below.  

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
Marking all assets as private by adding a `PrivateAssets="All"` attribute is generally a good practice if the reference is a development only dependency, which is often the case for assemblies that implement rewrite steps.

A custom compilation step is defined by a class that implements the [IRewriteStep interface](https://github.com/microsoft/qsharp-compiler/blob/master/src/QsCompiler/Compiler/PluginInterface.cs). The output assembly of a project reference or any .NET Core library contained in a package reference marked as qsc reference is loaded during compilation and searched for classes implementing the `IRewriteStep` interface. Any such class is instantiated using the default constructor, and the implemented transformation is executed. 

[comment]: # (TODO: add a section detailing the IRewriteStep interface, and link it here)

The order in which these steps are executed can be configured by specifying their priority:
```
    <ProjectReference Include="MyCustomCompilationStep.csproj" IsQscReference="true" Priority="2"/>
    ...
    <PackageReference Include="MyCustom.Qsharp.Compiler.Extensions" Version="1.0.0.0" IsQscReference="true" Priority="1"/>
```
If no priority is specified, the priority for that reference is set to zero. 
Steps defined within packages or projects with higher priority are executed first. If several classes within a certain reference implement the `IRewriteStep` interface, then these steps are executed according to their priority specified as part of the interface. The priority defined for the project or package reference takes precedence, such that the priorities defined by the interface property are not compared across assemblies.   

[comment]: # (TODO: describe how to limit included rewrite steps to a particular execution target)

### Injected C# code ###

It is possible to inject C# code into Q# packages e.g. for integration purposes. By default the Sdk is configured to do just that, see also the section on [defined properties](#defined-project-properties). That code may be generated as part of a custom rewrite step, e.g. if the code generation requires information about the Q# compilation. 
The Sdk defines a couple of build targets that can be redefined to run certain tasks before important build stages. These targets do nothing by default and merely serve as handles to easily integrate into the build process.

The following such targets are currently available: 

- `BeforeQsharpCompile`:    
The target will execute right before the Q# compiler is invoked. All assembly references and the paths to all qsc references will be resolved at that time.   

- `BeforeCsharpCompile`:    
The target will execute right before the C# compiler is invoked. All Q# compilation steps will have completed at that time, and in particular all rewrite steps will have been executed.  

For example, if a qsc reference contains a rewrite step that generates C# code during transformation, that code can be included into the built dll by adding the following target to the project file: 

```
  <Target Name="BeforeCsharpCompile">
    <ItemGroup>
      <Compile Include="$(GeneratedFilesOutputPath)**/*.cs" Exclude="@(Compile)" AutoGen="true" />
    </ItemGroup>
  </Target>  
```

### Trouble shooting compiler extensions ###

The compiler attempts to load rewrite steps even if these have been compiled against a different compiler version. While we do our best to mitigate issue due to a version mismatch, it is generally recommended to use compiler extensions that are compiled against the same compiler package version as the Sdk version of the project. 

When a rewrite steps fails to execute, setting the `QscVerbosity` to "Detailed" or "Diagnostic" will log the encountered exception with stack trace:
```
  <QscVerbosity>Detailed</QscVerbosity>
```
By default, the compiler will search the project output directory for a suitable assembly in case a dependency cannot be found. In case of a `FileNotFoundException`, and option may be to add the corresponding reference to the project, or to copy the missing dll to the output directory. 

## Defined project properties ##

The Sdk defines the following properties for each project using it: 

- `QsharpLangVersion`:    
The version of the Q# language specification.

- `QuantumSdkVersion`:    
The NuGet version of the Sdk package.

The following properties can be configured to customize the build: 

- `CsharpGeneration`:    
Specifies whether to generate C# code as part of the compilation process. Setting this property to false may prevent certain interoperability features or integration with other pieces of the Quantum Development Kit. 

- `IncludeQsharpCorePackages`:     
Specifies whether the packages providing the basic language support for Q# are referenced. This property is set to true by default. If set to false, the Sdk will not reference any Q# libraries. 

- `QscExe`:    
The command to invoke the Q# compiler. The value set by default invokes the Q# compiler that is packaged as tool with the Sdk. The default value can be accessed via the `DefaultQscExe` property. 

- `QscVerbosity`:    
Defines the verbosity of the Q# compiler. Recognized values are: Quiet, Minimal, Normal, Detailed, and Diagnostic.

- `QsharpDocsGeneration`:    
Specified whether to generate yml documentation for the compiled Q# code. The default value is "false". 

- `QsharpDocsOutputPath`:    
Directory where any generated documentation will be saved. 

[comment]: # (TODO: document QscBuildConfigExe, QscBuildConfigOutputPath)

# Sdk Packages #

To understand how the content in this package works it is useful to understand how the properties, item groups, and targets defined in the Sdk are combined with those defined by a specific project. 
The order of evaluation for properties and item groups is roughly the following: 

- Properties defined in \*.props files of the Sdk
- Properties defined or included by the specific project file
- Properties defined in *.targets files of the Sdk
- Item groups defined in *.props files of the Sdk
- Item groups defined or included by the specific project file
- Item groups defined in *.targets files of the Sdk  

Similar considerations apply for the definition of targets. MSBuild will overwrite targets if multiple targets with the same name are defined. In that case, the target is replaced in its entirety independent on whether the values for `DependsOn`, `BeforeTarget`, and `AfterTarget` match - i.e. those will be overwritten. However, a target can be "anchored" by the surrounding targets' specifications of their dependencies, see e.g. the defined `BeforeCsharpCompile` target. 

## Load context in .NET Core

To avoid issues with conflicting packages, we load each Q# compiler extension into its own context. For more information, see the Core CLR [design docs](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/assemblyloadcontext.md). 

## Known Issues ##

The following issues and PRs may be of interest when using the Sdk:
> https://github.com/NuGet/Home/issues/8692    
> https://github.com/dotnet/runtime/issues/949    
> https://github.com/NuGet/NuGet.Client/pull/3170    
