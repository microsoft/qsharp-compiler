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

    AllocationManager::AllocationManagerPtr AllocationManager::createNew()
    {
        AllocationManagerPtr ret;
        ret.reset(new AllocationManager());

        return ret;
    }

    AllocationManager::Index AllocationManager::allocate(String const& name, Index const& size)
    {
        auto ret = next_qubit_index_;

        // Creating a memory segment mappign in case we are dealing with qubits
        MemoryMapping map;
        map.name  = name;
        map.index = allocation_index_;
        map.size  = size;

        if (!name.empty())
        {
            if (name_to_index_.find(map.name) != name_to_index_.end())
            {
                throw std::runtime_error("Memory segment with name " + map.name + " already exists.");
            }

            name_to_index_[map.name] = map.index;
        }

        map.start = next_qubit_index_;

        // Advancing start
        next_qubit_index_ += size;
        map.end = map.start + size;

        mappings_.emplace(allocation_index_, std::move(map));

        // Advancing the allocation index
        ++allocation_index_;

        return ret;
    }

    AllocationManager::Index AllocationManager::getOffset(String const& name) const
    {
        auto it = name_to_index_.find(name);
        if (it == name_to_index_.end())
        {
            throw std::runtime_error("Memory segment with name " + name + " not found.");
        }
        auto index = it->second;

        auto it2 = mappings_.find(index);
        if (it2 == mappings_.end())
        {
            throw std::runtime_error(
                "Memory segment with name " + name + " not found - index exist, but not present in mapping.");
        }

        return it2->second.start;
    }

    AllocationManager::Address AllocationManager::getAddress(String const& name, Index const& n) const
    {
        return getAddress(getOffset(name), n);
    }

    AllocationManager::Address AllocationManager::getAddress(Address const& address, Index const& n) const
    {
        return address + n;
    }

    void AllocationManager::release(String const& name)
    {
        auto it = name_to_index_.find(name);
        if (it == name_to_index_.end())
        {
            throw std::runtime_error("Memory segment with name " + name + " not found.");
        }
        name_to_index_.erase(it);

        // TODO(tfr): Address to index
    }

    void AllocationManager::release(Address const&)
    {
        // TODO(tfr): Address to index
        // TODO(tfr): Name to index
    }

} // namespace quantum
} // namespace microsoft
