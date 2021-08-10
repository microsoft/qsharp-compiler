// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/QubitAllocationAnalysis/QubitAllocationAnalysis.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>
#include <unordered_set>

namespace microsoft {
namespace quantum {

QubitAllocationAnalysisAnalytics::Result QubitAllocationAnalysisAnalytics::run(
    llvm::Function &function, llvm::FunctionAnalysisManager & /*unused*/)
{
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      auto call_instr = llvm::dyn_cast<llvm::CallBase>(&instr);
      if (call_instr == nullptr)
      {
        continue;
      }
      auto target_function = call_instr->getCalledFunction();
      auto name            = target_function->getName();
      //      llvm::errs() << "Testing " << name << " : " << *call_instr << "\n";

      if (name == "__quantum__rt__qubit_allocate")
      {
        return {true};
      }

      if (name == "__quantum__rt__qubit_allocate_array")
      {
        return {true};
      }

      if (name == "__quantum__qis__m__body")
      {
        return {true};
      }

      if (name == "__quantum__qis__z__body")
      {
        return {true};
      }
    }
  }

  return {false};
}

QubitAllocationAnalysisPrinter::QubitAllocationAnalysisPrinter(llvm::raw_ostream &out_stream)
  : out_stream_(out_stream)
{}

llvm::PreservedAnalyses QubitAllocationAnalysisPrinter::run(llvm::Function &               function,
                                                            llvm::FunctionAnalysisManager &fam)
{
  auto &result = fam.getResult<QubitAllocationAnalysisAnalytics>(function);

  if (result.value)
  {
    out_stream_ << function.getName() << " contains quantum allocations.\n";
  }
  else
  {
    out_stream_ << function.getName() << " is logic only.\n";
  }
  return llvm::PreservedAnalyses::all();
}

bool QubitAllocationAnalysisPrinter::isRequired()
{
  return true;
}

llvm::AnalysisKey QubitAllocationAnalysisAnalytics::Key;

}  // namespace quantum
}  // namespace microsoft
