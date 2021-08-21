// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "StateSimulator.hpp"

using namespace Microsoft::Quantum;


///
/// Qubit management
///

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

std::string StateSimulator::QubitToString(Qubit q)
{
    return std::to_string(this->qbm->GetQubitId(q));
}


///
/// Result management
///

static Result zero = reinterpret_cast<Result>(0);
static Result one = reinterpret_cast<Result>(1);

void StateSimulator::ReleaseResult(Result r) {}

bool StateSimulator::AreEqualResults(Result r1, Result r2)
{
    return (r1 == r2);
}

ResultValue StateSimulator::GetResultValue(Result r)
{
    return (r == one) ? Result_One : Result_Zero;
}

Result StateSimulator::UseZero()
{
    return zero;
}

Result StateSimulator::UseOne()
{
    return one;
}
