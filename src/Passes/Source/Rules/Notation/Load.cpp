// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/Operands/AnyPattern.hpp"
#include "Rules/Operands/CallPattern.hpp"
#include "Rules/Operands/Instruction.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
namespace notation {

using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

IOperandPrototypePtr load(IOperandPrototypePtr const &arg)
{
  auto ret = std::make_shared<LoadPattern>();

  ret->addChild(arg);
  return static_cast<IOperandPrototypePtr>(ret);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
