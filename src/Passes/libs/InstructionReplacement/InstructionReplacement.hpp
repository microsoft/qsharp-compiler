#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#include "InstructionReplacement/Pattern.hpp"
#include "Llvm.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class InstructionReplacementPass : public llvm::PassInfoMixin<InstructionReplacementPass>
{
public:
  using Instruction      = llvm::Instruction;
  using Patterns         = std::vector<Pattern>;
  using InstructionStack = Pattern::InstructionStack;
  using Value            = llvm::Value;

  InstructionReplacementPass()
  {
    Pattern pattern;
    pattern.addPattern(std::make_unique<CallPattern>("__quantum__rt__array_update_alias_count"));
    patterns_.emplace_back(std::move(pattern));
  }

  /// Constructors and destructors
  /// @{
  InstructionReplacementPass(InstructionReplacementPass const &) = default;
  InstructionReplacementPass(InstructionReplacementPass &&)      = default;
  ~InstructionReplacementPass()                                  = default;
  /// @}

  /// Operators
  /// @{
  InstructionReplacementPass &operator=(InstructionReplacementPass const &) = default;
  InstructionReplacementPass &operator=(InstructionReplacementPass &&) = default;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  static bool             isRequired();
  /// @}

  bool match(Value *value) const;

private:
  Patterns         patterns_;
  InstructionStack instruction_stack_{};
};

}  // namespace quantum
}  // namespace microsoft
