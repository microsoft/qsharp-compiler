# Building a Simulator for QIR

## Understanding the QIR Runtime system

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
// Elementary operatons
virtual void X(Qubit target) = 0;
virtual void Y(Qubit target) = 0;
virtual void Z(Qubit target) = 0;
virtual void H(Qubit target) = 0;
virtual void S(Qubit target) = 0;
virtual void T(Qubit target) = 0;
virtual void R(PauliId axis, Qubit target, double theta) = 0;
virtual void Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta) = 0;

// Multicontrolled operations
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

This example provides two "from scratch" sample implementations of a simulator:

- a state-less [trace simulator](#the-trace-simulator): prints each quantum instructions it receives, useful for debugging or simple hardware backend hookup
- a full state [quantum simulator](#the-state-simulator): simulates ideal quantum computer, inefficient but simple implementation that maps directly to mathematical theory

To cleanly separate out different functionalities, the following file structure is used for the sample simulators:

- `<Type>Simulator.hpp` : Declares the simulator class, including required internal data and functions, as well as interface functions.
- `QubitManager.hpp` : Simple qubit manager implementations to be used by the simulators.
- `RuntimeManagement.cpp` : Implementation of all simulator functionality related to the `IRuntimeDriver` interface.
- `<Type>Simulation.cpp` : Implementation of all simulator functionality related to the `IQuantumGateSet` interface.

### The Trace Simulator

A trace simulator is a fast and simple way to hook up a backend to the QIR Runtime system.
It's function is to print all quantum instructions it receives.
While this can useful for debugging, it is also a simple way to connect QIR with a hardware backend by customizing the output format of the simulator to the input format of the hardware backend.

The provided sample in the `TraceSimulator` directory demonstrates how to implement a trace simulator using the structure described in the preceding section.
The main components in each file are explained below, leaving out repetitive or boiler-plate code which can be viewed in the files themselves.

---

`TraceSimulator.hpp`:

As a basic trace simulator can be implemented in a state-less fashion, the `TraceSimulator` class' only member is a qubit manager instance.
For consistency with the `StateSimulator`, we also use the same private functions to handle single-qubit and multi-controlled single-qubit gates:

```cpp
class TraceSimulator : public IRuntimeDriver, public IQuantumGateSet
{
    // Associated qubit manager instance to handle qubit representation.
    QubitManager *qbm;

    // To be called by quantum gate set operations.
    void ApplyGate(Gate gate, Qubit target);
    void ApplyControlledGate(Gate gate, long numControls, Qubit controls[], Qubit target);
```

For the trace simulator, the `Gate` type is represented by a string:

```cpp
using Gate = std::string;
```

---

`QubitManager.hpp`:

The sample qubit manager is kept simple, with the only required functionality besides allocation/deallocation being the ability convert a qubit to a printable string.
Here we simply use the internal qubit ID representation:

```cpp
std::string GetQubitName(Qubit qubit)
{
    return std::to_string(GetQubitId(qubit));
}
```

Where the qubit ID is a simple incrementing counter, and the `Qubit` object is just a pointer (as defined in [CoreTypes.hpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/public/CoreTypes.hpp)) with the qubit ID as its value:

```cpp
class QubitManager
{
    // Keep track of unique qubit ids via simple counter.
    uint64_t nextQubitId = 0;

    // Get the internal ID associated to a qubit object.
    static uint64_t GetQubitId(Qubit qubit)
    {
        return reinterpret_cast<uint64_t>(qubit);
    }

  public:
    Qubit AllocateQubit()
    {
        return reinterpret_cast<Qubit>(this->nextQubitId++);
    }
```

Note that there is no qubit reuse happening here, as done in the manager provided by the Runtime.

---

`RuntimeManagement.cpp`:

Implementation of the `IRuntimeDriver` interface is straightforward.
Qubit management is delegated to the respective `QubitManager` functions, while result management hands out either of the two `Result` types (also defined as pointers in [CoreTypes.hpp](https://github.com/microsoft/qsharp-runtime/blob/main/src/Qir/Runtime/public/CoreTypes.hpp)):

```cpp
static Result zero = reinterpret_cast<Result>(0);
static Result one = reinterpret_cast<Result>(1);
```

In order to restrict the trace simulator to straight-line pieces of code, measurement result comparisons are disabled by throwing an error on `AreEqualResults` calls of the interface:

```cpp
bool TraceSimulator::AreEqualResults(Result r1, Result r2)
{
    // Don't implement measurement-based branching for the trace simulator.
    throw std::logic_error("operation_not_supported");
}
```

Such a straight-line code restriction is not a requirement for trace simulators, but it works well with the type of hardware currently available (when using the trace simulator in that context).
For a much more complete trace simulator that is also used for resource estimation, see the [QIR Runtime trace simulator](https://github.com/microsoft/qsharp-runtime/tree/main/src/Qir/Runtime/lib/Tracer).

---

`TraceSimulation.cpp`:

Most of the instruction set required by the `IQuantumGateSet` interface consists of single-qubit gates and multi-controlled single-qubit gates.
Thus it makes sense to reuse two private functions to print out these gates:

```cpp
void TraceSimulator::ApplyGate(Gate gate, Qubit target)
{
    std::cout << "Applying gate \"" << gate << "\" on qubit "
              << this->qbm->GetQubitName(target) << std::endl;
}

void TraceSimulator::ApplyControlledGate(Gate gate, long numControls, Qubit controls[], Qubit target)
{
    std::cout << "Applying gate \"" << gate << "\" on target qubit "
              << this->qbm->GetQubitName(target) << " and controlled on qubits ";
    for (int i = 0; i < numControls; i++)
        std::cout << this->qbm->GetQubitName(controls[i]) << " ";
    std::cout << std::endl;
}
```

The text that is printed can be adjusted to suit the specific needs of the application.
The individual gates from the instruction set then call the above functions by providing the correct gate name:

```cpp
void TraceSimulator::X(Qubit q)
{
    ApplyGate("X", q);
}

void TraceSimulator::ControlledX(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("X", numControls, controls, target);
}
```

The multi-qubit gates `Exp` and `ControlledExp` can be handled similarly, just with additional printing support for multiple target qubits and different Pauli bases, and similarly for the `Measure` instruction.

An example output might look as follows:

```output
Applying gate "H" on qubit 0
Applying gate "X" on target qubit 1 and controlled on qubits 0
Applying gate "R(1.570796)_Z" on qubit 0
Applying gate "Exp(0.100000, X Y)" on qubits 0 1
Measuring qubits:
    0 in base Z
    1 in base X
```

---

Compile the sample trace simulator to a dynamic library with:

```shell
clang++ -shared TraceSimulator/RuntimeManagement.cpp TraceSimulator/TraceSimulation.cpp -o TraceSimulator.dll -Iinclude -Llib -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core'
```

### The State Simulator

This example also contains a sample full state simulator that works with the QIR Runtime.
