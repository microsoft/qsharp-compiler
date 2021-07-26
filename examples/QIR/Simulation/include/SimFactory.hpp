// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <memory>
#include <vector>

#include "QirRuntimeApi_I.hpp"

namespace Microsoft
{
namespace Quantum
{
    // Toffoli Simulator
    QIR_SHARED_API std::unique_ptr<IRuntimeDriver> CreateToffoliSimulator();

    // Full State Simulator
    QIR_SHARED_API std::unique_ptr<IRuntimeDriver> CreateFullstateSimulator();

} // namespace Quantum
} // namespace Microsoft