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

Capture::Capture(std::string const &name)
  : name_{name}
{}

OperandPrototypePtr Capture::operator=(OperandPrototypePtr const &other)
{
  auto ret = other->copy();
  ret->enableCapture(name_);
  return ret;
}

Capture operator""_cap(char const *name, std::size_t)
{
  return Capture(name);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
