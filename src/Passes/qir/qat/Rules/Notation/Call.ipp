#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/Patterns/AnyPattern.hpp"
#include "Rules/Patterns/CallPattern.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
namespace notation {

using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

template <typename... Args>
IOperandPrototypePtr call(std::string const &name, Args... args)
{
  IOperandPrototypePtr              ret = std::make_shared<CallPattern>(name);
  std::vector<IOperandPrototypePtr> arguments{args...};

  // Adding arguments to matching
  for (auto &a : arguments)
  {
    ret->addChild(a);
  }

  // Function name is kept in the last operand
  ret->addChild(std::make_shared<AnyPattern>());

  return ret;
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
