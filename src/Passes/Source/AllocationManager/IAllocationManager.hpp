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

class IAllocationManager
{
public:
  using Address              = uint64_t;
  using Index                = uint64_t;
  using String               = std::string;
  using AllocationManagerPtr = std::shared_ptr<IAllocationManager>;

  virtual ~IAllocationManager();
  virtual Address allocate(String const &name = "", Index const &size = 1) = 0;
  virtual void    release(Address const &address)                          = 0;
};

}  // namespace quantum
}  // namespace microsoft
