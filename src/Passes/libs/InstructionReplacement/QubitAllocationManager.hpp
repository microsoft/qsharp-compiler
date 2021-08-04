#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    Index  index{0};  ///< Index of the allocation
    Index  size{0};   ///< Size of memory segment
    Index  start{0};  ///< Start index of memory segment
    Index  end{0};    ///< Index not included in memory segment
  };
  using NameToIndex = std::unordered_map<String, Index>;
  using Mappings    = std::vector<MemoryMapping>;

  static QubitAllocationManagerPtr createNew();

  void  allocate(String &&name, Index &&size);
  Index getOffset(String const &name) const;
  void  release(String const &name);

private:
  QubitAllocationManager() = default;

  NameToIndex name_to_index_;
  Mappings    mappings_;
};

}  // namespace quantum
}  // namespace microsoft
