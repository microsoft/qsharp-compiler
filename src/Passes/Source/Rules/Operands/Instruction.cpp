// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Operands/Instruction.hpp"

#include "Rules/OperandPrototype.hpp"

namespace microsoft {
namespace quantum {

template <typename T>
InstructionPattern<T>::~InstructionPattern() = default;
template <typename T>
bool InstructionPattern<T>::match(Value *instr, Captures &captures) const
{
  auto *load_instr = llvm::dyn_cast<T>(instr);
  if (load_instr == nullptr)
  {
    return fail(instr, captures);
  }

  return success(instr, captures);
}

template <typename T>
typename InstructionPattern<T>::Child InstructionPattern<T>::copy() const
{
  auto ret = std::make_shared<InstructionPattern<T>>();
  ret->copyPropertiesFrom(*this);
  return std::move(ret);
}

// TODO(tfr): This seems to be a bug in LLVM. Template instantiations in
// a single translation unit is not supposed to reinstantiate across other
// translation units.
//
// However, it is suspecious that htis problem has been around since Clang 8.
// so this needs more investigation. For now, this work around suffices
// See
// https://bugs.llvm.org/show_bug.cgi?id=18733
// https://stackoverflow.com/questions/56041900/why-does-explicit-template-instantiation-result-in-weak-template-vtables-warning
// for more information
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wweak-template-vtables"
template class InstructionPattern<llvm::StoreInst>;
template class InstructionPattern<llvm::LoadInst>;
template class InstructionPattern<llvm::BitCastInst>;
template class InstructionPattern<llvm::BranchInst>;
#pragma clang diagnostic pop

}  // namespace quantum
}  // namespace microsoft
