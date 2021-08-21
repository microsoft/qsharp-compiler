// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <iostream>
#include <string>

#include "TraceSimulator.hpp"

using namespace Microsoft::Quantum;

static std::string SelectPauli(PauliId axis)
{
    switch (axis) {
        case PauliId_I:
            return "I";
        case PauliId_X:
            return "X";
        case PauliId_Y:
            return "Y";
        case PauliId_Z:
            return "Z";
    }
}


///
/// Gate application
///

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


///
/// Supported quantum operations
///

void TraceSimulator::X(Qubit q)
{
    ApplyGate("X", q);
}

void TraceSimulator::ControlledX(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("X", numControls, controls, target);
}

void TraceSimulator::Y(Qubit q)
{
    ApplyGate("Y", q);
}

void TraceSimulator::ControlledY(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("Y", numControls, controls, target);
}

void TraceSimulator::Z(Qubit q)
{
    ApplyGate("Z", q);
}

void TraceSimulator::ControlledZ(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("Z", numControls, controls, target);
}

void TraceSimulator::H(Qubit q)
{
    ApplyGate("H", q);
}

void TraceSimulator::ControlledH(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("H", numControls, controls, target);
}

void TraceSimulator::S(Qubit q)
{
    ApplyGate("S", q);
}

void TraceSimulator::ControlledS(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("S", numControls, controls, target);
}

void TraceSimulator::AdjointS(Qubit q)
{
    ApplyGate("Sdag", q);
}

void TraceSimulator::ControlledAdjointS(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("Sdag", numControls, controls, target);
}

void TraceSimulator::T(Qubit q)
{
    ApplyGate("T", q);
}

void TraceSimulator::ControlledT(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("T", numControls, controls, target);
}

void TraceSimulator::AdjointT(Qubit q)
{
    ApplyGate("Tdag", q);
}

void TraceSimulator::ControlledAdjointT(long numControls, Qubit controls[], Qubit target)
{
    ApplyControlledGate("Tdag", numControls, controls, target);
}

void TraceSimulator::R(PauliId axis, Qubit q, double theta)
{
    Gate gatename = "R("+std::to_string(theta)+")_"+SelectPauli(axis);
    ApplyGate(gatename, q);
}

void TraceSimulator::ControlledR(long numControls, Qubit controls[], PauliId axis, Qubit target, double theta)
{
    Gate gatename = "R("+std::to_string(theta)+")_"+SelectPauli(axis);
    ApplyControlledGate(gatename, numControls, controls, target);
}

void TraceSimulator::Exp(long numTargets, PauliId paulis[], Qubit targets[], double theta)
{
    Gate gatename = "Exp(" + std::to_string(theta) + ",";
    for (int i = 0; i < numTargets; i++)
        gatename += " " + SelectPauli(paulis[i]);
    gatename += ")";

    std::cout << "Applying gate \"" << gatename << "\" on qubits ";
    for (int i = 0; i < numTargets; i++)
        std::cout << this->qbm->GetQubitName(targets[i]) << " ";
    std::cout << std::endl;
}

void TraceSimulator::ControlledExp(long numControls, Qubit controls[], long numTargets, PauliId paulis[], Qubit targets[], double theta)
{
    Gate gatename = "Exp(" + std::to_string(theta) + ",";
    for (int i = 0; i < numTargets; i++)
        gatename += " " + SelectPauli(paulis[i]);
    gatename += ")";

    std::cout << "Applying gate \"" << gatename << "\" on target qubits ";
    for (int i = 0; i < numTargets; i++)
        std::cout << this->qbm->GetQubitName(targets[i]) << " ";
    std::cout << " and controlled on qubits ";
    for (int i = 0; i < numControls; i++)
        std::cout << this->qbm->GetQubitName(controls[i]) << " ";
    std::cout << std::endl;
}

Result TraceSimulator::Measure(long numBases, PauliId bases[], long numTargets, Qubit targets[])
{
    std::cout << "Measuring qubits:" << std::endl;
    for (int i = 0; i < numTargets; i++)
        std::cout << "    " << this->qbm->GetQubitName(targets[i])
                  << " in base " << SelectPauli(bases[i]) << std::endl;
    return UseZero();
}
