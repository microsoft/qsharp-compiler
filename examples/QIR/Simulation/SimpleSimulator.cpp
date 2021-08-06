#include <unordered_map>
#include <complex>
#include <cstdlib>
#include <iostream>

#include "QirRuntimeApi_I.hpp"
#include "QSharpSimApi_I.hpp"

#include "Eigen/Dense"
#include "Eigen/KroneckerProduct"
#include "Eigen/MatrixFunctions"

using namespace Eigen;
using namespace std::complex_literals;

# define PI 3.14159265358979323846
# define TOLERANCE 1e-6

namespace Microsoft
{
namespace Quantum
{
    using State = VectorXcd;
    using Gate = Matrix2cd;
    using Pauli = Matrix2cd;
    using Operator = MatrixXcd;

    static Operator PartialTrace(Operator u, short idx, short dim)
    {
        // Partial trace defined as:
        //    tr_B[U] = (Id ⊗ <0|) U (Id ⊗ |0>) + (Id ⊗ <1|) U (Id ⊗ |1>)
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

    class CSimpleSimulator : public IRuntimeDriver, public IQuantumGateSet
    {
        // Keep track of unique qubit ids via simple counter,
        // mapped to a "small" register of active qubits.
        uint64_t nextQubitId = 0;
        short numActiveQubits = 0;
        std::unordered_map<uint64_t, short> qubitMap;
        std::vector<uint64_t> computeRegister;

        // State of a qubit is represented by its full 2^n column vector of probability amplitudes.
        // With no qubits allocated, the state starts out as the scalar 1.
        State stateVec = State::Ones(1);

        // Clients should never attempt to dereference the Result.
        Result zero = reinterpret_cast<Result>(0);
        Result one = reinterpret_cast<Result>(1);

        // Unique qubit name for each newly allocated qubit.
        static uint64_t GetQubitId(Qubit qubit)
        {
            return reinterpret_cast<uint64_t>(qubit);
        }

        // Index of the qubit in the computing register.
        short GetQubitIdx(Qubit qubit)
        {
            return this->qubitMap[GetQubitId(qubit)];
        }

        void UpdateState(short qubitIndex, bool remove = false)
        {   
            // When adding a qubit, the state vector can be updated with: |Ψ'> = |Ψ> ⊗ |0>.
            // When removing a qubit, it is traced out from the state vector: ρ = tr_i[|Ψ><Ψ|].
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

        void ApplyGate(Gate gate, short qubitIndex)
        {
            // Construct unitary as Id_A ⊗ G ⊗ Id_B, split by the qubit index.
            long dimA = pow(2, qubitIndex);
            long dimB = pow(2, this->numActiveQubits-qubitIndex-1);
            Operator unitary = Operator::Identity(dimA, dimA);
            unitary = kroneckerProduct(unitary, gate).eval();
            unitary = kroneckerProduct(unitary, Operator::Identity(dimB, dimB)).eval(); 

            // Apply gate with |Ψ'> = U|Ψ>.
            this->stateVec = unitary*this->stateVec;
        }

        void ApplyControlledGate(Gate gate, long numControls, Qubit controls[], short targetIndex)
        {
            // Controlled unitary on a bipartite system A⊗B can be expressed as:
            //     cU = (|0><0| ⊗ 1) + (|1><1| ⊗ U)    if control on A
            //     cU = (1 ⊗ |0><0|) + (U ⊗ |1><1|)    if control on B
            // Thus, the full unitary will be built starting from target in both directions
            // to handle controls coming both before and after the target.
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
                    unitary = (kroneckerProduct(Operator::Identity(dimU, dimU), project0) // 1 ⊗ |0><0|
                              +kroneckerProduct(unitary, project1)).eval();               // U ⊗ |1><1|
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
                    unitary = (kroneckerProduct(project0, Operator::Identity(dimU, dimU)) // |0><0| ⊗ 1
                              +kroneckerProduct(project1, unitary)).eval();               // |1><1| ⊗ U
                    controlItBw++;
                } else {
                    unitary = kroneckerProduct(Operator::Identity(2,2), unitary).eval();
                }
                dimU *= 2;
            }

            // Apply gate with |Ψ'> = U|Ψ>.
            this->stateVec = unitary*this->stateVec;
        }

        Pauli SelectPauliOp(PauliId axis)
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

        Operator BuildPauliUnitary(long numTargets, PauliId paulis[], Qubit targets[])
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
                Pauli p_i = SelectPauliOp(i == sortedTargetBase[i].first ?
                                          sortedTargetBase[targetIdx++].second :
                                          PauliId_I);
                pauliUnitary = kroneckerProduct(pauliUnitary, p_i).eval();
            }

            return pauliUnitary;
        }

      public:
        CSimpleSimulator(uint32_t userProvidedSeed = 0)
        {
            srand(userProvidedSeed);
        }
        ~CSimpleSimulator() override = default;

        ///
        /// Implementation of IRuntimeDriver
        ///
        void ReleaseResult(Result /*r*/) override {}

        bool AreEqualResults(Result r1, Result r2) override
        {
            return (r1 == r2);
        }

        ResultValue GetResultValue(Result r) override
        {
            return (r == one) ? Result_One : Result_Zero;
        }

        Result UseZero() override
        {
            return zero;
        }

        Result UseOne() override
        {
            return one;
        }

        Qubit AllocateQubit() override
        {
            UpdateState(this->numActiveQubits);
            this->qubitMap[this->nextQubitId] = this->numActiveQubits++;
            this->computeRegister.push_back(this->nextQubitId);
            return reinterpret_cast<Qubit>(this->nextQubitId++);
        }

        void ReleaseQubit(Qubit q) override
        {
            UpdateState(GetQubitIdx(q), /*remove=*/true);
            this->computeRegister.erase(this->computeRegister.begin()+GetQubitIdx(q));
            for (int i = GetQubitIdx(q); i < --this->numActiveQubits; i++)
                this->qubitMap[this->computeRegister[i]] -= 1;
            this->qubitMap.erase(GetQubitId(q));
        }

        std::string QubitToString(Qubit q) override
        {
            return std::to_string(GetQubitId(q));
        }


        ///
        /// Implementation of IQuantumGateSet
        ///
        void X(Qubit q) override
        {
            Gate x; x << 0, 1,
                         1, 0;
            ApplyGate(x, GetQubitIdx(q));
        }

        void ControlledX(long numControls, Qubit controls[], Qubit target) override
        {
            Gate x; x << 0, 1,
                         1, 0;
            ApplyControlledGate(x, numControls, controls, GetQubitIdx(target));
        }

        void Y(Qubit q) override
        {
            Gate y; y <<  0,-1i,
                         1i,  0;
            ApplyGate(y, GetQubitIdx(q));
        }

        void ControlledY(long numControls, Qubit controls[], Qubit target) override
        {
            Gate y; y <<  0,-1i,
                         1i,  0;
            ApplyControlledGate(y, numControls, controls, GetQubitIdx(target));
        }

        void Z(Qubit q) override
        {
            Gate z; z << 1, 0,
                         0,-1;
            ApplyGate(z, GetQubitIdx(q));
        }

        void ControlledZ(long numControls, Qubit controls[], Qubit target) override
        {
            Gate z; z << 1, 0,
                         0,-1;
            ApplyControlledGate(z, numControls, controls, GetQubitIdx(target));
        }

        void H(Qubit q) override
        {
            Gate h; h << 1, 1,
                         1,-1;
            h = h / sqrt(2);
            ApplyGate(h, GetQubitIdx(q));
        }

        void ControlledH(long numControls, Qubit controls[], Qubit target) override
        {
            Gate h; h << 1, 1,
                         1,-1;
            h = h / sqrt(2);
            ApplyControlledGate(h, numControls, controls, GetQubitIdx(target));
        }

        void S(Qubit q) override
        {
            Gate s; s << 1,  0,
                         0, 1i;
            ApplyGate(s, GetQubitIdx(q));
        }

        void ControlledS(long numControls, Qubit controls[], Qubit target) override
        {
            Gate s; s << 1,  0,
                         0, 1i;
            ApplyControlledGate(s, numControls, controls, GetQubitIdx(target));
        }

        void AdjointS(Qubit q) override
        {
            Gate sdag; sdag << 1,  0,
                               0,-1i;
            ApplyGate(sdag, GetQubitIdx(q));
        }

        void ControlledAdjointS(long numControls, Qubit controls[], Qubit target) override
        {
            Gate sdag; sdag << 1,  0,
                               0,-1i;
            ApplyControlledGate(sdag, numControls, controls, GetQubitIdx(target));
        }

        void T(Qubit q) override
        {
            Gate t; t << 1, 0,
                         0, exp(1i*PI/4.);
            ApplyGate(t, GetQubitIdx(q));
        }

        void ControlledT(long numControls, Qubit controls[], Qubit target) override
        {
            Gate t; t << 1, 0,
                         0, exp(1i*PI/4.);
            ApplyControlledGate(t, numControls, controls, GetQubitIdx(target));
        }

        void AdjointT(Qubit q) override
        {
            Gate tdag; tdag << 1, 0,
                               0, exp(-1i*PI/4.);
            ApplyGate(tdag, GetQubitIdx(q));
        }

        void ControlledAdjointT(long numControls, Qubit controls[], Qubit target) override
        {
            Gate tdag; tdag << 1, 0,
                               0, exp(-1i*PI/4.);
            ApplyControlledGate(tdag, numControls, controls, GetQubitIdx(target));
        }

        void R(PauliId axis, Qubit q, double theta) override
        {
            Gate r = (-1i*theta/2.0*SelectPauliOp(axis)).exp();
            ApplyGate(r, GetQubitIdx(q));
        }

        void ControlledR(long numControls, Qubit controls[], PauliId axis, Qubit target, double theta) override
        {
            Gate r = (-1i*theta/2.0*SelectPauliOp(axis)).exp();
            ApplyControlledGate(r, numControls, controls, GetQubitIdx(target));
        }

        void Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta) override
        {
            Operator u = (1i*theta*BuildPauliUnitary(numTargets, paulis, targets)).exp();
            this->stateVec = u*this->stateVec;
        }

        void ControlledExp(long numControls, Qubit controls[], long numTargets, PauliId paulis[], Qubit targets[], double theta) override
        {
        }

        Result Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[]) override
        {
            assert(numBases == numTargets);
            short dim = this->numActiveQubits;

            // Projection operators P_+- for Pauli measurements {P_i}:
            //     P_+- = (1 +- P_1⊗P_2⊗..⊗P_n)/2
            Operator paulis = BuildPauliUnitary(numTargets, bases, targets);
            Operator p_projector = (Operator::Identity(dim, dim) + paulis)/2;
            Operator m_projector = (Operator::Identity(dim, dim) - paulis)/2;

            // Probability of getting outcome Zero is p(+) = <Ψ|P_+|Ψ>.
            double probZero = real(this->stateVec.conjugate().dot(p_projector*this->stateVec));

            // Select measurement outcome via PRNG.
            double random0to1 = (double) rand() / (RAND_MAX);
            Result outcome = random0to1 < probZero ? zero : one;

            // Update state vector with |Ψ'> = 1/√p(m) P_m|Ψ>.
            if (outcome == zero)
                this->stateVec = (p_projector * this->stateVec)/sqrt(probZero);
            else
                this->stateVec = (m_projector * this->stateVec)/sqrt(1-probZero);

            return outcome;
        }

    }; // class CSimpleSimulator

    std::unique_ptr<IRuntimeDriver> CreateSimpleSimulator(uint32_t userProvidedSeed = 0)
    {
        return std::make_unique<CSimpleSimulator>(userProvidedSeed);
    }

} // namespace Quantum
} // namespace Microsoft
