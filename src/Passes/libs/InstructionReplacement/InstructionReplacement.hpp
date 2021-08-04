#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "InstructionReplacement/Pattern.hpp"
#include "InstructionReplacement/QubitAllocationManager.hpp"
#include "InstructionReplacement/Rule.hpp"
#include "Llvm.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class InstructionReplacementPass : public llvm::PassInfoMixin<InstructionReplacementPass>
{
public:
  using Captures                  = OperandPrototype::Captures;
  using Replacements              = ReplacementRule::Replacements;
  using Instruction               = llvm::Instruction;
  using Rules                     = std::vector<ReplacementRule>;
  using Value                     = llvm::Value;
  using Builder                   = ReplacementRule::Builder;
  using QubitAllocationManagerPtr = QubitAllocationManager::QubitAllocationManagerPtr;

  /// Constructors and destructors
  /// @{
  InstructionReplacementPass();
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

  bool matchAndReplace(Instruction *value);

private:
  Rules        rules_;         ///< Rules that describes QIR mappings
  Replacements replacements_;  ///< Registered replacements to be executed.
};

}  // namespace quantum
}  // namespace microsoft
