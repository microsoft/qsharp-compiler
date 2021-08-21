// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "QirRuntimeApi_I.hpp"
#include "QSharpSimApi_I.hpp"

#include "QubitManager.hpp"

using Gate = std::string;

namespace Microsoft
{
namespace Quantum
{
    class TraceSimulator : public IRuntimeDriver, public IQuantumGateSet
    {
        // Associated qubit manager instance to handle qubit representation.
        QubitManager *qbm;

        // To be called by quantum gate set operations.
        void ApplyGate(Gate gate, Qubit target);
        void ApplyControlledGate(Gate gate, long numControls, Qubit controls[], Qubit target);

      public:
        TraceSimulator()
        {
            this->qbm = new QubitManager();
        }
        ~TraceSimulator()
        {
            delete this->qbm;
        }


        ///
        /// Implementation of IRuntimeDriver
        ///
        void ReleaseResult(Result r) override;

        bool AreEqualResults(Result r1, Result r2) override;

        ResultValue GetResultValue(Result r) override;

        Result UseZero() override;

        Result UseOne() override;

        Qubit AllocateQubit() override;

        void ReleaseQubit(Qubit q) override;

        std::string QubitToString(Qubit q) override;


        ///
        /// Implementation of IQuantumGateSet
        ///
        void X(Qubit q) override;

        void ControlledX(long numControls, Qubit controls[], Qubit target) override;

        void Y(Qubit q) override;

        void ControlledY(long numControls, Qubit controls[], Qubit target) override;

        void Z(Qubit q) override;

        void ControlledZ(long numControls, Qubit controls[], Qubit target) override;

        void H(Qubit q) override;

        void ControlledH(long numControls, Qubit controls[], Qubit target) override;

        void S(Qubit q) override;

        void ControlledS(long numControls, Qubit controls[], Qubit target) override;

        void AdjointS(Qubit q) override;

        void ControlledAdjointS(long numControls, Qubit controls[], Qubit target) override;

        void T(Qubit q) override;

        void ControlledT(long numControls, Qubit controls[], Qubit target) override;

        void AdjointT(Qubit q) override;

        void ControlledAdjointT(long numControls, Qubit controls[], Qubit target) override;

        void R(PauliId axis, Qubit target, double theta) override;

        void ControlledR(long numControls, Qubit controls[], PauliId axis, Qubit target, double theta) override;

        void Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta) override;

        void ControlledExp(long numControls, Qubit controls[], long numTargets, PauliId paulis[], Qubit targets[], double theta) override;

        Result Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[]) override;

    }; // class TraceSimulator

} // namespace Quantum
} // namespace Microsoft
