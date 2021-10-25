// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"

#include <memory>
#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    BasicAllocationManager::BasicAllocationManagerPtr BasicAllocationManager::createNew()
    {
        BasicAllocationManagerPtr ret;
        ret.reset(new BasicAllocationManager());

        return ret;
    }

    BasicAllocationManager::Index BasicAllocationManager::allocate(String const& name, Index const& size)
    {
        auto ret = next_qubit_index_;

        // Creating a memory segment mapping in case we are dealing with qubits
        MemoryMapping map;
        map.name  = name;
        map.index = allocation_index_;
        map.size  = size;

        map.start = next_qubit_index_;

        // Advancing start
        next_qubit_index_ += size;
        map.end = map.start + size;

        mappings_.emplace_back(std::move(map));

        // Advancing the allocation index
        ++allocation_index_;

        updateRegistersInUse(registersInUse() + size);

        return ret;
    }

    void BasicAllocationManager::release(Address const& address)
    {
        --allocation_index_;
        auto it = mappings_.begin();

        // Finding the element. Note that we could implement binary
        // search but assume that we are dealing with few allocations
        while (it != mappings_.end() && it->start != address)
        {
            ++it;
        }

        if (it == mappings_.end())
        {
            throw std::runtime_error("Qubit segment not found.");
        }

        if (!reuse_qubits_)
        {
            mappings_.erase(it);
        }
        else
        {
            if (it->size > registersInUse())
            {
                throw std::runtime_error("Attempting to release more qubits than what is currently allocated.");
            }

            // In case we are reusing registers, we update how many we are currently using
            updateRegistersInUse(registersInUse() - it->size);

            mappings_.erase(it);

            // Updating the next qubit index with naive algorithm that guarantees
            // 1. Continuous allocation
            // 2. No overlap in address
            if (mappings_.empty())
            {
                next_qubit_index_ = 0;
            }
            else
            {
                auto& b           = mappings_.back();
                next_qubit_index_ = b.start + b.size;
            }
        }
    }

    void BasicAllocationManager::setReuseRegisters(bool val)
    {
        reuse_qubits_ = val;
    }

    void BasicAllocationManager::reset()
    {
        updateRegistersInUse(0);
        mappings_.clear();
        allocation_index_ = 0;
        next_qubit_index_ = 0;
    }
} // namespace quantum
} // namespace microsoft
