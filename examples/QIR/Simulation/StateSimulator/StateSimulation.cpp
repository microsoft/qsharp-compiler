// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <complex>
#include <utility>

#include "StateSimulator.hpp"

#include "Eigen/KroneckerProduct"
#include "Eigen/MatrixFunctions"

using namespace Microsoft::Quantum;
using namespace Eigen;
using namespace std::complex_literals;

# define PI 3.14159265358979323846
# define TOLERANCE 1e-6

static Operator PartialTrace(Operator u, short idx, short dim)
{
    // Partial trace defined as:
    //    tr_B[U] = (Id ⊗ 〈0|) U (Id ⊗ |0⟩) + (Id ⊗ 〈1|) U (Id ⊗ |1⟩)
    // This is expanded to arbitrary sized systems where a single qubit is traced out.
    Operator proj_zero = Operator::Ones(1,1), proj_one = Operator::Ones(1,1);
    for (int i = 0; i < idx; i++) {
        proj_zero = kroneckerProduct(proj_zero, Operator::Identity(2,2)).eval();
        proj_one = kroneckerProduct(proj_one, Operator::Identity(2,2)).eval();
    }
    proj_zero = kroneckerProduct(proj_zero, Vector2cd(1,0)).eval();
    proj_one = kroneckerProduct(proj_one, Vector2cd(0,1)).eval();
    for (int i = idx+1; i < dim; i++) {
        proj_zero = kroneckerProduct(proj_zero, Operator::Identity(2,2)).eval();
        proj_one = kroneckerProduct(proj_one, Operator::Identity(2,2)).eval();
    }

    Operator result = proj_zero.adjoint() * u * proj_zero
                    + proj_one.adjoint() * u * proj_one;

    return result;
}

static Pauli SelectPauliOp(PauliId axis)
{
    switch (axis) {
        case PauliId_I:
            return (Pauli() << 1,0,0,1).finished();
        case PauliId_X:
            return (Pauli() << 0,1,1,0).finished();
        case PauliId_Y:
            return (Pauli() << 0,-1i,1i,0).finished();
        case PauliId_Z:
            return (Pauli() << 1,0,0,-1).finished();
    }
}


///
/// State manipulation
///

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

void StateSimulator::ApplyGate(Gate gate, Qubit target)
{
    // Construct unitary as Id_A ⊗ G ⊗ Id_C, split by the qubit index.
    short qubitIndex = GetQubitIdx(target);
    long dimA = pow(2, qubitIndex);
    long dimC = pow(2, this->numActiveQubits-qubitIndex-1);
    Operator unitary = Operator::Identity(dimA, dimA);
    unitary = kroneckerProduct(unitary, gate).eval();
    unitary = kroneckerProduct(unitary, Operator::Identity(dimC, dimC)).eval();

    // Apply gate with |Ψ'⟩ = U|Ψ⟩.
    this->stateVec = unitary*this->stateVec;
}

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


///
/// Supported quantum operations
///

void StateSimulator::X(Qubit q)
{
    Gate x; x << 0, 1,
                 1, 0;
    ApplyGate(x, q);
}

void StateSimulator::ControlledX(long numControls, Qubit controls[], Qubit target)
{
    Gate x; x << 0, 1,
                 1, 0;
    ApplyControlledGate(x, numControls, controls, target);
}

void StateSimulator::Y(Qubit q)
{
    Gate y; y <<  0,-1i,
                 1i,  0;
    ApplyGate(y, q);
}

void StateSimulator::ControlledY(long numControls, Qubit controls[], Qubit target)
{
    Gate y; y <<  0,-1i,
                 1i,  0;
    ApplyControlledGate(y, numControls, controls, target);
}

void StateSimulator::Z(Qubit q)
{
    Gate z; z << 1, 0,
                 0,-1;
    ApplyGate(z, q);
}

void StateSimulator::ControlledZ(long numControls, Qubit controls[], Qubit target)
{
    Gate z; z << 1, 0,
                 0,-1;
    ApplyControlledGate(z, numControls, controls, target);
}

void StateSimulator::H(Qubit q)
{
    Gate h; h << 1, 1,
                 1,-1;
    h = h / sqrt(2);
    ApplyGate(h, q);
}

void StateSimulator::ControlledH(long numControls, Qubit controls[], Qubit target)
{
    Gate h; h << 1, 1,
                 1,-1;
    h = h / sqrt(2);
    ApplyControlledGate(h, numControls, controls, target);
}

void StateSimulator::S(Qubit q)
{
    Gate s; s << 1,  0,
                 0, 1i;
    ApplyGate(s, q);
}

void StateSimulator::ControlledS(long numControls, Qubit controls[], Qubit target)
{
    Gate s; s << 1,  0,
                 0, 1i;
    ApplyControlledGate(s, numControls, controls, target);
}

void StateSimulator::AdjointS(Qubit q)
{
    Gate sdag; sdag << 1,  0,
                       0,-1i;
    ApplyGate(sdag, q);
}

void StateSimulator::ControlledAdjointS(long numControls, Qubit controls[], Qubit target)
{
    Gate sdag; sdag << 1,  0,
                       0,-1i;
    ApplyControlledGate(sdag, numControls, controls, target);
}

void StateSimulator::T(Qubit q)
{
    Gate t; t << 1, 0,
                 0, exp(1i*PI/4.);
    ApplyGate(t, q);
}

void StateSimulator::ControlledT(long numControls, Qubit controls[], Qubit target)
{
    Gate t; t << 1, 0,
                 0, exp(1i*PI/4.);
    ApplyControlledGate(t, numControls, controls, target);
}

void StateSimulator::AdjointT(Qubit q)
{
    Gate tdag; tdag << 1, 0,
                       0, exp(-1i*PI/4.);
    ApplyGate(tdag, q);
}

void StateSimulator::ControlledAdjointT(long numControls, Qubit controls[], Qubit target)
{
    Gate tdag; tdag << 1, 0,
                       0, exp(-1i*PI/4.);
    ApplyControlledGate(tdag, numControls, controls, target);
}

void StateSimulator::R(PauliId axis, Qubit q, double theta)
{
    Gate r = (-1i*theta/2.0*SelectPauliOp(axis)).exp();
    ApplyGate(r, q);
}

void StateSimulator::ControlledR(long numControls, Qubit controls[], PauliId axis, Qubit target, double theta)
{
    Gate r = (-1i*theta/2.0*SelectPauliOp(axis)).exp();
    ApplyControlledGate(r, numControls, controls, target);
}

void StateSimulator::Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta)
{
    Operator u = (1i*theta*BuildPauliUnitary(numTargets, paulis, targets)).exp();
    this->stateVec = u*this->stateVec;
}

void StateSimulator::ControlledExp(long numControls, Qubit controls[], long numTargets, PauliId paulis[], Qubit targets[], double theta)
{
    throw std::logic_error("operation_not_supported");
}

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

Operator StateSimulator::BuildPauliUnitary(long numTargets, PauliId paulis[], Qubit targets[])
{
    // Sort pauli matrices by the target qubit's index in the compute register.
    std::vector<std::pair<short, PauliId>> sortedTargetBase(numTargets);
    for (int i = 0; i < numTargets; i++)
        sortedTargetBase.push_back({GetQubitIdx(targets[i]), paulis[i]});
    std::sort(sortedTargetBase.begin(), sortedTargetBase.end(),
              [](std::pair<short, PauliId> x, std::pair<short, PauliId> y) {
              return x.first < y.first;
    });

    Operator pauliUnitary = Operator::Ones(1,1);
    for (int i = 0, targetIdx = 0; i < this->numActiveQubits; i++) {
        Pauli p_i = SelectPauliOp(i == sortedTargetBase[targetIdx].first ?
                                  sortedTargetBase[targetIdx++].second :
                                  PauliId_I);
        pauliUnitary = kroneckerProduct(pauliUnitary, p_i).eval();
    }

    return pauliUnitary;
}
