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
  auto ret = start_;

  // Creating a memory segment mappign in case we are dealing with qubits
  MemoryMapping map;
  map.name  = name;
  map.index = mappings_.size();
  map.size  = size;

  if (!name.empty())
  {
    if (name_to_index_.find(map.name) != name_to_index_.end())
    {
      throw std::runtime_error("Memory segment with name " + map.name + " already exists.");
    }

    name_to_index_[map.name] = map.index;
  }

  map.start = start_;
  start_ += size;

  map.end = map.start + size;
  mappings_.emplace_back(std::move(map));

  return ret;
}

AllocationManager::Index AllocationManager::getOffset(String const &name) const
{
  auto it = name_to_index_.find(name);
  if (it == name_to_index_.end())
  {
    throw std::runtime_error("Memory segment with name " + name + " not found.");
  }
  auto index = it->second;

  return mappings_[index].start;
}

void AllocationManager::release()
{}

void AllocationManager::release(String const &name)
{
  auto it = name_to_index_.find(name);
  if (it == name_to_index_.end())
  {
    throw std::runtime_error("Memory segment with name " + name + " not found.");
  }
  name_to_index_.erase(it);

  auto it2 = resources_.find(name);
  if (it2 == resources_.end())
  {
    throw std::runtime_error("Resource with name " + name + " does not exists.");
  }
  resources_.erase(it2);
}

}  // namespace quantum
}  // namespace microsoft
