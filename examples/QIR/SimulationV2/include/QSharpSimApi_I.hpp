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
        virtual ~IQuantumGateSet() {}

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
        virtual void ControlledExp(
            long numControls,
            Qubit controls[],
            long numTargets,
            PauliId paulis[],
            Qubit targets[],
            double theta) = 0;

        // Adjoint operations
        virtual void AdjointS(Qubit target) = 0;
        virtual void AdjointT(Qubit target) = 0;
        virtual void ControlledAdjointS(long numControls, Qubit controls[], Qubit target) = 0;
        virtual void ControlledAdjointT(long numControls, Qubit controls[], Qubit target) = 0;

        // Results
        virtual Result Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[]) = 0;
    };

    struct QIR_SHARED_API IDiagnostics
    {
        virtual ~IDiagnostics() {}

        // The callback should be invoked on each basis vector (in the standard computational basis) in little-endian
        // order until it returns `false` or the state is fully dumped.
        typedef bool (*TGetStateCallback)(size_t /*basis vector*/, double /* amplitude Re*/, double /* amplitude Im*/);

        // Deprecated, use `DumpMachine()` and `DumpRegister()` instead.
        virtual void GetState(TGetStateCallback callback) = 0;

        virtual void DumpMachine(const void* location) = 0;
        virtual void DumpRegister(const void* location, const QirArray* qubits) = 0;

        // Both Assert methods return `true`, if the assert holds, `false` otherwise.
        virtual bool Assert(
            long numTargets,
            PauliId bases[],
            Qubit targets[],
            Result result,
            const char* failureMessage) = 0;    // TODO: The `failureMessage` is not used, consider removing. The `bool` is returned.

        virtual bool AssertProbability(
            long numTargets,
            PauliId bases[],
            Qubit targets[],
            double probabilityOfZero,
            double precision,
            const char* failureMessage) = 0;  // TODO: The `failureMessage` is not used, consider removing. The `bool` is returned.
    };

}
}