# QIR Emission - Preview Feature

This file contains directions for using the preview feature integrated into the Q# compiler to emit QIR.
QIR is a convention for how to represent quantum programs in LLVM. Its specification can be found [here](https://github.com/qir-alliance/qir-spec#quantum-intermediate-representation-qir).
We aim to ultimately move the Q# compiler to be fully LLVM-based. While the emission is supported starting with the March 2021 release, it is as of this time not yet connected to the runtime. The QIR runtime and instructions for how to execute the emitted QIR can be found [here](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime#the-native-qir-runtime). We are working on a full integration in the future.

## Using the Q# Compiler to Emit QIR

QIR can be emitted for any Q# project as long as its output type is an executable.
To enable QIR emission, open the project file in a text editor and add the following project property:
```
<QirGeneration>true</QirGeneration>
```
If the project builds successfully, the .ll file containing QIR can be found in `qir` folder in the project folder. Alternatively, the folder path can be specified via the `QirOutputPath` project property. The project file should look similar to this:
```
<Project Sdk="Microsoft.Quantum.Sdk/0.20.2111176351-beta">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <QirOutputPath>$(MSBuildThisFileDirectory)/qir</QirOutputPath>
    <QscVerbosity>Detailed</QscVerbosity>
  </PropertyGroup>
</Project>
```
For more information about project properties and other Sdk capabilities, see [here](../../../src/QuantumSdk#the-microsoftquantumsdk-nuget-package). Examples for working with QIR, and specifically a project with QIR emission enabled can be found [here](../../../examples/QIR).

## Limitations

Please be aware that as of this time, it is not possible to both QIR and C#.
Open issues related to QIR emission can be found [here](https://github.com/microsoft/qsharp-compiler/issues?q=is%3Aopen+is%3Aissue+label%3A%22area%3A+QIR%22).
The emitted QIR does currently not contain any debug information. We are planning to add better support in the future. If you are interested in contributing, please indicate your interest on [this issue](https://github.com/microsoft/qsharp-compiler/issues/637).

### Entry Points

For Q# operation decorated with the `@EntryPoint` attribute, the QIR generation
will create an additional C-callable wrapper function.
The name of this wrapper is the same as the full, namespace-qualified name
of the entry point, with periods replaced by double underscores.
The entry point wrapper function gets tagged with an custom LLVM "EntryPoint" attribute.

The entry point wrapper performs some translation between standard
C types and QIR types.

| QIR type | C-compatible LLVM type |
| --- | --- |
| `%Tuple*` | a pointer to the LLVM struct that corresponds to the fully typed Q# tuple |
| `%Array*` | `{i64, i8*}*` |
| `%BigInt*` | `{i64, i8*}*` |
| `%String*` | `i8*` |
| `%Result*` | `i8` |
| `%Range*` | `{i64, i64, i64}*` |
| `%Int` | `i64` |
| `%Double` | `double` |
| `%Bool` | `i8` |
| `%Pauli` | `i8` |

The QIR generator does not currently create a `main(argc, argv)` that translates
string values to QIR types that would allow QIR compiled to executable through clang
to be executed from the command line.

## QIR Specification

The QIR specification is on the [Q# language repository](https://github.com/qir-alliance/qir-spec).

