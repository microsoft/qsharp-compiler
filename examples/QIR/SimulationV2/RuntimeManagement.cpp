#include "SimpleSimulator.hpp"

using namespace Microsoft::Quantum;


///
/// Qubit management
///

Qubit CSimpleSimulator::AllocateQubit()
{
    UpdateState(this->numActiveQubits++);
    return this->qbm->AllocateQubit();
}

void CSimpleSimulator::ReleaseQubit(Qubit q)
{
    UpdateState(this->qbm->GetQubitIdx(q), /*remove=*/true);
    this->qbm->ReleaseQubit(q);
    this->numActiveQubits--;
}

std::string CSimpleSimulator::QubitToString(Qubit q)
{
    return this->qbm->GetQubitName(q);
}


///
/// Result management
///

static Result zero = reinterpret_cast<Result>(0);
static Result one = reinterpret_cast<Result>(1);

void CSimpleSimulator::ReleaseResult(Result r) {}

bool CSimpleSimulator::AreEqualResults(Result r1, Result r2)
{
    return (r1 == r2);
}

ResultValue CSimpleSimulator::GetResultValue(Result r)
{
    return (r == one) ? Result_One : Result_Zero;
}

Result CSimpleSimulator::UseZero()
{
    return zero;
}

Result CSimpleSimulator::UseOne()
{
    return one;
}
