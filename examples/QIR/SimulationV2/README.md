# Building a Simulator for QIR

```shell
clang++ -shared RuntimeManagement.cpp Simulation.cpp -o SimpleSimulator.dll -Iinclude -Llib -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core'
```

## Overview

### The QIR Runtime system

The [QIR Runtime](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime) implements the [QIR language specification](https://github.com/microsoft/qsharp-language/tree/main/Specifications/QIR) in LLVM.
A quantum program written in QIR (or translated from Q# e.g.) will expect specific runtime functions to be available, which can include data type management or console output, but also qubit allocation, callable creation, and functor application:

```llvm
%String* __quantum__rt__string_create(i8*)
void __quantum__rt__message(%String*)
%Qubit* __quantum__rt__qubit_allocate()
void __quantum__rt__callable_invoke(%Callable*, %Tuple*, %Tuple*)
void __quantum__rt__callable_make_adjoint(%Callable*)
```

Some can be implemented directly by the Runtime, such as `__quantum__rt__message` or `__quantum__rt__string_create`, while others will require a call to the backend system, such as `__quantum__rt__qubit_allocate`.
The backend system could be a software simulator or hardware runtime environment.
The focus in this guide will be on how the QIR runtime interfaces with a simulator.

Note that a quantum instruction set is not part of QIR spec, nevertheless the QIR Runtime implements the instruction set expected by Q#, such as:

```llvm
void @__quantum__qis__h__body(%Qubit*)
void @__quantum__qis__h__ctl(%Array*, %Qubit*)
void @__quantum__qis__z__body(%Qubit*)
void @__quantum__qis__z__ctl(%Array*, %Qubit*)
%Result* @__quantum__qis__measure__body(%Array*, %Array*)
```

In order to communicate with the QIR Runtime, hardware backends or simulators can implement the following interfaces defined by the Runtime:

- the driver `IRuntimeDriver` : this is critical for the Runtime to operate, implementing qubit allocation and other backend-specific functionality
- the gate set `IQuantumGateSet`: only used by those backends that want to provide the Q# instruction set
- the diagnostics `IDiagnostics` : optional but helpful component to provide debug information of the backend state

The first component of the Runtime is the QIR Bridge located at [lib/QIR/bridge-rt.ll](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QIR/bridge-rt.ll) which translates between QIR and the Runtime implementation.
Different parts of the QIR spec are then implemented in C++ in the [lib/QIR](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime/lib/QIR) folder.
For example, the `__quantum__rt__string_create` QIR function is translated to the C++ `quantum__rt__string_create` function which resides in [lib/QIR/strings.cpp].
For others such as `__quantum__rt__qubit_allocate`, the Runtime will eventually call the implementation provided by the driver.

Other components that are used by QIR simulators for Q# are provided by `lib/QSharpCore` and `lib/QSharpFoundation`.
In particular, the `QSharpCore` component provides the Q# instruction set.
Similar to above, a bridge [lib/QSharpCore/qsharp-core-qis.ll](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QSharpCore/qsharp-core-qis.ll) first translates a function such as `__quantum__qis__h__body` to `quantum__qis__h__body` located in [lib/QSharpCore/intrinsics.cpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QSharpCore/intrinsics.cpp), which then calls upon the specific implementation provided by the gate set interface.

A qubit manager (see next section) is also provided in [lib/QIR/QubitManager.cpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QIR/QubitManager.cpp).


## Qubit Management

Generally, we can distinguish three layers of qubit representation or abstractions.
Two of those are frequently encountered in quantum computing literature, the *logical qubit* and *the physical* qubit.

A logical qubit is an idealized entity that represents the Qubit from quantum computation theory: a perfect two-level system that behaves as expected when applying quantum gates or measurements.
The physical qubit on the other hand is a real physical system that *approximately* behaves as a two-level system, and is imperfectly manipulated and isolated from the environment, leading to a limited reliability and information life-time.

There are various techniques to create logical qubits from physical qubits via the use of error correction schemes and fault-tolerant systems.
Whether the logical qubit is implemented by 1 or 1000 physical qubits, which type of physical system is used, or even whether one logical qubit remains on the same physical qubits throughout the entire computation, is not relevant at the logical level and is thus abstracted away.
Quantum computing algorithms are typically formulated at the logical level unless they specifically deal with error correction or other aspects of physical qubits.
Providing logical qubits is the task of the hardware-level runtime environment.

In practical implementations, it is worth considering an additional layer on top of logical qubits, namely that of *virtual qubits* or *program qubits*.

The concept is somewhat analogous to that of a virtual address space in classical computing, in which the same address space is shared or reused by multiple programs, even though it appears to the program as if the entire address space is available to it.
Similarly, it's possible to present a virtual qubit space to a program that is larger than the number of logical qubits available, whether via the reuse of previously deallocated qubits, or borrowing a qubit from a different program if it is known to be safe to do so.
Further, the number of available logical qubits may change throughout the operation of a quantum computer (e.g. due to certain physical qubits becoming unusable), which needs to be handled.
Deciding which logical qubit to assign to which virtual qubit, and when and how to reuse logical qubits, is the task of the *qubit manager*.


## Structure of the Simulator

components of the simulator:

- qubit management:
  - virtual qubits
  - logical qubits
  - physical qubits

In a simulator, the physical qubit layer is not present as it most often directly implements logical qubits, unless the simulator is built to simulate noise in real quantum computers.
The virtual layer is handled by a separate QubitManager component.

In order to interface with the QubitManager, the SimpleSimulator provides an `UpdateState` function 

- representing quantum state (e.g. bool in classical circuits, 2^n complex vector, ...)
- updating state
- measuring state
- define supported quantum operations

Here, we use the instruction set provided by the QIR runtime for the simulation of Q# programs.


Optional components:

- diagnostics:
  - look at internal state representation
  - compute measurement distributions
