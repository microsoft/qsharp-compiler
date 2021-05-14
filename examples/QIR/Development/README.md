# Q# - Preview feature: Compilation to QIR

This project has been modified to serve as a place to experiment with the [QIR generation](../../../src/QsCompiler/QirGeneration) capabilities of the Q# compiler as part of [Unitary Hack](https://github.com/unitaryfund/unitaryhack). 

The Q# project in this folder is compiled using the source code version of the Q# compiler and the QIR generation; any changes to them will be reflected in the emitted QIR.

## Unitary Hack

We are working on adding support for compiling Q# to QIR and executing it. 
QIR is a convention for how to represent quantum programs in LLVM. 
We aim to ultimately move the Q# compiler to be fully LLVM-based.

While the support and integration is not yet complete, we have set up this example for how to compile a Q# project to QIR and execute it on our full state simulator for early adventurers who are excited to give it a try!

We have defined two task for the Unitary Hack, both of which can be done using this project:

- Create a Q# program that is significantly different than any of our samples or other programs posted for unitary hack, compile it to QIR, execute it using our full state simulator. Post a short clip showing the execution and the generated QIR in the GitHub issue. 
      
- Find a Q# program that doesn't compile correctly into QIR or unexpectedly fails when executing the QIR on the full state simulator due to an issue with the generated QIR that hasn't been filed yet, and file the issue. 

See the descriptions on the corresponding GitHub issues marked with #unitaryhack for more details on each task.

## Found Issues
