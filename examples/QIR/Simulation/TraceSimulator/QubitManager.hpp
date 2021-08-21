// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "CoreTypes.hpp"

namespace Microsoft
{
namespace Quantum
{
    class QubitManager
    {
        // Keep track of unique qubit ids via simple counter.
        uint64_t nextQubitId = 0;

        // Get the internal ID associated to a qubit object.
        static uint64_t GetQubitId(Qubit qubit)
        {
            return reinterpret_cast<uint64_t>(qubit);
        }

      public:
        Qubit AllocateQubit()
        {
            return reinterpret_cast<Qubit>(this->nextQubitId++);
        }

        void ReleaseQubit(Qubit qubit)
        {
        }

        // Get a human-readable name for the qubit.
        std::string GetQubitName(Qubit qubit)
        {
            return std::to_string(GetQubitId(qubit));
        }
    };

} // namespace Quantum
} // namespace Microsoft
