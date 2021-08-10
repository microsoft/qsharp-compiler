// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/TransformationRule/TransformationRule.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {

llvm::PreservedAnalyses TransformationRulePass::run(llvm::Function &function,
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
    auto instr1 = llvm::dyn_cast<llvm::Instruction>(it->first);
    if (instr1 == nullptr)
    {
      llvm::errs() << "; WARNING: cannot deal with non-instruction replacements\n";
      continue;
    }

    // Cheking if have a replacement for the instruction
    if (it->second != nullptr)
    {
      // ... if so, we just replace it,
      auto instr2 = llvm::dyn_cast<llvm::Instruction>(it->second);
      if (instr2 == nullptr)
      {
        llvm::errs() << "; WARNING: cannot replace instruction with non-instruction\n";
        continue;
      }
      llvm::ReplaceInstWithInst(instr1, instr2);
    }
    else
    {
      // ... otherwise we delete the the instruction
      // Removing all uses
      if (!instr1->use_empty())
      {
        instr1->replaceAllUsesWith(llvm::UndefValue::get(instr1->getType()));
      }

      // And finally we delete the instruction
      instr1->eraseFromParent();
    }
  }

  /*
  for (auto &basic_block : function)
  {
    llvm::errs() << "REPLACEMENTS DONE FOR:\n";
    llvm::errs() << basic_block << "\n\n";
  }
  */

  // If we did not change the IR, we report that we preserved all
  if (replacements_.empty())
  {
    return llvm::PreservedAnalyses::all();
  }

  // ... and otherwise, we report that we preserved none.
  return llvm::PreservedAnalyses::none();
}

bool TransformationRulePass::isRequired()
{
  return true;
}

}  // namespace quantum
}  // namespace microsoft
