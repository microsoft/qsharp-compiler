#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#include "Llvm/Llvm.hpp"

#include <memory>
#include <string>
#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

class AllocationManager
{
public:
  using Index                = uint64_t;
  using String               = std::string;
  using AllocationManagerPtr = std::shared_ptr<AllocationManager>;
  using Resource             = std::vector<llvm::Value *>;
  using Resources            = std::unordered_map<std::string, Resource>;

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

  static AllocationManagerPtr createNew();

  void  allocate(String const &name, Index const &size, bool value_only = false);
  Index getOffset(String const &name) const;
  void  release(String const &name);

  Resource &get(String const &name);

private:
  AllocationManager() = default;

  NameToIndex name_to_index_;
  Mappings    mappings_;

  Resources resources_;
};

}  // namespace quantum
}  // namespace microsoft
