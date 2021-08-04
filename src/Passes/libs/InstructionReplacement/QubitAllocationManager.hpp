#pragma once

#include <memory>
#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class QubitAllocationManager
{
public:
  using Index                     = uint64_t;
  using String                    = std::string;
  using QubitAllocationManagerPtr = std::shared_ptr<QubitAllocationManager>;

  struct MemoryMapping
  {
    String name{""};
    Index  index{0};
    Index  size{0};
    Index  start{0};
    Index  end{0};  ///< Index not included in memory segment
  };
  using NameToIndex = std::unordered_map<String, Index>;
  using Mappings    = std::vector<MemoryMapping>;

  static QubitAllocationManagerPtr createNew()
  {
    QubitAllocationManagerPtr ret;
    ret.reset(new QubitAllocationManager());

    return ret;
  }

  void allocate(String &&name, Index &&size)
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

  Index getOffset(String const &name) const
  {
    auto it = name_to_index_.find(name);
    if (it == name_to_index_.end())
    {
      throw std::runtime_error("Memory segment with name " + name + " not found.");
    }
    auto index = it->second;

    return mappings_[index].start;
  }

  void release(String const & /*name*/)
  {}

private:
  QubitAllocationManager() = default;

  NameToIndex name_to_index_;
  Mappings    mappings_;
};

}  // namespace quantum
}  // namespace microsoft
