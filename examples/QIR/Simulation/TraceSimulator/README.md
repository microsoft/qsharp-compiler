# The Trace Simulator

**NOTE:** The text below is out-of-date and is to be rewritten. See more up-to-date information in the PR ["Replace C++ QIR Runtime with Rust QIR stdlib"](https://github.com/microsoft/qsharp-runtime/pull/1087).

A trace simulator is a quick way to provide a backend to the QIR Runtime system.
It's function is to print out all quantum instructions it receives.
While this can useful for debugging, it is also a simple way to connect QIR to hardware backends that do not support control flow.
This can be achieved by customizing the output format of the simulator to the input format of the hardware backend, resulting in an output of straight-line "assembly" code.

This sample provides a "from scratch" implementation of a trace simulator, including the qubit manager, which can be used as a starting point to experiment with connecting the QIR Runtime with other systems.

## Structure of the Simulator

The QIR Runtime provides three interfaces to connect backends to the Runtime:

- `IRuntimeDriver` : Provides basic runtime functions such as qubit and measurement result management.
- `IQuantumGateSet` : The Q# instruction set. Implementation of this interface is not strictly required, as long as *some* instruction set is implemented, and the QIR code only calls instructions from that set (may necessitate the use of a bridge, see the "QIR Bridge" and the [top-level guide](../#understanding-the-qir-runtime-system)).
- `IDiagnostics` : Optional interface to provide insight into the state of a simulator or hardware backend (useful for debugging).

For a more detailed look at these interfaces, refer to the [top-level guide](../#structure-of-a-simulator) of the simulation example.

To cleanly separate out different functionalities, the following file structure is used for the sample trace simulator:

- `TraceSimulator.hpp` : Declaration of the simulator class, including required internal data structures and functions, as well as interface functions.
- `QubitManager.hpp` : Simple qubit manager implementations to be used by the simulator.
- `RuntimeManagement.cpp` : Implementation of all simulator functionality related to the `IRuntimeDriver` interface.
- `TraceSimulation.cpp` : Implementation of all simulator functionality related to the `IQuantumGateSet` interface.

## Trace Simulator Implementation

The main components of each file are explained below, leaving out repetitive or boiler-plate code, the full extent of which can be viewed in the source files.

---

`TraceSimulator.hpp`

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

For the trace simulator, the `Gate` type is represented by a string intended to hold the gate name:

```cpp
using Gate = std::string;
```

---

`QubitManager.hpp`

The sample qubit manager is kept simple, with the only required functionality besides allocation/deallocation being the ability convert a qubit to a printable string.
In this case, the internal qubit ID representation is used:

```cpp
std::string GetQubitName(Qubit qubit)
{
    return std::to_string(GetQubitId(qubit));
}
```

The qubit ID is a simple perpetually increasing counter, so no IDs are ever reused (careful with overflow!).
The `Qubit` object itself is just a pointer (as defined in "CoreTypes.hpp") with the qubit ID as its value:

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

    void ReleaseQubit(Qubit qubit)
    {
    }
```

While this is qubit manager is rather trivial, it can be adjusted to provided additional functionality suited to particular applications.
The qubit manager provided by the Runtime for example provides configurable qubit reuse functionality (virtualization).

---

`RuntimeManagement.cpp`

Implementation of the `IRuntimeDriver` interface is straightforward.
Qubit management is delegated to the respective `QubitManager` functions, while result management compares or hands out either of two `Result` values.
The `Result` type is defined as a pointer to an undefined type in "CoreTypes.hpp", allowing for backends to define custom result types.
Here, we use the raw pointer type with different numeric values for each result:

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
For a much more complete trace simulator that is also used for resource estimation, see the "QIR Runtime trace simulator".

---

`TraceSimulation.cpp`

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

## Compiling the simulator

The simulator samples require a working [Clang](https://clang.llvm.org/) installation to compile.
Refer to the [Optimization example](../../Optimization#installing-clang) for instructions on setting up Clang and LLVM.

Although the "QIR Runtime header files" are sufficient to compile the simulator, actually running it will require the Runtime binaries.
Use the NuGet CLI with the commands below to download the [QIR Runtime package](https://www.nuget.org/packages/Microsoft.Quantum.Qir.Runtime) and extract the appropriate headers and libraries (adjusting the package version as required):

- **Windows**:

    ```shell
    mkdir build
    curl https://dist.nuget.org/win-x86-commandline/latest/nuget.exe --output build/nuget.exe
    build/nuget install Microsoft.Quantum.Qir.Runtime -Version 0.18.2106148911-alpha -DirectDownload -DependencyVersion Ignore -OutputDirectory tmp
    cp tmp/Microsoft.Quantum.Qir.Runtime.0.18.2106148911-alpha/runtimes/any/native/include/* build
    cp tmp/Microsoft.Quantum.Qir.Runtime.0.18.2106148911-alpha/runtimes/win-x64/native/* build
    rm -r tmp
    ```

- **Linux** (installs mono for the NuGet CLI):

    ```shell
    mkdir build
    sudo apt update && sudo apt install -y mono-complete
    curl https://dist.nuget.org/win-x86-commandline/latest/nuget.exe --output build/nuget
    mono build/nuget sources add -name nuget.org -source https://api.nuget.org/v3/index.json
    mono build/nuget install Microsoft.Quantum.Qir.Runtime -Version 0.18.2106148911-alpha -DirectDownload -DependencyVersion Ignore -OutputDirectory tmp
    cp tmp/Microsoft.Quantum.Qir.Runtime.0.18.2106148911-alpha/runtimes/any/native/include/* build
    cp tmp/Microsoft.Quantum.Qir.Runtime.0.18.2106148911-alpha/runtimes/linux-x64/native/* build
    rm -r tmp
    ```

The sample trace simulator can then be compiled to a static library with the following commands:

- **Windows**:

    ```shell
    clang++ -fuse-ld=llvm-lib RuntimeManagement.cpp TraceSimulation.cpp -Ibuild -o build/TraceSimulator.lib
    ```

    Where the parameter `-fuse-ld` is used to specify a linker and `llvm-lib` is an LLVM replacement for MSVC's static library tool [LIB](https://docs.microsoft.com/cpp/build/reference/lib-reference).

- **Linux**:

    ```shell
    clang++ -c RuntimeManagement.cpp -Ibuild -o build/RuntimeManagement.o
    clang++ -c TraceSimulation.cpp -Ibuild -o build/TraceSimulation.o
    llvm-ar rc build/libTraceSimulator.a build/RuntimeManagement.o build/TraceSimulation.o
    ```

    Where the parameter `-c` is used to create object files, which are then combined to an archive using the `llvm-ar` command.

## Running the simulator

The [optimization example](../../Optimization#running-qir) already contains detailed instructions on how to run a Q#->QIR program via the QIR Runtime.
The basic approach remains the same for a custom simulator, except that the dynamic library housing the Q# full state simulator should be replaced with the custom simulator library compiled as shown in the previous section.
Since the compilation starting point need not always be a Q# program, the generalized steps are summarized in this section.

### Obtaining QIR code

If the quantum program is already in QIR form, this step can be skipped, making sure to adjust any filenames and paths for the example at hand.
The remaining instructions will assume the usual Q# Hello World example program.

Create a new Q# application and compile it to QIR via the following steps:

- Run `dotnet new console -lang Q# -o Hello`
- Add `<QirGeneration>true</QirGeneration>` to the `<PropertyGroup>` in `Hello/Hello.csproj`
- Run `dotnet build Hello`

### Compiling the QIR program

Create or adjust the driver program `Main.cpp` to use the custom trace simulator as shown below.
Replace `Hello__HelloQ` with any QIR function to be simulated:

```cpp
#include "QirContext.hpp"
#include "QirRuntime.hpp"
#include "QirRuntimeApi_I.hpp"

namespace Microsoft
{
namespace Quantum
{
    // Custom Trace Simulator
    std::unique_ptr<IRuntimeDriver> CreateTraceSimulator();

} // namespace Quantum
} // namespace Microsoft

using namespace Microsoft::Quantum;

// QIR function to be called
extern "C" void Hello__HelloQ();

int main(int argc, char* argv[]){
    std::unique_ptr<IRuntimeDriver> sim = CreateTraceSimulator();
    QirContextScope qirctx(sim.get(), true /*trackAllocatedObjects*/);
    Hello__HelloQ();
    return 0;
}
```

With all dependencies present in the `build` directory, the quantum program can be compiled with the custom simulator as backend:

```shell
clang++ Hello/qir/Hello.ll Main.cpp -Ibuild -Lbuild -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core' -l'Microsoft.Quantum.Qir.QSharp.Foundation' -l'TraceSimulator' -Wl',-rpath=build' -o build/Hello.exe
build/Hello
```
