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
  replacements_.clear();

  // Pass body
  bool changed{false};
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      /*
      llvm::errs() << instr << "\n";
      for (uint32_t i = 0; i < instr.getNumOperands(); ++i)
      {
        auto op = llvm::dyn_cast<llvm::User>(instr.getOperand(i));
        if (op == nullptr)
        {
          continue;
        }
        llvm::errs() << " - " << *op << "\n";
        for (uint32_t j = 0; j < op->getNumOperands(); ++j)
        {
          auto x = op->getOperand(j);
          if (x == nullptr)
          {
            continue;
          }
          llvm::errs() << "    * " << *x << "\n";
        }
      }

      llvm::errs() << "\n\n";
      */
      if (matchAndReplace(&instr))
      {
        changed = true;
      }
    }
  }

  llvm::errs() << "REPLACEMENTS!" << this << "\n";

  for (auto it = replacements_.rbegin(); it != replacements_.rend(); ++it)
  {
    if (it->second != nullptr)
    {
      llvm::errs() << "Replacing " << *it->first;
      llvm::ReplaceInstWithInst(it->first, it->second);
      llvm::errs() << " with " << *it->second << "\n";
    }
    else
    {
      auto instruction = it->first;
      if (!instruction->use_empty())
      {
        instruction->replaceAllUsesWith(llvm::UndefValue::get(instruction->getType()));
      }
      instruction->eraseFromParent();
    }
  }
  // llvm::errs() << "Implement your pass here: " << function.getName() << "\n";

  return llvm::PreservedAnalyses::none();
}

bool InstructionReplacementPass::isRequired()
{
  return true;
}

bool InstructionReplacementPass::matchAndReplace(Instruction *value)
{
  Captures captures;
  for (auto const &rule : rules_)
  {
    if (rule.match(value, captures))
    {
      llvm::IRBuilder<> builder{value};
      if (rule.replace(builder, value, captures, replacements_))
      {
        return true;
      }
    }
  }
  return false;
}

}  // namespace quantum
}  // namespace microsoft
