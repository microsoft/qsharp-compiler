// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <memory>
#include <stdexcept>

#include "TraceSimulator.hpp"

using namespace Microsoft::Quantum;


///
/// Qubit management
///

Qubit TraceSimulator::AllocateQubit()
{
    return this->qbm->AllocateQubit();
}

void TraceSimulator::ReleaseQubit(Qubit q)
{
    this->qbm->ReleaseQubit(q);
}

std::string TraceSimulator::QubitToString(Qubit q)
{
    return this->qbm->GetQubitName(q);
}


///
/// Result management
///

static Result zero = reinterpret_cast<Result>(0);
static Result one = reinterpret_cast<Result>(1);

void TraceSimulator::ReleaseResult(Result r) {}

bool TraceSimulator::AreEqualResults(Result r1, Result r2)
{
    // Don't implement measurement-based branching for the trace simulator.
    throw std::logic_error("operation_not_supported");
}

ResultValue TraceSimulator::GetResultValue(Result r)
{
    return (r == one) ? Result_One : Result_Zero;
}

Result TraceSimulator::UseZero()
{
    return zero;
}

Result TraceSimulator::UseOne()
{
    return one;
}


///
/// Runtime driver instantiation
///

namespace Microsoft
{
namespace Quantum
{
    std::unique_ptr<IRuntimeDriver> CreateTraceSimulator()
    {
        return std::make_unique<TraceSimulator>();
    }

} // namespace Quantum
} // namespace Microsoft
