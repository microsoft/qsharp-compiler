// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"
#include "ConstSizeArrayAnalysis/ConstSizeArrayAnalysis.hpp"

#include <fstream>
#include <iostream>

namespace {
// Interface to plugin
llvm::PassPluginLibraryInfo getConstSizeArrayAnalysisPluginInfo()
{
  using namespace microsoft::quantum;
  using namespace llvm;

  return {
      LLVM_PLUGIN_API_VERSION, "ConstSizeArrayAnalysis", LLVM_VERSION_STRING, [](PassBuilder &pb) {
        // Registering a printer for the anaylsis
        pb.registerPipelineParsingCallback([](StringRef name, FunctionPassManager &fpm,
                                              ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
          if (name == "print<{operation-name}>")
          {
            fpm.addPass(ConstSizeArrayAnalysisPrinter(llvm::errs()));
            return true;
          }
          return false;
        });

        pb.registerVectorizerStartEPCallback(
            [](llvm::FunctionPassManager &fpm, llvm::PassBuilder::OptimizationLevel /*level*/) {
              fpm.addPass(ConstSizeArrayAnalysisPrinter(llvm::errs()));
            });

        // Registering the analysis module
        pb.registerAnalysisRegistrationCallback([](FunctionAnalysisManager &fam) {
          fam.registerPass([] { return ConstSizeArrayAnalysisAnalytics(); });
        });
      }};
}

}  // namespace

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
  return getConstSizeArrayAnalysisPluginInfo();
}
