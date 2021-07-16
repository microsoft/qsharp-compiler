# QIR Generation and Optimization

Since QIR is a specification of LLVM IR, the IR code can be manipulated via the usual tools provided by LLVM, such as [Clang](https://clang.llvm.org/), the [LLVM optimizer](https://llvm.org/docs/CommandGuide/opt.html), the [LLVM static compiler](https://llvm.org/docs/CommandGuide/llc.html), and the [just-in-time (JIT) compiler](https://llvm.org/docs/CommandGuide/lli.html).
This example is structured as a walk-through of the process covering installation, QIR generation, and running built-in and custom LLVM optimization pipelines.

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
* (Linux only) Ensure the [GNU OpenMP support library](https://gcc.gnu.org/onlinedocs/libgomp/) is installed on your system, e.g. via `sudo apt install libgomp1`

### Installing Clang

Depending on your platform, some LLVM tools may only be available by compiling from source.
*Clang* however should be readily available from package managers or as pre-compiled binaries on most systems.
LLVM's [official release page](https://releases.llvm.org/download.html) provides sources and binaries of LLVM and Clang.
It's recommended to use LLVM version 11.1.0, as this is the version used by the Q# compiler. 

Here are some suggestions on how to obtain Clang for different platforms.
On Windows, the conda method is recommended since it's also able to install the LLVM optimizer (see next section).

Package managers:

* **Ubuntu** : `sudo apt install clang-11`
* **Windows** : `choco install llvm --version=11.1.0` using [chocolatey](https://chocolatey.org/)
* **macOS** : `brew install llvm@11` using [homebrew](https://brew.sh/)

* **all** : `conda install -c conda-forge clang=11.1.0 clangxx=11.1.0 llvm=11.1.0` using [conda](https://conda.io/projects/conda/en/latest/user-guide/install/index.html)

Pre-built binaries/installers:

* **Ubuntu** : get `clang+llvm-11.1.0-x86_64-linux-gnu-ubuntu-20.10.tar.xz` from the [GitHub release](https://github.com/llvm/llvm-project/releases/tag/llvmorg-11.1.0)
* **Windows** : get `LLVM-11.1.0-win64.exe` from the [GitHub release](https://github.com/llvm/llvm-project/releases/tag/llvmorg-11.1.0)
* **macOS** : get `clang+llvm-11.0.0-x86_64-apple-darwin.tar.xz` from the [11.0.0 release](https://github.com/llvm/llvm-project/releases/tag/llvmorg-11.0.0) (11.1.0 not released)


On Linux, if installing via `apt`, the clang/llvm commands will have `-11` attached to their name.
It's convenient to define aliases for these commands so as not to have to type out the full name every time.
If you want to skip this step, substitute `clang`/`clang++`/`opt` with `clang-11`/`clang++-11`/`opt-11` throughout the rest of this document.

```bash
echo 'alias clang=clang-11' >> ~/.bashrc
echo 'alias clang++=clang++-11' >> ~/.bashrc
echo 'alias clang=opt-11' >> ~/.bashrc
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
    operation SayHello() : Unit {
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
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>

</Project>
```

Enabling QIR generation is a simple matter of adding the `<QirGeneration>` property to the project file:

```xml
<Project Sdk="Microsoft.Quantum.Sdk/0.18.2106148911">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <QirGeneration>true</QirGeneration>
  </PropertyGroup>

</Project>
```

### Building the project

Build the project by running `dotnet build` from the project root folder, or specify the path manually as `dotnet build path/to/Hello.csproj`.
In addition to building the (simulator) executable, the compiler will also create a `qir` folder with an LLVM representation of the program (`Hello.ll`).

For small projects, such as the default hello world program, a lot of the generated QIR code may not actually be required to run the program.
The next section describes how to run optimization passes on QIR, which can strip away unnecessary code or perform various other transformations written for LLVM.

## Optimizing QIR

While Clang is typically used to compile and optimize e.g. C code, it can also be used to manipulate LLVM IR directly.
The command below tells Clang to run a series of optimizations on the generated QIR code `Hello.ll` and output back LLVM IR:

```shell
clang -S Hello.ll -O3 -emit-llvm -o Hello-o3.ll
```

Parameters (see also the [clang man page](https://clang.llvm.org/docs/CommandGuide/clang.html)):

* `-S` : turn off assembly and linking stages (since we want to stay at the IR level)
* `Hello.ll` : the input file (in this case human-readable LLVM IR)
* `-O3` : LLVM optimization level, ranging from O0 to O3 (among others)
* `-emit-llvm` : output LLVM IR instead of assembly code
* `-o Hello-o3.ll` : the output file

The resulting QIR code is now significantly smaller, only containing the declarations and definitions used by the hello world program:

```llvm
%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

declare %String* @__quantum__rt__string_create(i8*) local_unnamed_addr

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

define void @Hello__SayHello__Interop() local_unnamed_addr #0 {
entry:
  %0 = tail call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  tail call void @__quantum__rt__message(%String* %0)
  tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

define void @Hello__SayHello() local_unnamed_addr #1 {
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
opt -S Hello.ll -globaldce -o Hello-dce.ll
```

Parameters (see also the [opt man page](https://llvm.org/docs/CommandGuide/opt.html)):

* `-S` : output human-readable LLVM IR instead of bytecode
* `Hello.ll` : the input file
* `-globaldce` : global DCE pass, removes unused definitions/declarations
* `-o Hello-dce.ll` : the output file (stdout if omitted)

This produces the following code:

```llvm
%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

define internal void @Hello__SayHello__body() {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare %String* @__quantum__rt__string_create(i8*)

declare void @__quantum__rt__message(%String*)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

define void @Hello__SayHello__Interop() #0 {
entry:
  call void @Hello__HelloQ__body()
  ret void
}

define void @Hello__SayHello() #1 {
entry:
  call void @Hello__HelloQ__body()
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
```

Note that compared to the output from `-O3`, the function `Hello__SayHello__body` was not inlined, as well as some other small differences such as missing tail calls.
Add *function inlining* with the following pass:

```shell
opt -S Hello.ll -globaldce -inline -o Hello-dce.ll
```

Which produces:

```llvm
%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

declare %String* @__quantum__rt__string_create(i8*)

declare void @__quantum__rt__message(%String*)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

define void @Hello__SayHello__Interop() #0 {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

define void @Hello__SayHello() #1 {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
```

Check out the full list of [LLVM passes](https://llvm.org/docs/Passes.html) for other optimizations.
