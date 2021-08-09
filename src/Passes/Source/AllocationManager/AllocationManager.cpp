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

void AllocationManager::allocate(String const &name, Index const &size, bool value_only)
{
  // Creating an array to store values
  // llvm::errs() << "Allocating " << name << " " << size << "\n";
  if (arrays_.find(name) != arrays_.end())
  {
    throw std::runtime_error("Array with name " + name + " already exists.");
  }

  arrays_[name].resize(size);
  for (auto &v : arrays_[name])
  {
    v = nullptr;
  }

  // Creating a memory segment mappign in case we are dealing with qubits
  if (!value_only)
  {
    MemoryMapping map;
    map.name  = name;
    map.index = mappings_.size();
    map.size  = size;

    if (name_to_index_.find(map.name) != name_to_index_.end())
    {
      throw std::runtime_error("Memory segment with name " + map.name + " already exists.");
    }

    name_to_index_[map.name] = map.index;
    if (!mappings_.empty())
    {
      map.start = mappings_.back().end;
    }

    map.end = map.start + size;
    mappings_.emplace_back(std::move(map));
  }
}

AllocationManager::Array &AllocationManager::get(String const &name)
{
  auto it = arrays_.find(name);
  if (it == arrays_.end())
  {
    throw std::runtime_error("Array with name " + name + " does not exists.");
  }
  return it->second;
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

void AllocationManager::release(String const & /*name*/)
{}

}  // namespace quantum
}  // namespace microsoft
