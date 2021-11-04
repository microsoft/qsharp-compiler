// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/IAllocationManager.hpp"

namespace microsoft
{
namespace quantum
{

    IAllocationManager::~IAllocationManager() = default;

    uint64_t IAllocationManager::allocationsInUse() const
    {
        return registers_in_use_;
    }

    uint64_t IAllocationManager::maxAllocationsUsed() const
    {
        return max_registers_used_;
    }

    void IAllocationManager::updateRegistersInUse(uint64_t n)
    {
        registers_in_use_ = n;
        if (n > max_registers_used_)
        {
            max_registers_used_ = n;
        }
    }

} // namespace quantum
} // namespace microsoft
