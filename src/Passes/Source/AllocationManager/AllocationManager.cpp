// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"

#include <memory>
#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

AllocationManager::AllocationManagerPtr AllocationManager::createNew()
{
  AllocationManagerPtr ret;
  ret.reset(new AllocationManager());

  return ret;
}

AllocationManager::Index AllocationManager::allocate(String const &name, Index const &size)
{
  auto ret = next_qubit_index_;

  // Creating a memory segment mappign in case we are dealing with qubits
  MemoryMapping map;
  map.name  = name;
  map.index = allocation_index_;
  map.size  = size;

  map.start = next_qubit_index_;

  // Advancing start
  next_qubit_index_ += size;
  map.end = map.start + size;

  mappings_.emplace(allocation_index_, std::move(map));

  // Advancing the allocation index
  ++allocation_index_;
  llvm::errs() << "Allocating " << ret << "\n";

  return ret;
}

AllocationManager::Index AllocationManager::getOffset(String const &name) const
{
  throw std::runtime_error("getOffset by name is deprecated: " + name);
}

AllocationManager::Address AllocationManager::getAddress(String const &name, Index const &n) const
{
  return getAddress(getOffset(name), n);
}

AllocationManager::Address AllocationManager::getAddress(Address const &address,
                                                         Index const &  n) const
{
  return address + n;
}

void AllocationManager::release(Address const &address)
{
  --allocation_index_;
  auto it = mappings_.find(allocation_index_);
  if (it == mappings_.end())
  {
    throw std::runtime_error("Segment not found");
  }

  if (it->second.start != address)
  {
    throw std::runtime_error("Address mismatch upon release");
  }

  next_qubit_index_ = it->second.start;
  llvm::errs() << "Releasing " << next_qubit_index_ << "\n";

  mappings_.erase(it);
}

}  // namespace quantum
}  // namespace microsoft
