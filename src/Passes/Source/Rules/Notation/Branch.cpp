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

using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;

OperandPrototypePtr Branch(OperandPrototypePtr cond, OperandPrototypePtr arg1,
                           OperandPrototypePtr arg2)
{
  auto branch_pattern = std::make_shared<BranchPattern>();

  branch_pattern->addChild(cond);
  branch_pattern->addChild(arg1);
  branch_pattern->addChild(arg2);

  return static_cast<OperandPrototypePtr>(branch_pattern);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
