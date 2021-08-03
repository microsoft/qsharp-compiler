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

    instruction_stack_.clear();
    for (auto &instr : basic_block)
    {
      instruction_stack_.push_back(&instr);
      if (match())
      {
        changed = true;
        std::cout << "FOUND REPLACEMENT" << std::endl;
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

bool InstructionReplacementPass::match() const
{
  for (auto const &pattern : patterns_)
  {
    if (pattern.match(instruction_stack_))
    {
      return true;
    }
  }
  return false;
}

}  // namespace quantum
}  // namespace microsoft
