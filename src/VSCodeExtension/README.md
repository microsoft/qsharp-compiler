# Microsoft Quantum Development Kit for Visual Studio Code Preview

Thank you for your interest in Microsoft's Quantum Development Kit for Visual Studio Code preview. 
The Quantum Development Kit contains the tools you'll need to build your own quantum computing programs and experiments. 
Assuming some experience with Visual Studio Code, beginners can write their first quantum program, and experienced researchers can quickly and efficiently develop new quantum algorithms.

**NOTE: The simulator included with the Quantum Development Kit requires a 64-bit operating system to run.**

To jump right in, start with [Installation and validation](http://docs.microsoft.com/quantum/quantum-installconfig#vscode) to create and validate your development environment. 
Then use [Quickstart - your first computer program](http://docs.microsoft.com/quantum/quantum-WriteAQuantumProgram) to learn about the structure of a Q# project and how to write the quantum equivalent of "Hello, world!" --  a quantum teleport application.

If you'd like more general information about Microsoft's quantum computing initiative, see [Microsoft Quantum](https://www.microsoft.com/quantum/).

## Feedback pipeline
Your feedback about all parts of the Quantum Development Kit is important. Please go to [our GitHub repository](https://github.com/microsoft/qsharp-compiler) to provide feedback on the Q# compiler and language extensions, or to learn more about where to give feedback on other parts of the Quantum Development Kit.
 
## Microsoft Quantum Development Kit components
The Quantum Development Kit preview provides a complete development and simulation environment that contains the following components.
<table>
<tr><th>Component</th><th>Function</th></tr>
<tr><td>Q# language and compiler</td><td>Q# is a domain-specific programming language used for expressing quantum algorithms. It is used for writing sub-programs that execute on an adjunct quantum processor under the control of a classical host program and computer.</td></tr>
<tr><td>Q# standard library</td><td>The library contains operations and functions that support both the classical language control requirement and the Q# quantum algorithms.</td></tr>
<tr><td>Local quantum machine simulator</td><td>A full state vector simulator optimized for accurate vector simulation and speed.</td></tr>
<tr><td>Quantum computer trace simulator</td><td>The trace simulator does not simulate the quantum environment like the local quantum simulator. It is used to estimate the resources required to execute a quantum program and also allow faster debugging of the non-Q# control code.</td></tr>
<tr><td>Visual Studio Code extension</td><td>The extension contains syntax highlighting and code snippets.</td></tr>
</table>

## Quantum Development Kit documentation
The current documentation includes the following topics.
* [Quantum computing concepts](https://docs.microsoft.com/en-us/quantum/concepts/) includes topics such as the relevance of linear algebra to quantum computing, the nature and use of a qubit, how to read a quantum circuit, and more.
* [Installation and validation](https://docs.microsoft.com/en-us/quantum/install-guide/vs-code) describes how to quickly set up Visual Studio Code for quantum development.
* [Quickstart - your first quantum program](https://docs.microsoft.com/en-us/quantum/quickstart) walks you through how to create the Teleport application in Visual Studio Code. You'll learn how to define a Q# operation, call the Q# operation using C#, and how to execute your quantum algorithm.
* [Managing quantum machines and drivers](https://docs.microsoft.com/en-us/quantum/machines/) describes how quantum algorithms are executed, what quantum machines are available, and how to write a non-Q# driver for the quantum program.
* [Quantum development techniques](https://docs.microsoft.com/en-us/quantum/techniques/) specifies the core concepts used to create quantum programs in Q#. Topics include file structures, operations and functions, working with qubits, and some advanced topics.
* [Q# standard libraries](https://docs.microsoft.com/en-us/quantum/libraries/standard/) describes the operations and functions that support both the classical language control requirement and the Q# quantum algorithms. Topics include control flow, data structures, error correction, testing, and debugging. 
* [Q# language reference](https://docs.microsoft.com/en-us/quantum/language/) details the Q# language including the type model, expressions, statements, and compiler use.
* [For more information](https://docs.microsoft.com/en-us/quantum/for-more-info) contains specially selected references to deep coverage of quantum computing topics.
* [Quantum trace simulator reference](https://docs.microsoft.com/dotnet/api/Microsoft.Quantum.Simulation.Simulators.QCTraceSimulators?branch=master) contains reference material about trace simulator entities and exceptions.
* [Q# library reference](https://docs.microsoft.com/qsharp/api/) contains reference information about library entities by namespace.

## Privacy

VS Code collects usage data and sends it to Microsoft to help improve our products and services. Read our [Privacy Statement](https://go.microsoft.com/fwlink/?LinkID=528096&clcid=0x409) to learn more. If you don't wish to send usage data to Microsoft, you can set the `telemetry.enableTelemetry` setting to `false`. Learn more in our [FAQ](https://code.visualstudio.com/docs/supporting/faq#_how-to-disable-telemetry-reporting).
