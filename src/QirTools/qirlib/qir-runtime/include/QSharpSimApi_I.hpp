// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <complex>

#include "CoreTypes.hpp"
#include "QirTypes.hpp"

namespace Microsoft
{
namespace Quantum
{
    struct QIR_SHARED_API IQuantumGateSet
    {
        virtual ~IQuantumGateSet()
        {
        }
        IQuantumGateSet() = default;

        // Elementary operatons
        virtual void X(QubitIdType target)                                                       = 0;
        virtual void Y(QubitIdType target)                                                       = 0;
        virtual void Z(QubitIdType target)                                                       = 0;
        virtual void H(QubitIdType target)                                                       = 0;
        virtual void S(QubitIdType target)                                                       = 0;
        virtual void T(QubitIdType target)                                                       = 0;
        virtual void R(PauliId axis, QubitIdType target, double theta)                           = 0;
        virtual void Exp(long numTargets, PauliId paulis[], QubitIdType targets[], double theta) = 0;

        // Multicontrolled operations
        virtual void ControlledX(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledY(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledZ(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledH(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledS(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledT(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledR(long numControls, QubitIdType controls[], PauliId axis, QubitIdType target,
                                 double theta)                                                 = 0;
        virtual void ControlledExp(long numControls, QubitIdType controls[], long numTargets, PauliId paulis[],
                                   QubitIdType targets[], double theta)                        = 0;

        // Adjoint operations
        virtual void AdjointS(QubitIdType target)                                                     = 0;
        virtual void AdjointT(QubitIdType target)                                                     = 0;
        virtual void ControlledAdjointS(long numControls, QubitIdType controls[], QubitIdType target) = 0;
        virtual void ControlledAdjointT(long numControls, QubitIdType controls[], QubitIdType target) = 0;

        // Results
        virtual Result Measure(long numBases, PauliId bases[], long numTargets, QubitIdType targets[]) = 0;

      private:
        IQuantumGateSet& operator=(const IQuantumGateSet&) = delete;
        IQuantumGateSet(const IQuantumGateSet&)            = delete;
    };

    struct QIR_SHARED_API IDiagnostics
    {
        virtual ~IDiagnostics()
        {
        }
        IDiagnostics() = default;

        // The callback should be invoked on each basis vector (in the standard computational basis) in little-endian
        // order until it returns `false` or the state is fully dumped.
        typedef bool (*TGetStateCallback)(size_t /*basis vector*/, double /* amplitude Re*/, double /* amplitude Im*/);

        // Deprecated, use `DumpMachine()` and `DumpRegister()` instead.
        virtual void GetState(TGetStateCallback callback) = 0;

        virtual void DumpMachine(const void* location)                          = 0;
        virtual void DumpRegister(const void* location, const QirArray* qubits) = 0;

        // Both Assert methods return `true`, if the assert holds, `false` otherwise.
        virtual bool Assert(long numTargets, PauliId bases[], QubitIdType targets[], Result result,
                            const char* failureMessage) = 0; // TODO: The `failureMessage` is not used, consider
                                                             // removing. The `bool` is returned.

        virtual bool AssertProbability(long numTargets, PauliId bases[], QubitIdType targets[],
                                       double probabilityOfZero, double precision,
                                       const char* failureMessage) = 0; // TODO: The `failureMessage` is not used,
                                                                        // consider removing. The `bool` is returned.

      private:
        IDiagnostics& operator=(const IDiagnostics&) = delete;
        IDiagnostics(const IDiagnostics&)            = delete;
    };

} // namespace Quantum
} // namespace Microsoft
