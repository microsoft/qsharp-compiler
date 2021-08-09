#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/RuleSet.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class InstructionReplacementPass : public llvm::PassInfoMixin<InstructionReplacementPass>
{
public:
  using Replacements         = ReplacementRule::Replacements;
  using Instruction          = llvm::Instruction;
  using Rules                = std::vector<ReplacementRule>;
  using Value                = llvm::Value;
  using Builder              = ReplacementRule::Builder;
  using AllocationManagerPtr = AllocationManager::AllocationManagerPtr;

  /// Constructors and destructors
  /// @{
  InstructionReplacementPass()                                   = default;
  InstructionReplacementPass(InstructionReplacementPass const &) = delete;
  InstructionReplacementPass(InstructionReplacementPass &&)      = default;
  ~InstructionReplacementPass()                                  = default;
  /// @}

  /// Operators
  /// @{
  InstructionReplacementPass &operator=(InstructionReplacementPass const &) = delete;
  InstructionReplacementPass &operator=(InstructionReplacementPass &&) = default;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  static bool             isRequired();
  /// @}

private:
  RuleSet      rule_set_{};
  Replacements replacements_;  ///< Registered replacements to be executed.
};

}  // namespace quantum
}  // namespace microsoft
