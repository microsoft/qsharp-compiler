
# LlvmBindings Standalone port

This is a port of a small subset of Llvm.NET (originally from [UbiquityDotNET/Llvm.NET](https://github.com/UbiquityDotNET/Llvm.NET)). This port uses [LLVMSharp](https://github.com/microsoft/LLVMSharp) as the backend for wrapped calls into the LLVM-C API instead of Ubiquity.NET.LibLlvm native wrapper. Much of the functionality of Llvm.NET has been stripped out to accomodate this. This project is not intended as a standalone library or for use anywhere outside of the QIR generation component of the Q# compiler.

Please use [UbiquityDotNET/Llvm.NET](https://github.com/UbiquityDotNET/Llvm.NET) for the official release and to make any contributions.

See NOTICE.txt for license information and full list of dependencies.