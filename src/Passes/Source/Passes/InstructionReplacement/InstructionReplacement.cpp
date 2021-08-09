// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/InstructionReplacement/InstructionReplacement.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {

llvm::PreservedAnalyses InstructionReplacementPass::run(llvm::Function &function,
                                                        llvm::FunctionAnalysisManager & /*fam*/)
{
  replacements_.clear();

  // For every instruction in every block, we attempt a match
  // and replace.
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      rule_set_.matchAndReplace(&instr, replacements_);
    }
  }

  // Applying all replacements
  for (auto it = replacements_.rbegin(); it != replacements_.rend(); ++it)
  {
    // Cheking if have a replacement for the instruction
    if (it->second != nullptr)
    {
      // ... if so, we just replace it,
      llvm::ReplaceInstWithInst(it->first, it->second);
    }
    else
    {
      // ... otherwise we delete the the instruction
      auto instruction = it->first;

      // Removing all uses
      if (!instruction->use_empty())
      {
        instruction->replaceAllUsesWith(llvm::UndefValue::get(instruction->getType()));
      }

      // And finally we delete the instruction
      instruction->eraseFromParent();
    }
  }

  // If we did not change the IR, we report that we preserved all
  if (replacements_.empty())
  {
    return llvm::PreservedAnalyses::all();
  }

  // ... and otherwise, we report that we preserved none.
  return llvm::PreservedAnalyses::none();
}

bool InstructionReplacementPass::isRequired()
{
  return true;
}

}  // namespace quantum
}  // namespace microsoft
