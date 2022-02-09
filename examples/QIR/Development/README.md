# Development of the QIR Generation Logic

This project is intended for trouble shooting the [QIR generation](../../../src/QsCompiler/QirGeneration). The Q# project in this folder is compiled using the source code version of the Q# compiler and the QIR generation; any changes to them will be reflected in the emitted QIR.

This is *not* how the QIR generation is meant to be used if you don't intend to modify the source code for it. Please see the [Emission project](../Emission) for how to use a shipped version of the compiler and QIR generation.

Note: If you are using the Development project on Linux, you may run into the following error: `System.DllNotFoundException: Unable to load shared library 'libLLVM' or one of its dependencies.` The solution to this is to set the LD_LIBRARY_PATH environment variable to point towards the libLLVM.so libary. This can be done using the following command in bash:
```
export LD_LIBRARY_PATH=/usr/lib/llvm-11/lib
```
