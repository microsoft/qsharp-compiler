# The State Simulator

**NOTE:** The text below is out-of-date and is to be rewritten. See more up-to-date information in the PR ["Replace C++ QIR Runtime with Rust QIR stdlib"](https://github.com/microsoft/qsharp-runtime/pull/1087).

A full state simulator mimics an ideal quantum computer with infinite compute register, although classical hardware limitations generally impose a simulation limit of at most a few dozen active qubits.
This sample provides a "from scratch" implementation of such a simulator, using the C++ linear algebra library [Eigen](http://eigen.tuxfamily.org/).
The simulator is designed to closely correspond to the mathematical operations which are typically used to describe quantum circuits.
Instead of using a custom qubit manager, the sample demonstrates how to hook up the qubit manager provided by the QIR Runtime.

## Structure of the Simulator

The QIR Runtime provides three interfaces to connect backends to the Runtime, which will be implemented by the simulator:

- `IRuntimeDriver` : Provides basic runtime functions such as qubit and measurement result management.
- `IQuantumGateSet` : The Q# instruction set. Implementation of this interface is not strictly required, as long as *some* instruction set is implemented, and the QIR code only calls instructions from that set (may necessitate the use of a bridge, see the "QIR Bridge").
- `IDiagnostics` : Optional interface to provide insight into the state of a simulator or hardware backend (useful for debugging).

For a more detailed look at them, refer to the [top-level guide](../#structure-of-a-simulator) of the simulation example.

The approach taken by the simulator closely mimics the formalism used to describe computations in the circuit model of quantum computing.
The state an n-qubit computer is represented by a 2^n complex vector called state vector.
Quantum operations are represented by 2^n x 2^n unitary matrices which are applied to the state vector via matrix multiplication.
In order to cleanly separate out different functionalities, the following file structure is used for the sample state simulator:

- `TraceSimulator.hpp` : Declaration of the simulator class, including required internal data structures and functions, as well as interface functions.
- `RuntimeManagement.cpp` : Implementation of all simulator functionality related to the `IRuntimeDriver` interface.
- `TraceSimulation.cpp` : Implementation of all simulator functionality related to the `IQuantumGateSet` interface.

## State Simulator Implementation

The main components of each file are explained below, leaving out repetitive or boiler-plate code, the full extent of which can be viewed in the source files.

---

`StateSimulator.hpp`

A couple of types are defined to represent quantum objects.
Since the state vector expands and contracts throughout the computation, the `State` type is defined as dynamic size complex vector from the Eigen library.
For the state simulator, the 1-qubit `Gate` type is represented by a 2x2 complex matrix, and the same goes for the `Pauli` matrix type.
Lastly an `Operator` matrix type is defined for arbitrary size quantum operators:

```cpp
using State = Eigen::VectorXcd;
using Gate = Eigen::Matrix2cd;
using Pauli = Eigen::Matrix2cd;
using Operator = Eigen::MatrixXcd;
```

While a qubit manager instance `qbm` manages `Qubit` objects via internal IDs, the full state simulator keeps a compact register of currently active qubits in the `computeRegister`.
This register is required to maintain the order of qubits in their state representation as well as construct operators on the currently active qubit space.
So for example, for some set of qubits {q_i, q_j, q_k} in the compute register, the simulator performs computations on the Hilbert space H_i ⊗ H_j ⊗ H_k.
The state of the active qubits in this Hilbert space is stored in the `stateVector` member:

```cpp
class StateSimulator : public IRuntimeDriver, public IQuantumGateSet
{
    // Associated qubit manager instance to handle qubit representation.
    CQubitManager *qbm;

    // The register of currently active qubits.
    short numActiveQubits = 0;
    std::vector<Qubit> computeRegister;

    // The state of the compute register is represented by its full 2^n column vector of probability amplitudes.
    // With no qubits allocated, the state vector starts out as the scalar 1.
    State stateVec = State::Ones(1);
```

The helper functions below deal with updating the state vector for new or deallocated qubits, apply `Gate` or multi-controlled `Gate` operations to the compute register, and build operators over the active qubit space made up of tensor products of pauli matrices.

```cpp
    // To be called on allocation/deallocation of qubits to update the state vector.
    void UpdateState(short qubitIndex, bool remove = false);

    // To be called by quantum gate set operations.
    void ApplyGate(Gate gate, Qubit target);
    void ApplyControlledGate(Gate gate, long numControls, Qubit controls[], Qubit target);

    // Builds a unitary matrix over the state space made of Pauli operators.
    Operator BuildPauliUnitary(long numTargets, PauliId paulis[], Qubit targets[]);
```

A new qubit manager instance can simply be attached to the simulator in the constructor, which also initializes the PRNG with a provided seed:

```cpp
    StateSimulator(uint32_t userProvidedSeed = 0)
    {
        srand(userProvidedSeed);
        this->qbm = new CQubitManager();
    }
    ~StateSimulator()
    {
        delete this->qbm;
    }
```

The following functions will be used from the QIR qubit manager, but additional functionality is present to manage how qubits are reused (full interface at "public/QubitManager.hpp"):

```cpp
Qubit CQubitManager::Allocate()
CQubitManager::Release(Qubit q)
int32_t CQubitManager::GetQubitId(Qubit q)
```

---

`RuntimeManagement.cpp`

Implementation of the `IRuntimeDriver` interface is straightforward.
Qubit management is delegated to the respective `QubitManager` functions, taking care to add or remove qubits from the compute register and update the state vector accordingly.
Note that new qubits are simply appended to the end of the register:

```cpp
Qubit StateSimulator::AllocateQubit()
{
    Qubit q = this->qbm->Allocate();
    this->computeRegister.push_back(q);
    UpdateState(this->numActiveQubits++);  // |Ψ'⟩ = |Ψ⟩ ⊗ |0⟩
    return q;
}

void StateSimulator::ReleaseQubit(Qubit q)
{
    UpdateState(GetQubitIdx(q), /*remove=*/true);  // ρ' = tr_i[|Ψ⟩〈Ψ|]
    this->numActiveQubits--;
    this->computeRegister.erase(this->computeRegister.begin() + GetQubitIdx(q));
    this->qbm->Release(q);
}
```

Result management hands out either of two `Result` values, where the `Result` type is defined as a pointer to an undefined type in "CoreTypes.hpp", allowing for backends to define custom result types.
Here, we use the raw pointer type with different numeric values for each result:

```cpp
static Result zero = reinterpret_cast<Result>(0);
static Result one = reinterpret_cast<Result>(1);
```

Note that similar to `Result`, backends can also provide a custom qubit type since `Qubit` is also defined as a pointer to an undefined type.

---

`StateSimulation.cpp`

Most of the instruction set required by the `IQuantumGateSet` interface consists of single-qubit gates and multi-controlled single-qubit gates.
Thus, it makes sense to define two private methods that apply an arbitrary `Gate` or controlled `Gate` to the state vector.
This allows most gate instructions to simply consist of an instantiation of the base `Gate` via its matrix elements, followed by a call to either of the two apply methods.
For example, the `H` gate and `ControlledT` gate are simply defined as follows:

```cpp
void StateSimulator::H(Qubit q)
{
    Gate h; h << 1, 1,
                 1,-1;
    h = h / sqrt(2);
    ApplyGate(h, q);
}

void StateSimulator::ControlledT(long numControls, Qubit controls[], Qubit target)
{
    Gate t; t << 1, 0,
                 0, exp(1i*PI/4.);
    ApplyControlledGate(t, numControls, controls, target);
}
```

The underlying idea of the `ApplyGate` method is to construct an operator over the entire state space and use matrix multiplication to apply it to the state vector.
This can be done by simply sandwiching the gate to be applied between two identity matrices that span the rest of the Hilbert space (i.e. `U = Id_A ⊗ G ⊗ Id_C`):

```cpp
void StateSimulator::ApplyGate(Gate gate, Qubit target)
{
    // Construct unitary as Id_A ⊗ G ⊗ Id_C, split at the qubit index.
    short qubitIndex = GetQubitIdx(target);
    long dimA = pow(2, qubitIndex);
    long dimC = pow(2, this->numActiveQubits-qubitIndex-1);
    Operator unitary = Operator::Identity(dimA, dimA);
    unitary = kroneckerProduct(unitary, gate).eval();
    unitary = kroneckerProduct(unitary, Operator::Identity(dimC, dimC)).eval();

    // Apply gate with |Ψ'⟩ = U|Ψ⟩.
    this->stateVec = unitary*this->stateVec;
}
```

The `ApplyControlledGate` method is a bit lengthier because of it has to support arbitrary control qubits, but the idea is the same.
This time, the final operator needs to be constructed starting from the target qubit outward, using the tensor product to add an action on a new qubit at each step.
There are three possible scenarios for each qubit (say in increasing direction of the register):

- the qubit is the target:
    initialize the operator to the gate `U = G`
- the new qubit is not involved in the computation:
    add a 2x2 identity to the operator `U' = U ⊗ Id`
- the new qubit is a control:
    the new operator is a combination of the identity when the control is in |0⟩ and the action computed so far when the control is in |1⟩ `U' = (Id ⊗ |0⟩〈0|) + (U ⊗ |1⟩〈1|)`

```cpp
void StateSimulator::ApplyControlledGate(Gate gate, long numControls, Qubit controls[], Qubit target)
{
    // Controlled unitary on a bipartite system A⊗B can be expressed as:
    //     cU = (|0⟩〈0| ⊗ 1) + (|1⟩〈1| ⊗ U)    if control on A
    //     cU = (1 ⊗ |0⟩〈0|) + (U ⊗ |1⟩〈1|)    if control on B
    // Thus, the full unitary will be built starting from target in both directions
    // to handle controls coming both before and after the target.
    short targetIndex = GetQubitIdx(target);
    std::vector<short> preTargetIndices, postTargetIndices;
    for (int i = 0; i < numControls; i++) {
        short idx = GetQubitIdx(controls[i]);
        if (idx < targetIndex)
            preTargetIndices.push_back(idx);
        else
            postTargetIndices.push_back(idx);
    }
    sort(preTargetIndices.begin(), preTargetIndices.end());
    sort(postTargetIndices.begin(), postTargetIndices.end());

    long dimU = 2;
    Operator unitary = gate;
    Operator project0 = (Operator(2,2) << 1,0,0,0).finished();
    Operator project1 = (Operator(2,2) << 0,0,0,1).finished();
    // Build up unitary from target to last qubit.
    auto controlItFw = postTargetIndices.begin();
    for (int i = targetIndex+1; i < this->numActiveQubits; i++) {
        if (controlItFw != postTargetIndices.end() && i == *controlItFw) {
            unitary = (kroneckerProduct(Operator::Identity(dimU, dimU), project0) // 1 ⊗ |0⟩〈0|
                      +kroneckerProduct(unitary, project1)).eval();               // U ⊗ |1⟩〈1|
            controlItFw++;
        } else {
            unitary = kroneckerProduct(unitary, Operator::Identity(2,2)).eval();
        }
        dimU *= 2;
    }
    // Build up the unitary from target to first qubit.
    auto controlItBw = preTargetIndices.rbegin();
    for (int i = targetIndex-1; i >= 0; i--) {
        if (controlItBw != preTargetIndices.rend() && i == *controlItBw) {
            unitary = (kroneckerProduct(project0, Operator::Identity(dimU, dimU)) // |0⟩〈0| ⊗ 1
                      +kroneckerProduct(project1, unitary)).eval();               // |1⟩〈1| ⊗ U
            controlItBw++;
        } else {
            unitary = kroneckerProduct(Operator::Identity(2,2), unitary).eval();
        }
        dimU *= 2;
    }

    // Apply gate with |Ψ'⟩ = U|Ψ⟩.
    this->stateVec = unitary*this->stateVec;
}
```

We also need to define what happens to the state vector when we add or remove a qubit.
In the case of adding a new qubit, the tensor product (or Kronecker product) is used to add the qubit to the state vector (last in the register, i.e `|Ψ'⟩ = |Ψ⟩ ⊗ |0⟩`).
When removing a qubit, it is assumed to be in a product state with the rest of the register, and can thus be traced out from the state vector (i.e. `ρ' = |Ψ'⟩〈Ψ'| = tr_i[|Ψ⟩〈Ψ|]`).
The `PartialTrace` method is defined in `StateSimulation.cpp` based on the definition `tr_B[U] = (Id ⊗ 〈0|) U (Id ⊗ |0⟩) + (Id ⊗ 〈1|) U (Id ⊗ |1⟩)`).
The new state vector is then extracted from the density matrix using eigenvalue decomposition.
Up to computational precision, there should only be a single eigenvalue whose corresponding eigenvector is the new state vector:

```cpp
void StateSimulator::UpdateState(short qubitIndex, bool remove)
{
    // When adding a qubit, the state vector can be updated with: |Ψ'⟩ = |Ψ⟩ ⊗ |0⟩.
    // When removing a qubit, it is traced out from the state vector: ρ' = tr_i[|Ψ⟩〈Ψ|].
    if (!remove) {
        this->stateVec = kroneckerProduct(this->stateVec, Vector2cd(1,0)).eval();
    } else {
        Operator densityMatrix = this->stateVec * this->stateVec.adjoint();
        densityMatrix = PartialTrace(densityMatrix, qubitIndex, this->numActiveQubits);

        // Ensure state is pure tr(ρ^2)=1, meaning the removed qubit was in a product state.
        assert(abs((densityMatrix*densityMatrix).trace()-1.0) < TOLERANCE);

        SelfAdjointEigenSolver<Operator> eigensolver(densityMatrix);
        assert(eigensolver.info() == Success && "Failed to decompose density matrix.");

        Index maxEigenIdx;
        ArrayXd eigenvals = eigensolver.eigenvalues().array();
        eigenvals.abs().maxCoeff(&maxEigenIdx);
        assert(abs(eigenvals(maxEigenIdx) - 1.0) < TOLERANCE);

        this->stateVec = eigensolver.eigenvectors().col(maxEigenIdx);
    }
}
```

Measurements are applied using the postulates and theory of projective measurements in QM.
Accordingly, a measurement is defined via a set of projection operators `{P_m}`, each one associated to one measurement outcome `m`.
The probability of obtaining outcome `m` is given by `p(m) = 〈Ψ|P_m|Ψ⟩`, and the post-measurement state is `|Ψ'⟩ = 1/√p(m) P_m|Ψ⟩`.
The type of measurement implemented for the QIR Runtime is a [projective Pauli measurement](https://docs.microsoft.com/azure/quantum/concepts-pauli-measurements) defined by a set of pauli matrices `P_i ∈ {Id, X, Y, Z}` determining the basis of measurement for each qubit.
There are only two possible results for such a measurement, given by a positive (+) and negative (-) parity, since each individual Pauli measurement returns either +1 or -1.
Thus, the two projective measurement operators are given by `P_+- = (1 +- P_1⊗P_2⊗..⊗P_n)/2`:

```cpp
Result StateSimulator::Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[])
{
    assert(numBases == numTargets);
    short dim = this->numActiveQubits;

    // Projection operators P_+- for Pauli measurements {P_i}:
    //     P_+- = (1 +- P_1⊗P_2⊗..⊗P_n)/2
    Operator paulis = BuildPauliUnitary(numTargets, bases, targets);
    Operator p_projector = (Operator::Identity(dim, dim) + paulis)/2;
    Operator m_projector = (Operator::Identity(dim, dim) - paulis)/2;

    // Probability of getting outcome Zero is p(+) = 〈Ψ|P_+|Ψ⟩.
    double probZero = real(this->stateVec.conjugate().dot(p_projector*this->stateVec));

    // Select measurement outcome via PRNG.
    double random0to1 = (double) rand() / (RAND_MAX);
    Result outcome = random0to1 < probZero ? UseZero() : UseOne();

    // Update state vector with |Ψ'⟩ = 1/√p(m) P_m|Ψ⟩.
    if (outcome == UseZero())
        this->stateVec = (p_projector * this->stateVec)/sqrt(probZero);
    else
        this->stateVec = (m_projector * this->stateVec)/sqrt(1-probZero);

    return outcome;
}
```

The function `BuildPauliUnitary` simply generates the `Operator` "`P_1⊗P_2⊗..⊗P_n`" over the active qubit space.


## Compiling the simulator

The simulator samples require a working [Clang](https://clang.llvm.org/) installation to compile.
Refer to the [Optimization example](../../Optimization#installing-clang) for instructions on setting up Clang and LLVM.

Download the latest stable release of the [Eigen library](http://eigen.tuxfamily.org/) and copy over the relevant headers as described in [include/Eigen](include/Eigen/README.md).

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

The sample state simulator can then be compiled to a static library with the following commands:

- **Windows**:

    ```shell
    clang++ -fuse-ld=llvm-lib RuntimeManagement.cpp StateSimulation.cpp -Iinclude -Ibuild -o build/StateSimulator.lib
    ```

    Where the parameter `-fuse-ld` is used to specify a linker and `llvm-lib` is an LLVM replacement for MSVC's static library tool [LIB](https://docs.microsoft.com/cpp/build/reference/lib-reference).

- **Linux**:

    ```shell
    clang++ -c RuntimeManagement.cpp -Iinclude -Ibuild -o build/RuntimeManagement.o
    clang++ -c StateSimulation.cpp -Iinclude -Ibuild -o build/StateSimulation.o
    llvm-ar rc build/libStateSimulator.a build/RuntimeManagement.o build/StateSimulation.o
    ```

    Where the parameter `-c` is used to create object files, which are then combined to an archive using the `llvm-ar` command.

## Running the simulator

Refer to the trace simulator sample for instructions on how to [run QIR with a custom simulator](../TraceSimulator/#running-the-simulator).
