# Q# Command Line Compiler #

The Q# compiler provides a [command line interface](./tree/master/src/QsCompiler/CommandLineTool) with the option to specify any dotnet executable as target. 
The specified target(s) will be invoked with the path where the compilation output has been generated. 
The corresponding syntax tree can be reconstructed leveraging the routines provided in the CompilationLoader class available as part of the [Microsoft.Quantum.Compiler NuGet package](https://www.nuget.org/packages/Microsoft.Quantum.Compiler). 

