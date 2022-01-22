# Building a Simulator for QIR

This example discusses the structure of the QIR Runtime system and how to attach a simulator to it using two sample simulators implemented "from scratch":

- a state-less [trace simulator](TraceSimulator): prints each quantum instructions it receives, useful for debugging or simple hardware backend hookup
- a full state [quantum simulator](StateSimulator): simulates ideal quantum computer, inefficient but simple implementation that maps directly to mathematical description

The file `SimulatorTemplate.cpp` in this directory is also good starting point for a custom simulator implementation, as it provides a template that just needs to be filled in with the bodies of required methods.

## Understanding the QIR Runtime system

The [QIR Runtime](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime) implements the [QIR language specification](https://github.com/qir-alliance/qir-spec) in LLVM.
A quantum program written directly in QIR (or compiled into QIR from Q# e.g.) will expect specific runtime functions to be available, which can include data type management or console output, but also qubit allocation, callable creation, and functor application, such as these QIR/LLVM functions below:

```llvm
%String* __quantum__rt__string_create(i8*)
void __quantum__rt__message(%String*)
%Qubit* __quantum__rt__qubit_allocate()
void __quantum__rt__callable_invoke(%Callable*, %Tuple*, %Tuple*)
void __quantum__rt__callable_make_adjoint(%Callable*)
```

Some are directly implemented by the Runtime, such as `__quantum__rt__message` or `__quantum__rt__string_create`, while others will require a call to the backend system, such as `__quantum__rt__qubit_allocate`.
The backend system could be a software simulator or hardware runtime environment.
The focus in this guide will be on how the QIR Runtime interfaces with a simulator.

Note that a quantum instruction set is not part of the QIR spec, nevertheless the QIR Runtime implements the instruction set expected by Q#, which defines LLVM functions such as:

```llvm
void @__quantum__qis__h__body(%Qubit*)
void @__quantum__qis__t__adj(%Qubit*)
void @__quantum__qis__z__ctl(%Array*, %Qubit*)
%Result* @__quantum__qis__measure__body(%Array*, %Array*)
```

In order to communicate with the QIR Runtime, hardware backends or simulators can implement the following interfaces defined by the Runtime:

- the driver `IRuntimeDriver` : this is critical for the Runtime to operate, implementing qubit allocation and other backend-specific functionality
- the gate set `IQuantumGateSet` : only used by those backends that want to provide the Q# instruction set
- the diagnostics `IDiagnostics` : optional but helpful component to provide debug information of the backend state

The first component of the Runtime stack is the QIR Bridge at [public/QirRuntime.hpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/public/QirRuntime.hpp) which declares the functions for the runtime as `extern "C"`.
Different parts of the QIR spec are then implemented in C++ in the [lib/QIR](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime/lib/QIR) folder.
For example, the `__quantum__rt__string_create` function resides in [lib/QIR/strings.cpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QIR/strings.cpp).
For functions defined in [lib/QIR/delegated.cpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QIR/delegated.cpp) such as `__quantum__rt__qubit_allocate`, the Runtime eventually calls the implementation `IRuntimeDriver::Allocate` provided by the backend.

Other components are provided for Q# programs compiled to QIR in [lib/QSharpCore](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime/lib/QSharpCore) and [lib/QSharpFoundation](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime/lib/QSharpFoundation).
In particular, the `QSharpCore` component provides the Q# instruction set.
As above, a bridge [lib/QSharpCore/qsharp__core__qis.hpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QSharpCore/qsharp__core__qis.hpp) first declares a function such as `__quantum__qis__h__body` as `extern "C"`, which is then implemented in [lib/QSharpCore/intrinsics.cpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QSharpCore/intrinsics.cpp), which then calls the specific implementation `IQuantumGateSet::H` provided by the backend.

A qubit manager (see next section) is also provided in [lib/QIR/QubitManager.cpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/lib/QIR/QubitManager.cpp).

## Qubit Management

Generally, we can distinguish three layers of qubit representation or abstractions.
Two of those are frequently encountered in quantum computing literature, the *logical qubit* and *the physical* qubit.

A logical qubit is an idealized entity that represents the Qubit from quantum computation theory: a perfect two-level system that behaves as expected when applying quantum gates or measurements.
The physical qubit on the other hand is a real physical system that *approximately* behaves as a two-level system, and is imperfectly manipulated and isolated from the environment, leading to a limited reliability and information life-time.

There are various techniques to create logical qubits from physical qubits via the use of error correction schemes and fault-tolerant systems.
Whether the logical qubit is implemented by 1 or 1000 physical qubits, which type of physical system is used, or even whether one logical qubit remains on the same physical qubits throughout the entire computation, is not relevant at the logical level and is thus abstracted away.
Quantum computing algorithms are typically formulated at the logical level unless they specifically deal with error correction or are designed to run on near-term devices.
Providing logical qubits is the task of the hardware-level runtime environment.

In practical implementations, it is worth considering an additional layer on top of logical qubits, namely that of *virtual qubits* or *program qubits*.

The concept is somewhat analogous to that of a virtual address space in classical computing, in which the same address space is shared or reused by multiple programs, even though it appears to the program as if the entire address space is available to it.
Similarly, it's possible to present a virtual qubit space to a program that is larger than the number of logical qubits available, whether via the reuse of previously deallocated qubits, or borrowing a qubit from a different program if it is known to be safe to do so.
Further, the number of available logical qubits may change throughout the operation of a quantum computer (e.g. due to certain physical qubits becoming unusable), which needs to be handled.
Deciding which logical qubit to assign to which virtual qubit, and when and how to reuse logical qubits, is the task of the *qubit manager*.

Note: Instead of implementing your own qubit manager you should use the one provided by the Runtime at [public/QubitManager.hpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/public/QubitManager.hpp).

## Structure of a Simulator

As mentioned further up, the QIR Runtime provides three interfaces to connect backends to the Runtime:

- `IRuntimeDriver` : Basic runtime functions such as qubit and measurement result management.
- `IQuantumGateSet` : The Q# instruction set. Implementation of this interface is not strictly required, as long as some instruction set is implemented, and the QIR code only calls instructions from that set (may necessitate the use of a bridge, see the [QIR Bridge](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime#qir-bridge-and-runtime) and [previous section](#understanding-the-qir-runtime-system)).
- `IDiagnostics` : Optional interface to provide insight into the state of a simulator or hardware backend (useful for debugging).

Let's have a look at the functionality each interface provides.

---

`IRuntimeDriver`

```cpp
// Qubit management
virtual Qubit AllocateQubit() = 0;
virtual void ReleaseQubit(Qubit qubit) = 0;
virtual std::string QubitToString(Qubit qubit) = 0;

// Result management
virtual void ReleaseResult(Result result) = 0;
virtual bool AreEqualResults(Result r1, Result r2) = 0;
virtual ResultValue GetResultValue(Result result) = 0;
virtual Result UseZero() = 0;
virtual Result UseOne() = 0;
```

---

`IQuantumGateSet`

```cpp
// Elementary operations
virtual void X(Qubit target) = 0;
virtual void Y(Qubit target) = 0;
virtual void Z(Qubit target) = 0;
virtual void H(Qubit target) = 0;
virtual void S(Qubit target) = 0;
virtual void T(Qubit target) = 0;
virtual void R(PauliId axis, Qubit target, double theta) = 0;
virtual void Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta) = 0;

// Multi-controlled operations
virtual void ControlledX(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledY(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledZ(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledH(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledS(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledT(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledR(long numControls, Qubit controls[], PauliId axis, Qubit target, double theta) = 0;
virtual void ControlledExp(long numControls, Qubit controls[], long numTargets, PauliId paulis[], Qubit targets[], double theta) = 0;

// Adjoint operations
virtual void AdjointS(Qubit target) = 0;
virtual void AdjointT(Qubit target) = 0;
virtual void ControlledAdjointS(long numControls, Qubit controls[], Qubit target) = 0;
virtual void ControlledAdjointT(long numControls, Qubit controls[], Qubit target) = 0;

// Measurement operations
virtual Result Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[]) = 0;
```

---

`IDiagnostics`

```cpp
// Internal state
virtual void GetState(TGetStateCallback callback) = 0; // deprecated
virtual void DumpMachine(const void* location) = 0;
virtual void DumpRegister(const void* location, const QirArray* qubits) = 0;

// Measurement outcomes
virtual bool Assert(long numTargets, PauliId bases[], Qubit targets[], Result result, const char* failureMessage) = 0;
virtual bool AssertProbability(long numTargets, PauliId bases[], Qubit targets[], double probabilityOfZero, double precision, const char* failureMessage) = 0;
```

---

The requirements on a simulator are then:

- Qubit management, which can include:
  - virtual qubits
  - logical qubits
  - physical qubits

  Generally, the physical qubit layer is not present in simulators, as they typically directly implement logical qubits, unless the simulator is built to simulate noise in real quantum computers.
  While it is useful to handle qubit management in a separate component, some integration between the qubit manager and backend can be beneficial depending on the application.

- Result management:

  Generally trivial for simulators.

- Define supported quantum operations:

  Here, we'll consider the instruction set provided by the QIR runtime for the simulation of Q# programs (`IQuantumGateSet`).

- (optional) Represent quantum state, e.g.:
  - Boolean for classical circuits
  - 2^n complex vector

- (optional) Update internal state for each instruction

- Return simulation results, e.g.:
  - simulated measurement results
  - gate counts

- (optional) Provide diagnostics information, e.g.:
  - look at internal state representation
  - compute measurement distributions

## TraceSimulator

See [TraceSimulator](TraceSimulator).

## StateSimulator

See [StateSimulator](StateSimulator).
