// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/Operands/Any.hpp"
#include "Rules/Operands/Call.hpp"
#include "Rules/Operands/Instruction.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
namespace notation {

using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

IOperandPrototypePtr intToPtr(IOperandPrototypePtr const &arg)
{
  auto cast_pattern = std::make_shared<IntToPtrPattern>();

  cast_pattern->addChild(arg);
  return static_cast<IOperandPrototypePtr>(cast_pattern);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
