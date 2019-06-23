# Welcome to the Microsoft Quantum Development Kit preview #

Thank you for your interest in Microsoft's Quantum Development Kit preview. The development kit contains the tools you'll need to build your own quantum computing programs and experiments. Assuming some experience with Microsoft Visual Studio, beginners can write their first quantum program, and experienced researchers can quickly and efficiently develop new quantum algorithms.

**NOTE: The simulator included with the Quantum Development Kit requires a 64-bit installation of Microsoft Windows to run.**

To jump right in, start with [Installation and validation](http://docs.microsoft.com/quantum/quantum-installconfig) to create and validate your development environment. Then use [Quickstart - your first computer program](http://docs.microsoft.com/quantum/quantum-WriteAQuantumProgram) to learn about the structure of a Q# project and how to write the quantum equivalent of "Hello, world!" --  a quantum teleport application.

If you'd like more general information about Microsoft's quantum computing initiative, see [Microsoft Quantum](https://www.microsoft.com/quantum/).

## Release notes for January 18, 2018, version 0.1.1801.1707
This release fixes some issues reported by the community:

- The simulator now works with older CPUs that do not support AVX. It will use AVX if the CPU supports it.
- Regional decimal settings will not cause the Q# compiler to fail.
- The `SignD` primitive operation has been changed to return `Int` rather than `Double`.

## Feedback pipeline
Your feedback about all parts of the Quantum Development Kit is important. We ask you to provide feedback by joining our community of developers at [Microsoft Quantum - Feedback](https://quantum.uservoice.com/). Sign in and share your experience in one of the following forums.

- Q# Language
- Debugging and Simulation
- Samples and Documentation
- Libraries
- Setup and IDE Integration
- General Ideas and Feature Requests

You will need a [Microsoft Account](https://signup.live.com/) to provide feedback.
 
## Microsoft Quantum Development Kit components
The Quantum Development Kit preview provides a complete development and simulation environment that contains the following components.
<table>
<tr><th>Component</th><th>Function</th></tr>
<tr><td>Q# language and compiler</td><td>Q# is a domain-specific programming language used for expressing quantum algorithms. It is used for writing sub-programs that execute on an adjunct quantum processor under the control of a classical host program and computer.</td></tr>
<tr><td>Q# standard library</td><td>The library contains operations and functions that support both the classical language control requirement and the Q# quantum algorithms.</td></tr>
<tr><td>Local quantum machine simulator</td><td>A full state vector simulator optimized for accurate vector simulation and speed.</td></tr>
<tr><td>Quantum computer trace simulator</td><td>The trace simulator does not simulate the quantum environment like the local quantum simulator. It is used to estimate the resources required to execute a quantum program and also allow faster debugging of the non-Q# control code.</td></tr>
<tr><td>Visual Studio extension</td><td>The extension contains templates for Q# files and projects as well as syntax highlighting. The extension also installs and creates automatic hooks to the compiler.</td></tr>
</table>

## Quantum Development Kit documentation
The current documentation includes the following topics.
* [Quantum computing concepts](http://docs.microsoft.com/quantum/quantum-concepts-1-Intro) includes topics such as the relevance of linear algebra to quantum computing, the nature and use of a qubit, how to read a quantum circuit, and more.
* [Installation and validation](http://docs.microsoft.com/quantum/quantum-InstallConfig) describes how to quickly set up your quantum development environment. Your Visual Studio environment will be enhanced with a compiler for the Q# language and templates for Q# projects and files.
* [Quickstart- your first quantum program](http://docs.microsoft.com/quantum/quantum-WriteAQuantumProgram) walks you through how to create the Teleport application in the Visual Studio development environment. You'll learn how to define a Q# operation, call the Q# operation using C#, and how to execute your quantum algorithm.
* [Managing quantum machines and drivers](http://docs.microsoft.com/quantum/quantum-SimulatorsAndMachines) describes how quantum algorithms are executed, what quantum machines are available, and how to write a non-Q# driver for the quantum program.
* [Quantum development techniques](http://docs.microsoft.com/quantum/quantum-devguide-1-Intro) specifies the core concepts used to create quantum programs in Q#. Topics include file structures, operations and functions, working with qubits, and some advanced topics.
* [Q# standard libraries](http://docs.microsoft.com/quantum/libraries/intro) describes the operations and functions that support both the classical language control requirement and the Q# quantum algorithms. Topics include control flow, data structures, error correction, testing, and debugging. 
* [Q# language reference](http://docs.microsoft.com/quantum/quantum-QR-Intro) details the Q# language including the type model, expressions, statements, and compiler use.
* [For more information](http://docs.microsoft.com/quantum/quantum-ForMoreInfo) contains specially selected references to deep coverage of quantum computing topics.
* [Quantum trace simulator reference](https://docs.microsoft.com/dotnet/api/Microsoft.Quantum.Simulation.Simulators.QCTraceSimulators?branch=master) contains reference material about trace simulator entities and exceptions.
* [Q# library reference](http://docs.microsoft.com/qsharp/api/) contains reference information about library entities by namespace.

