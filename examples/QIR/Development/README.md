# Development of the QIR Generation Logic

This project is intended for trouble shooting the [QIR generation](../../../src/QsCompiler/QirGeneration). The Q# project in this folder is compiled using the source code version of the Q# compiler and the QIR generation; any changes to them will be reflected in the emitted QIR.

This is *not* how the QIR generation is meant to be used if you don't intend to modify the source code for it. Please see the [Emission project](../Emission) for how to use a shipped version of the compiler and QIR generation.

Note: If you are using the Development project on Linux, you may run into the following error: `System.DllNotFoundException: Unable to load shared library 'libLLVM' or one of its dependencies.` The solution to this is to set the LD_LIBRARY_PATH environment variable to point towards the libLLVM.so libary. This can be done using the following command in bash:
```
export LD_LIBRARY_PATH=/usr/lib/llvm-11/lib
```

# Development of the QIR Debug Info Generation
To emit debug information within the QIR, uncomment the following line in `Development.csproj`.
```
<_QscCommandPredefinedAssemblyProperties>$(_QscCommandPredefinedAssemblyProperties) DebugSymbolsEnabled:"true"</_QscCommandPredefinedAssemblyProperties>
```

To test the Q# debugging experience, you can use `lldb` from the command line, but we recommend using the built in VS Code debugging GUI for a better experience. Use the following steps to set this up:
* Install the (CodeLLDB extension)[https://marketplace.visualstudio.com/items?itemName=vadimcn.vscode-lldb].
* Open or create a launch.json file within the subdirectory `.vscode` from the base of this repository. Add the following configuration.
```
        {
            "name": "Debug Program.qs",
            "type": "lldb",
            "request": "launch",
            "program": "${workspaceFolder}/examples/QIR/Development/DebugInfo/Development.exe",
            "args": [],
        }
```

Each time you want to test the Q# debugging experience based on the local version of the compiler, take the following steps:
* From the Development directory, run `dotnet build`. This will emit the QIR.
* From the DebugInfo subdirectory, run
```
clang++-11 -g -O0 qir_driver_simple.cpp ../qir/Development.ll -I. -L. -l:libMicrosoft.Quantum.Qir.Runtime.so -l:libMicrosoft.Quantum.Qir.QSharp.Core.so -l:libMicrosoft.Quantum.Qir.QSharp.Foundation.so -o Development.exe
```
This will compile a simple C++ driver and the emitted QIR representing `Program.qs` into the executable `Development.exe`.
* Set breakpoints in `Program.qs`.
* Launch the "Debug Program.qs" configuration

RyanTODO: explain how to update libraries or find a better way to connect them
