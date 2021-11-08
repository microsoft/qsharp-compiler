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

IOperandPrototypePtr constInt()
{
  auto cast_pattern = std::make_shared<ConstIntPattern>();

  return static_cast<IOperandPrototypePtr>(cast_pattern);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
