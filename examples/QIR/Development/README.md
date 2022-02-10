# Development of the QIR Generation Logic

This project is intended for trouble shooting the [QIR generation](../../../src/QsCompiler/QirGeneration). The Q# project in this folder is compiled using the source code version of the Q# compiler and the QIR generation; any changes to them will be reflected in the emitted QIR.

This is *not* how the QIR generation is meant to be used if you don't intend to modify the source code for it. Please see the [Emission project](../Emission) for how to use a shipped version of the compiler and QIR generation.

Note: If you are using the Development project on Linux, you may run into the following error: `System.DllNotFoundException: Unable to load shared library 'libLLVM' or one of its dependencies.` The solution to this is to set the LD_LIBRARY_PATH environment variable to point towards the libLLVM.so libary. This can be done using the following command in bash:
```
export LD_LIBRARY_PATH=/usr/lib/llvm-11/lib
```

# Development of the QIR Debug Info Generation

## NOTE: Incompatible with Windows

This debug feature is currently incompatible with Windows due to some LLVM tool limitations. The QIR emission itself still works on Windows, meaning you can request debug symbols within Development.csproj and they will appear in the emitted QIR when you build without errors. However, when you go to actually test the Q# debugging experience on Windows, you won't be able to set breakpoints. If you use the VS Code UI for debugging, the program simply won't stop at the lines you attempt to put breakpoints on. If you use LLDB from the command line, you will get this specific message when you attempt to set breakpoints:
```
WARNING:  Unable to resolve breakpoint to any actual locations.
```

This seems to be due to a bug in LLVM within either LLDB or Clang that prevents us from having debug information point back to a ".qs" file on Windows. This is not a problem on Linux or Mac.

As a workaround, you can duplicate the Q# file into into a ".c" file and change the emitted QIR such that the source file references have extension ".c" instead of ".qs." Then, you would attach the debugger to the ".c" file.

## Emitting debug information within the QIR

To emit debug information within the QIR, uncomment the following line in `Development.csproj`.
```
<_QscCommandPredefinedAssemblyProperties>$(_QscCommandPredefinedAssemblyProperties) EnableDebugSymbols:"true"</_QscCommandPredefinedAssemblyProperties>
```

## Testing the Q# debugging experience
To test the Q# debugging experience, you can use `lldb` from the command line, but we recommend using the built in VS Code debugging GUI for a better experience. Use the following steps to set this up:
* Install the (CodeLLDB extension)[https://marketplace.visualstudio.com/items?itemName=vadimcn.vscode-lldb].
* Open or create a launch.json file within the subdirectory `.vscode` from the base of this repository. Add the following configuration.
```
        {
            "name": "Debug Program.qs",
            "type": "lldb",
            "request": "launch",
            "program": "${workspaceFolder}/examples/QIR/Development/build/Development",
            "args": [],
        }
```
* Make sure that you have the following set in VSCode to allow breakpoints in Q# files: VSCode > Settings > Breakpoints > Allow Breakpoints in Any File

Each time you want to test the Q# debugging experience based on the local version of the compiler, take the following steps:
* From the Development directory, run `dotnet build`. This will emit the QIR.
* Set breakpoints in `Program.qs`.
* Launch the "Debug Program.qs" configuration from the VS Code debugger
