// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/Operands/Instruction.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
namespace notation {

using IOperandPrototypePtr = std::shared_ptr<IOperandPrototype>;

IOperandPrototypePtr basicBlock()
{
  auto ret = std::make_shared<BasicBlockPattern>();

  return static_cast<IOperandPrototypePtr>(ret);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
