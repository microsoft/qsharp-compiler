// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ConstSizeArrayAnalysis/ConstSizeArrayAnalysis.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {
ConstSizeArrayAnalysisAnalytics::Result ConstSizeArrayAnalysisAnalytics::run(
    llvm::Function &function, llvm::FunctionAnalysisManager & /*unused*/)
{
  ConstSizeArrayAnalysisAnalytics::Result result;

  // Collect analytics here

  // Use analytics here
  for (auto &basic_block : function)
  {
    for (auto &instruction : basic_block)
    {
      // Skipping debug code
      if (instruction.isDebugOrPseudoInst())
      {
        continue;
      }

      // Checking if it is a call instruction
      auto *call_instr = llvm::dyn_cast<llvm::CallBase>(&instruction);
      if (call_instr == nullptr)
      {
        continue;
      }

      auto target_function = call_instr->getCalledFunction();
      auto name            = target_function->getName();

      // TODO(tfr): Find a better way to inject runtime symbols
      if (name != "__quantum__rt__qubit_allocate_array")
      {
        continue;
      }

      // Validating that there exactly one argument
      if (call_instr->arg_size() != 1)
      {
        continue;
      }

      // Getting the size of the argument
      auto size_value = call_instr->getArgOperand(0);
      if (size_value == nullptr)
      {
        continue;
      }

      // Checking if the value is constant
      auto cst = llvm::dyn_cast<llvm::ConstantInt>(size_value);
      if (cst == nullptr)
      {
        continue;
      }

      result[name] = cst->getValue().getSExtValue();
    }
  }

  return result;
}

ConstSizeArrayAnalysisPrinter::ConstSizeArrayAnalysisPrinter(llvm::raw_ostream &out_stream)
  : out_stream_(out_stream)
{}

llvm::PreservedAnalyses ConstSizeArrayAnalysisPrinter::run(llvm::Function &               function,
                                                           llvm::FunctionAnalysisManager &fam)
{
  auto &results = fam.getResult<ConstSizeArrayAnalysisAnalytics>(function);

  if (!results.empty())
  {
    out_stream_ << function.getName() << "\n";
    out_stream_ << "===================="
                << "\n\n";
    for (auto const &size_info : results)
    {
      out_stream_ << size_info.first() << ": " << size_info.second << "\n";
    }
  }

  return llvm::PreservedAnalyses::all();
}

bool ConstSizeArrayAnalysisPrinter::isRequired()
{
  return true;
}

llvm::AnalysisKey ConstSizeArrayAnalysisAnalytics::Key;

}  // namespace quantum
}  // namespace microsoft
