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

IOperandPrototypePtr callByNameOnly(std::string const &name)
{
  IOperandPrototypePtr ret = std::make_shared<CallPattern>(name);
  return ret;
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
