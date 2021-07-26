// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <complex>

#include "CoreTypes.hpp"

namespace Microsoft
{
namespace Quantum
{
    struct QIR_SHARED_API IRuntimeDriver
    {
        virtual ~IRuntimeDriver() {}

        // Doesn't necessarily provide insight into the state of the qubit (for that look at IDiagnostics)
        virtual std::string QubitToString(Qubit qubit) = 0;

        // Qubit management
        virtual Qubit AllocateQubit() = 0;
        virtual void ReleaseQubit(Qubit qubit) = 0;

        virtual void ReleaseResult(Result result) = 0;
        virtual bool AreEqualResults(Result r1, Result r2) = 0;
        virtual ResultValue GetResultValue(Result result) = 0;
        // The caller *should not* release results obtained via these two methods. The
        // results are guaranteed to be finalized to the corresponding ResultValue, but
        // it's not required from the runtime to return same Result on subsequent calls.
        virtual Result UseZero() = 0;
        virtual Result UseOne() = 0;
    };

} // namespace Quantum
} // namespace Microsoft
