// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <memory>
#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

static QubitAllocationManager::QubitAllocationManagerPtr QubitAllocationManager::createNew()
{
  QubitAllocationManagerPtr ret;
  ret.reset(new QubitAllocationManager());

  return ret;
}

void QubitAllocationManager::allocate(String &&name, Index &&size)
{
  MemoryMapping map;
  map.name  = std::move(name);
  map.index = mappings_.size();
  map.size  = std::move(size);

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

Index QubitAllocationManager::getOffset(String const &name) const
{
  auto it = name_to_index_.find(name);
  if (it == name_to_index_.end())
  {
    throw std::runtime_error("Memory segment with name " + name + " not found.");
  }
  auto index = it->second;

  return mappings_[index].start;
}

void QubitAllocationManager::release(String const & /*name*/)
{}

}  // namespace quantum
}  // namespace microsoft
