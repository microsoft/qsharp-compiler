// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <memory>

#include "QirRuntimeApi_I.hpp"
#include "QSharpSimApi_I.hpp"

namespace Microsoft
{
namespace Quantum
{
    class CustomSimulator : public IRuntimeDriver, public IQuantumGateSet, public IDiagnostics
    {
      public:
        CustomSimulator()
        {
        }
        ~CustomSimulator() override
        {
        }

        ///
        /// Implementation of IRuntimeDriver
        ///
        void ReleaseResult(Result r) override
        {
        }

        bool AreEqualResults(Result r1, Result r2) override
        {
        }

        ResultValue GetResultValue(Result r) override
        {
        }

        Result UseZero() override
        {
        }

        Result UseOne() override
        {
        }

        Qubit AllocateQubit() override
        {
        }

        void ReleaseQubit(Qubit q) override
        {
        }

        std::string QubitToString(Qubit q) override
        {
        }


        ///
        /// Implementation of IDiagnostics
        ///
        bool Assert(long numTargets, PauliId* bases, Qubit* targets, Result result, const char* failureMessage) override
        {
        }

        bool AssertProbability(long numTargets, PauliId bases[], Qubit targets[], double probabilityOfZero, double precision, const char* failureMessage) override
        {
        }

        // Deprecated, use `DumpMachine()` and `DumpRegister()` instead.
        void GetState(TGetStateCallback callback) override
        {
        }

        void DumpMachine(const void* location) override
        {
        }

        void DumpRegister(const void* location, const QirArray* qubits) override
        {
        }


        ///
        /// Implementation of IQuantumGateSet
        ///
        void X(Qubit q) override
        {
        }

        void ControlledX(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void Y(Qubit q) override
        {
        }

        void ControlledY(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void Z(Qubit q) override
        {
        }

        void ControlledZ(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void H(Qubit q) override
        {
        }

        void ControlledH(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void S(Qubit q) override
        {
        }

        void ControlledS(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void AdjointS(Qubit q) override
        {
        }

        void ControlledAdjointS(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void T(Qubit q) override
        {
        }

        void ControlledT(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void AdjointT(Qubit q) override
        {
        }

        void ControlledAdjointT(long numControls, Qubit controls[], Qubit target) override
        {
        }

        void R(PauliId axis, Qubit target, double theta) override
        {
        }

        void ControlledR(long numControls, Qubit controls[], PauliId axis, Qubit target, double theta) override
        {
        }

        void Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta) override
        {
        }

        void ControlledExp(long numControls, Qubit controls[], long numTargets, PauliId paulis[], Qubit targets[], double theta) override
        {
        }

        Result Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[]) override
        {
        }

    }; // class CustomSimulator

    std::unique_ptr<IRuntimeDriver> CreateCustomSimulator()
    {
        return std::make_unique<CustomSimulator>();
    }

} // namespace Quantum
} // namespace Microsoft
