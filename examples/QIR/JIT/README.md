
# QIR JIT Example

This project shows a simple example for how to use LLVM JIT to compile QIR bitcode and execute it from within 
a dotnet project. Note that you must have compiled `examples/QIR/Development` with the `Add` operation 
before compiling this example. It will load the bitcode produced by QirGeneration, look for the 
`Microsoft__Quantum__Qir__Emission__Add__body` function, and invoke it to add two numbers together.

This works without linking to the QIR runtime because integer addition is performed using native LLVM
instructions and does not try to invoke any runtime functions or quantum instructions.

Mechanisms for JIT of runtime dependent functions is TBD.
