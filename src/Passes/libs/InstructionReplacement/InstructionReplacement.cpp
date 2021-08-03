// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "InstructionReplacement/InstructionReplacement.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {
llvm::PreservedAnalyses InstructionReplacementPass::run(llvm::Function &function,
                                                        llvm::FunctionAnalysisManager & /*fam*/)
{
  // Pass body
  bool changed{false};
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      //      instruction_stack_.push_back();
      if (matchAndReplace(&instr))
      {
        changed = true;
      }
    }
  }

  // llvm::errs() << "Implement your pass here: " << function.getName() << "\n";

  return llvm::PreservedAnalyses::all();
}

bool InstructionReplacementPass::isRequired()
{
  return true;
}

bool InstructionReplacementPass::matchAndReplace(Value *value) const
{
  Captures captures;
  for (auto const &rule : rules_)
  {
    if (rule.match(value, captures))
    {
      if (rule.replace(value, captures))
      {
        return true;
      }
    }
  }
  return false;
}

}  // namespace quantum
}  // namespace microsoft
