#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/Patterns/AnyPattern.hpp"
#include "Rules/Patterns/Instruction.hpp"
#include "Rules/Patterns/PhiPattern.hpp"
#include "Rules/ReplacementRule.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
namespace notation {

using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

template <typename... Args>
IOperandPrototypePtr phi(Args... args)
{
  IOperandPrototypePtr              ret = std::make_shared<PhiPattern>();
  std::vector<IOperandPrototypePtr> arguments{args...};

  // Adding arguments to matching
  for (auto &a : arguments)
  {
    ret->addChild(a);
  }

  return ret;
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
