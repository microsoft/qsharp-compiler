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

OperandPrototypePtr Load(OperandPrototypePtr arg)
{
  auto ret = std::make_shared<LoadPattern>();

  ret->addChild(arg);
  return static_cast<OperandPrototypePtr>(ret);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
