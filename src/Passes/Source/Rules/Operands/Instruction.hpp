#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/OperandPrototype.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

template <typename T>
class InstructionPattern : public IOperandPrototype
{
public:
  using IOperandPrototype::IOperandPrototype;
  ~InstructionPattern() override;
  bool  match(Value *instr, Captures &captures) const override;
  Child copy() const override;
};

using StorePattern    = InstructionPattern<llvm::StoreInst>;
using LoadPattern     = InstructionPattern<llvm::LoadInst>;
using BitCastPattern  = InstructionPattern<llvm::BitCastInst>;
using IntToPtrPattern = InstructionPattern<llvm::IntToPtrInst>;
using ConstIntPattern = InstructionPattern<llvm::ConstantInt>;
using BranchPattern   = InstructionPattern<llvm::BranchInst>;

}  // namespace quantum
}  // namespace microsoft
