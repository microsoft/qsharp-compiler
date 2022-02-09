# Optimizing QIR

Since QIR is a specification of LLVM IR, the IR code can be manipulated via the usual tools provided by LLVM, such as [Clang](https://clang.llvm.org/), the [LLVM optimizer](https://llvm.org/docs/CommandGuide/opt.html), the [LLVM static compiler](https://llvm.org/docs/CommandGuide/llc.html), and the [just-in-time (JIT) compiler](https://llvm.org/docs/CommandGuide/lli.html).
This example is structured as a walk-through of the process covering installation, QIR generation, running built-in and custom LLVM optimization pipelines, and compiling to an executable.

## Prerequisites

QIR generation is handled by the Q# compiler, while optimizations are performed at the LLVM level.
The following software will be used in this walk-through:

* *Quantum Development Kit (QDK)* : contains the Q# compiler
* *Clang* : LLVM based compiler for the C language family
* *LLVM optimizer* : tool to run custom optimization pipelines on LLVM IR code

### Installing the QDK

The *Quantum Development Kit (QDK)* documentation provides detailed guides on [installing the QDK](https://docs.microsoft.com/azure/quantum/install-overview-qdk#install-the-qdk-for-quantum-computing-locally) for various setups.
This document will assume a **command line** setup for **Q# standalone** applications.

Steps for QDK v0.18.2106 (June 2021):

    NOTE: check the install guide linked above for the most up-to-date instructions

* Install the [.NET Core SDK 3.1](https://dotnet.microsoft.com/download) (NOTE: use `dotnet-sdk-3.1` instead of `dotnet-sdk-5.0` in the Linux guides)
* Install the QDK with `dotnet new -i Microsoft.Quantum.ProjectTemplates`
* (**Linux**) Ensure the [GNU OpenMP support library](https://gcc.gnu.org/onlinedocs/libgomp/) is installed on your system, e.g. via `sudo apt install libgomp1`

### Installing Clang

Depending on your platform, some LLVM tools may only be available by compiling from source.
*Clang* however should be readily available from package managers or as pre-compiled binaries on most systems.
LLVM's [official release page](https://releases.llvm.org/download.html) provides sources and binaries of LLVM and Clang.
It's recommended to use LLVM version 11.1.0, as this is the version used by the Q# compiler.

Here are some suggestions on how to obtain Clang for different platforms.
On Windows, the conda method is recommended since it's also able to install the LLVM optimizer (see next section).

Package managers:

* **Ubuntu** : `sudo apt install clang-13`
* **Windows** : `choco install llvm --version=13.0.0` using [chocolatey](https://chocolatey.org/)
* **macOS** : `brew install llvm@11` using [homebrew](https://brew.sh/)

* **all** : `conda install -c conda-forge clang=13.0.0 clangxx=13.0.0 llvm=13.0.0` using [conda](https://conda.io/projects/conda/en/latest/user-guide/install/index.html)

Pre-built binaries/installers:

* **Ubuntu** : get `clang+llvm-13.0.0-x86_64-linux-gnu-ubuntu-20.10.tar.xz` from the [GitHub release](https://github.com/llvm/llvm-project/releases/tag/llvmorg-13.0.0)
* **Windows** : get `LLVM-13.0.0-win64.exe` from the [GitHub release](https://github.com/llvm/llvm-project/releases/tag/llvmorg-13.0.0)
* **macOS** : get `clang+llvm-13.0.0-x86_64-apple-darwin.tar.xz` from the [13.0.0 release](https://github.com/llvm/llvm-project/releases/tag/llvmorg-13.0.0)


(**Linux**) If installing via `apt`, the clang/llvm commands will have `-13` attached to their name.
It's convenient to define aliases for these commands so as not to have to type out the full name every time.
If you want to skip this step, substitute `clang`/`clang++`/`opt` with `clang-13`/`clang++-13`/`opt-13` throughout the rest of this document.

```bash
echo 'alias clang=clang-13' >> ~/.bashrc
echo 'alias clang++=clang++-13' >> ~/.bashrc
echo 'alias opt=opt-13' >> ~/.bashrc
```

Restart the terminal for the aliases to take effect.

### Installing the LLVM optimizer

If you followed the instructions above specific to Linux or macOS, your LLVM installation will have included the *LLVM optimizer*.
You can test this by typing `opt` or `opt-11` in your terminal.

If not, you can use [conda](https://conda.io/projects/conda/en/latest/user-guide/install/index.html) as shown below to get it:

```shell
conda install -c conda-forge llvm-tools=11.1.0
```

Although a rather involved process, [compiling LLVM from source](https://llvm.org/docs/GettingStarted.html) is also available and provides access to all tools under the LLVM project.

## Generating QIR

The starting point for QIR generation is a Q# program or library.
This section details the process for a simple *hello world* program, but any valid Q# code can be substituted in its stead.

### Creating a Q# project

The first step is to create a .NET project for a standalone Q# application.
The QDK project templates facilitate this task, which can be invoked with:

```shell
dotnet new console -lang Q# -o Hello
```

Parameters:

* `console` : specify the project to be a standalone console application
* `-lang Q#` : load the templates for Q# projects
* `-o Hello` : the project name, all files will be generated inside a folder of this name

Other configurations are also possible, such as [Q# libraries with a C# host program](https://docs.microsoft.com/azure/quantum/install-csharp-qdk?tabs=tabid-cmdline%2Ctabid-csharp#creating-a-q-library-and-a-net-host).

The standard Q# template produces a hello world program in the file `Program.qs`:

```csharp
namespace Hello {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation HelloQ() : Unit {
        Message("Hello quantum world!");
    }
}
```

This program will be compiled to QIR and subsequently optimized.

### Adjusting the project file

Q# uses .NET project files to control the compilation process, located in the project root folder under `<project-name>.csproj`.
The standard one provided for a standalone Q# application looks as follows:

```xml
<Project Sdk="Microsoft.Quantum.Sdk/0.18.2106148911">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>

</Project>
```

Enabling QIR generation is a simple matter of adding the `<QirGeneration>` property to the project file:

```xml
<Project Sdk="Microsoft.Quantum.Sdk/0.18.2106148911">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <QirGeneration>true</QirGeneration>
  </PropertyGroup>

</Project>
```

### Building the project

Build the project by running `dotnet build` from the project root folder (`cd Hello`), or specify the path manually as `dotnet build path/to/Hello.csproj`.
Instead of building the (simulator) executable, the compiler will create a `qir` folder with an LLVM representation of the program (`Hello.ll`).

For small projects, such as the default hello world program, a lot of the generated QIR code may not actually be required to run the program.
The next section describes how to run optimization passes on QIR, which can strip away unnecessary code or perform various other transformations written for LLVM.

## Optimizing QIR

While Clang is typically used to compile and optimize e.g. C code, it can also be used to manipulate LLVM IR directly.
The command below tells Clang to run a series of optimizations on the generated QIR code `Hello.ll` and output back LLVM IR (run from the qir folder):

```shell
clang -S qir/Hello.ll -O3 -emit-llvm -o qir/Hello-o3.ll
```

Parameters (see also the [clang man page](https://clang.llvm.org/docs/CommandGuide/clang.html)):

* `-S` : turn off assembly and linking stages (since we want to stay at the IR level)
* `qir/Hello.ll` : the input file (in this case human-readable LLVM IR)
* `-O3` : LLVM optimization level, ranging from O0 to O3 (among others)
* `-emit-llvm` : output LLVM IR instead of assembly code
* `-o qir/Hello-o3.ll` : the output file

The resulting QIR code is now significantly smaller, only containing the declarations and definitions used by the hello world program:

```llvm
%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

declare %String* @__quantum__rt__string_create(i8*) local_unnamed_addr

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

define void @Hello__HelloQ__Interop() local_unnamed_addr #0 {
entry:
  %0 = tail call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  tail call void @__quantum__rt__message(%String* %0)
  tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

define void @Hello__HelloQ() local_unnamed_addr #1 {
entry:
  %0 = tail call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  tail call void @__quantum__rt__message(%String* %0)
  tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
```

### Customizing the pass pipeline

Unfortunately, Clang is limited to a pre-configured set of optimization levels, such as `-03` used above.
In order to specify a custom pass pipeline, the LLVM optimizer `opt` is required.

On the hello world program, the *global dead code elimination (DCE)* pass should achieve very similar results compared to optimization via `-O3`.
Invoke it as follows:

```shell
opt -S qir/Hello.ll -globaldce -o qir/Hello-dce.ll
```

Parameters (see also the [opt man page](https://llvm.org/docs/CommandGuide/opt.html)):

* `-S` : output human-readable LLVM IR instead of bytecode
* `qir/Hello.ll` : the input file
* `-globaldce` : global DCE pass, removes unused definitions/declarations
* `-o qir/Hello-dce.ll` : the output file (stdout if omitted)

This produces the following code:

```llvm
%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

define internal void @Hello__HelloQ__body() {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare %String* @__quantum__rt__string_create(i8*)

declare void @__quantum__rt__message(%String*)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

define void @Hello__HelloQ__Interop() #0 {
entry:
  call void @Hello__HelloQ__body()
  ret void
}

define void @Hello__HelloQ() #1 {
entry:
  call void @Hello__HelloQ__body()
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
```

Note that compared to the output from `-O3`, the function `Hello__HelloQ__body` was not inlined, as well as some other small differences such as missing tail calls.
Add *function inlining* with the following pass:

```shell
opt -S qir/Hello.ll -globaldce -inline -o qir/Hello-dce-inline.ll
```

Which produces:

```llvm
%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

declare %String* @__quantum__rt__string_create(i8*)

declare void @__quantum__rt__message(%String*)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

define void @Hello__HelloQ__Interop() #0 {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

define void @Hello__HelloQ() #1 {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
```

Check out the full list of [LLVM passes](https://llvm.org/docs/Passes.html) for other optimizations.

## Running QIR

Since QIR code *is* LLVM IR, the usual code generation tools provided by LLVM can be used to produce an executable.
However, in order to handle QIR-specific types and functions, proper linkage of the QIR runtime and simulator libraries is required.

### Obtaining the QIR runtime & simulator

The [QIR runtime](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime) is distributed in the form of a NuGet package, from which we will pull the necessary library files.
The same goes for the [full state quantum simulator](https://docs.microsoft.com/azure/quantum/user-guide/machines/full-state-simulator), which the QIR runtime can hook into to simulate the quantum program.
In this section, the project file `Hello.csproj` is modified to generate these library files automatically.

For convenience, a variable `BuildOutputPath` is defined with the following line added to the top-level `PropertyGroup` section:

```xml
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <QirGeneration>true</QirGeneration>
    <BuildOutputPath>$(MSBuildThisFileDirectory)build</BuildOutputPath>
  </PropertyGroup>
```

All QIR runtime and simulator dependencies will be copied there.

Next, the aforementioned NuGet package dependencies must be declared.
One for the runtime and one for the simulator, using the `PackageReference` command:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Quantum.Qir.Runtime" Version="0.18.2106148911-alpha" GeneratePathProperty="true" />
    <PackageReference Include="Microsoft.Quantum.Simulators" Version="0.18.2106148911" GeneratePathProperty="true" />
  </ItemGroup>
```

The package versions should match the version of the QDK specified at the top of the file, however, the runtime is only available as an alpha version at the moment.
The `GeneratePathProperty` will allow us to directly reference specific files in the packages later on.

Lastly, a new build target is added called `GetDependencies`:

```xml
<Target Name="GetDependencies" AfterTargets="Build">
```

The property `AfterTargets` indicates the target is to be run after the regular build stage.

Inside, we simply copy library and C++ header files from the packages into the build folder with the `Copy` command:

```xml
    <Copy SourceFiles="$(SimulatorRuntime)" DestinationFolder="$(BuildOutputPath)" SkipUnchangedFiles="true" />
    <Copy SourceFiles="@(_QirRuntimeLibFiles)" DestinationFolder="$(BuildOutputPath)\%(RecursiveDir)" SkipUnchangedFiles="true" />
    <Copy SourceFiles="@(_QirRuntimeHeaderFiles)" DestinationFolder="$(BuildOutputPath)\%(RecursiveDir)" SkipUnchangedFiles="true" />
```

The variables used to specify source files must be defined appropriately for each operating system.
For example, only these definitions would be active on Windows:

```xml
      <QirRuntimeHeaders>$(PkgMicrosoft_Quantum_Qir_Runtime)/runtimes/any/native/include</QirRuntimeHeaders>
      <QirRuntimeLibs Condition="$([MSBuild]::IsOsPlatform('Windows'))">$(PkgMicrosoft_Quantum_Qir_Runtime)/runtimes/win-x64/native</QirRuntimeLibs>
      <SimulatorRuntime Condition="$([MSBuild]::IsOsPlatform('Windows'))">$(PkgMicrosoft_Quantum_Simulators)/runtimes/win-x64/native/Microsoft.Quantum.Simulator.Runtime.dll</SimulatorRuntime>
```

Note the variable `$(PkgMicrosoft_Quantum_Qir_Runtime)` for example is only available because of the `GeneratePathProperty` in the `Microsoft.Quantum.Qir.Runtime` package declaration.

Since `QirRuntimeHeaders` and `QirRuntimeLibs` only specify directories (whereas `SimulatorRuntime` specifies a single file), we further filter the files to be copied:

```xml
      <_QirRuntimeLibFiles Include="$(QirRuntimeLibs)/**/*.*" Exclude="$(QirRuntimeLibs)/**/*.exe" />
      <_QirRuntimeHeaderFiles Include="$(QirRuntimeHeaders)/**/*.hpp" />
```

Only `.hpp` files from the QIR header directory will be copied, and no `.exe` files from QIR library directory.

Put together, the new `Hello.csproj` project file should look as follows:

```xml
<Project Sdk="Microsoft.Quantum.Sdk/0.18.2106148911">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <QirGeneration>true</QirGeneration>
    <BuildOutputPath>$(MSBuildThisFileDirectory)build</BuildOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Quantum.Qir.Runtime" Version="0.18.2106148911-alpha" GeneratePathProperty="true" />
    <PackageReference Include="Microsoft.Quantum.Simulators" Version="0.18.2106148911" GeneratePathProperty="true" />
  </ItemGroup>

  <Target Name="GetDependencies" AfterTargets="Build">
    <PropertyGroup>
      <QirRuntimeHeaders>$(PkgMicrosoft_Quantum_Qir_Runtime)/runtimes/any/native/include</QirRuntimeHeaders>
      <QirRuntimeLibs Condition="$([MSBuild]::IsOsPlatform('OSX'))">$(PkgMicrosoft_Quantum_Qir_Runtime)/runtimes/osx-x64/native</QirRuntimeLibs>
      <QirRuntimeLibs Condition="$([MSBuild]::IsOsPlatform('Windows'))">$(PkgMicrosoft_Quantum_Qir_Runtime)/runtimes/win-x64/native</QirRuntimeLibs>
      <QirRuntimeLibs Condition="$([MSBuild]::IsOsPlatform('Linux'))">$(PkgMicrosoft_Quantum_Qir_Runtime)/runtimes/linux-x64/native</QirRuntimeLibs>
      <SimulatorRuntime Condition="$([MSBuild]::IsOsPlatform('OSX'))">$(PkgMicrosoft_Quantum_Simulators)/runtimes/osx-x64/native/Microsoft.Quantum.Simulator.Runtime.dll</SimulatorRuntime>
      <SimulatorRuntime Condition="$([MSBuild]::IsOsPlatform('Windows'))">$(PkgMicrosoft_Quantum_Simulators)/runtimes/win-x64/native/Microsoft.Quantum.Simulator.Runtime.dll</SimulatorRuntime>
      <SimulatorRuntime Condition="$([MSBuild]::IsOsPlatform('Linux'))">$(PkgMicrosoft_Quantum_Simulators)/runtimes/linux-x64/native/Microsoft.Quantum.Simulator.Runtime.dll</SimulatorRuntime>
    </PropertyGroup>
    <ItemGroup>
      <_QirRuntimeLibFiles Include="$(QirRuntimeLibs)/**/*.*" Exclude="$(QirRuntimeLibs)/**/*.exe" />
      <_QirRuntimeHeaderFiles Include="$(QirRuntimeHeaders)/**/*.hpp" />
    </ItemGroup>
    <Copy SourceFiles="$(SimulatorRuntime)" DestinationFolder="$(BuildOutputPath)" SkipUnchangedFiles="true" />
    <Copy SourceFiles="@(_QirRuntimeLibFiles)" DestinationFolder="$(BuildOutputPath)\%(RecursiveDir)" SkipUnchangedFiles="true" />
    <Copy SourceFiles="@(_QirRuntimeHeaderFiles)" DestinationFolder="$(BuildOutputPath)\%(RecursiveDir)" SkipUnchangedFiles="true" />
  </Target>

</Project>
```

Build the project again with `dotnet build` from the project root directory.
You should see the following important files appear in a folder named `build`, among others:

```
build
├── Microsoft.Quantum.Qir.QSharp.Core.dll
├── Microsoft.Quantum.Qir.QSharp.Foundation.dll
├── Microsoft.Quantum.Qir.Runtime.dll
├── Microsoft.Quantum.Simulator.Runtime.dll
├── QirContext.hpp
├── QirRuntime.hpp
└── SimFactory.hpp
```

(**Linux**) The `Microsoft.Quantum.Qir.*` dynamic libraries will already have the right naming scheme for Clang to use, but the `Microsoft.Quantum.Simulator.Runtime` library needs to be renamed.
The proper name format is `lib<library-name>.so`.

Execute the following command from the project root directory:

```bash
mv build/Microsoft.Quantum.Simulator.Runtime.dll build/libMicrosoft.Quantum.Simulator.Runtime.so
```

### Adding a driver

Trying to compile the QIR code in `Hello.ll` as is would present some problems, as it's missing a program entry point and the proper setup of the simulator.
A small C++ driver program (`Main.cpp`) will handle the setup and invoke Q# operations or functions directly from the QIR code.

```cpp
#include "QirContext.hpp"
#include "QirRuntime.hpp"
#include "SimFactory.hpp"

using namespace Microsoft::Quantum;
using namespace std;

extern "C" void Hello__HelloQ();

int main(int argc, char* argv[]){
    unique_ptr<IRuntimeDriver> sim = CreateFullstateSimulator();
    QirContextScope qirctx(sim.get(), true /*trackAllocatedObjects*/);
    Hello__HelloQ();
    return 0;
}
```

The driver consists of the following elements:

* header files (to interface with the libraries):

  - `QirContext` : used to register the simulator with the QIR runtime
  - `QirRuntime` : implements the types and functions defined in the [QIR specification](https://github.com/qir-alliance/qir-spec)
  - `SimFactory` : provides the Q# simulator

* namespaces :

  - `Microsoft::Quantum` : the QIR context and simulator live here
  - `std` : needed for `unique_ptr`

* external function declarations :

    This is were we declare functions from other compilation units we'd like to invoke.
    In our case, that compilation unit is the generated/optimized QIR code.
    `extern "C"` is strictly required here in order for the compiler to use the given function name exactly as is ('C' style linkage).
    Normally, C++ function names would be transformed during compilation to include namespace and call argument information in the function name, known as [mangling](https://en.wikipedia.org/wiki/Name_mangling).
    We can check that the QIR function `Hello_HelloQ` indeed appears in the `Hello.ll` file with that name.

* simulator invocation:

    Here we create a Q# [full state simulator](https://docs.microsoft.com/azure/quantum/user-guide/machines/full-state-simulator) instance that will run our quantum program and register it with the current context.
    Following this, everything is set up to call into Q# functions.

### Compiling the program

Multiple tools are available for this step, such as the LLVM static compiler + assembler + linker or the JIT compiler.
Here, Clang is used again, this time to compile and link the `Hello.ll` Q# program with the driver and QIR runtime libraries.

Invoke the following command on Windows:

```powershell
clang++ qir/Hello.ll Main.cpp -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -o build/Hello.exe
```

On Linux:

```bash
clang++ qir/Hello.ll Main.cpp -Wl,-rpath=build -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -l'Microsoft.Quantum.Simulator.Runtime' -o build/Hello.exe
```

Parameters:

* `qir/Hello.ll` : source file 1, the QIR execution unit containing the Q# code
* `Main.cpp` : source file 2, the driver containing the program entry point (main)
* `-Wl,-rpath=build` : add the `build` directory as a search path for dynamic libraries at runtime, `Wl,<arg>` is used to pass arguments to the linker (Linux only)
* `-Ibuild` : find header files in the `build` directory
* `-Lbuild` : find libraries in the `build` directory
* `-l'<libname>'` : link dynamic libraries copied earlier
* `-o build/Hello.exe` : path of the generated executable, placed in the build directory so Windows can find the dynamic libraries at runtime

Running the program should now print the output `Hello quantum world!` to the terminal:

```shell
./build/Hello.exe
```

The same can be done with the optimized QIR code.

On Windows:

```powershell
clang++ qir/Hello-dce-inline.ll Main.cpp -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -o build/Hello.exe && ./build/Hello.exe
```

On Linux:

```bash
clang++ qir/Hello-dce-inline.ll Main.cpp -Wl,-rpath=build -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -l'Microsoft.Quantum.Simulator.Runtime' -o build/Hello.exe && ./build/Hello.exe
```

As a last example, let's modify the Q# program `Program.qs` with a random bit generator and run through the whole process:

```csharp
namespace Hello {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation HelloQ() : Result {
        Message("Hello quantum world!");

        use qb = Qubit();
        H(qb);
        Message("Random bit:");
        return M(qb);
    }
}
```

Steps:

* build the project `dotnet build`
* optimize the code `clang -S qir/Hello.ll -O3 -emit-llvm -o qir/Hello-o3.ll`
* compile the code on Windows
    ```powershell
    clang++ qir/Hello-o3.ll Main.cpp -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -o build/Hello.exe
    ```
    or Linux
    ```bash
    clang++ qir/Hello.ll Main.cpp -Wl,-rpath=build -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -l'Microsoft.Quantum.Simulator.Runtime' -o build/Hello.exe
    ```
* simulate the program `./build/Hello.exe`
