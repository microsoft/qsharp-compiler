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

Capture::Capture(std::string const &name)
  : name_{name}
{}

IOperandPrototypePtr Capture::operator=(IOperandPrototypePtr const &other)  // NOLINT
{
  auto ret = other->copy();
  ret->captureAs(name_);
  return ret;
}

Capture operator""_cap(char const *name, std::size_t)
{
  return Capture(name);
}

}  // namespace notation
}  // namespace quantum
}  // namespace microsoft
