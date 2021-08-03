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
      //      instruction_stack_.push_back();
      if (match(&instr))
      {
        changed = true;
        std::cout << "FOUND REPLACEMENT: " << instr.getNumOperands() << std::endl;
        llvm::errs() << instr << "\n";
        for (uint32_t i = 0; i < instr.getNumOperands(); ++i)
        {
          llvm::errs() << " - " << (*instr.getOperand(i)) << "\n";
        }
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

bool InstructionReplacementPass::match(Value *value) const
{
  for (auto const &pattern : patterns_)
  {
    if (pattern.match(value))
    {
      return true;
    }
  }
  return false;
}

}  // namespace quantum
}  // namespace microsoft
