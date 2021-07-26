// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ConstSizeArrayAnalysis/ConstSizeArrayAnalysis.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {
ConstSizeArrayAnalysisAnalytics::Result ConstSizeArrayAnalysisAnalytics::run(llvm::Function &/*function*/,
                                              llvm::FunctionAnalysisManager & /*unused*/)
{
  ConstSizeArrayAnalysisAnalytics::Result result;

  // Collect analytics here

  return result;
}


ConstSizeArrayAnalysisPrinter::ConstSizeArrayAnalysisPrinter(llvm::raw_ostream& out_stream)
  : out_stream_(out_stream)
{
}

llvm::PreservedAnalyses ConstSizeArrayAnalysisPrinter::run(llvm::Function &               /*function*/,
                                           llvm::FunctionAnalysisManager & /*fam*/)
{
  // auto &results = fam.getResult<ConstSizeArrayAnalysisAnalytics>(function);

  // Use analytics here
  out_stream_ << "Analysis results are printed using this stream\n";

  return llvm::PreservedAnalyses::all();
}

bool ConstSizeArrayAnalysisPrinter::isRequired()
{
  return true;
}

llvm::AnalysisKey ConstSizeArrayAnalysisAnalytics::Key;

}  // namespace quantum
}  // namespace microsoft
