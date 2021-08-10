#include "StateSimulator.hpp"

using namespace Microsoft::Quantum;


///
/// Qubit management
///

Qubit StateSimulator::AllocateQubit()
{
    UpdateState(this->numActiveQubits++);
    return this->qbm->AllocateQubit();
}

void StateSimulator::ReleaseQubit(Qubit q)
{
    UpdateState(this->qbm->GetQubitIdx(q), /*remove=*/true);
    this->qbm->ReleaseQubit(q);
    this->numActiveQubits--;
}

std::string StateSimulator::QubitToString(Qubit q)
{
    return this->qbm->GetQubitName(q);
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
