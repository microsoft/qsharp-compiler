// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "OpsCounter/OpsCounter.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>
using namespace llvm;
llvm::AnalysisKey COpsCounterPass::Key;

llvm::PassPluginLibraryInfo GetOpsCounterPluginInfo()
{
  return {
      LLVM_PLUGIN_API_VERSION, "OpsCounter", LLVM_VERSION_STRING, [](PassBuilder &pb) {
        // Registering the printer
        pb.registerPipelineParsingCallback([](StringRef name, FunctionPassManager &fpm,
                                              ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
          if (name == "operation-counter")
          {
            fpm.addPass(COpsCounterPrinter(llvm::errs()));
            return true;
          }
          return false;
        });

        pb.registerVectorizerStartEPCallback(
            [](llvm::FunctionPassManager &fpm, llvm::PassBuilder::OptimizationLevel /*level*/) {
              fpm.addPass(COpsCounterPrinter(llvm::errs()));
            });

        // Registering the analysis module
        pb.registerAnalysisRegistrationCallback([](FunctionAnalysisManager &fam) {
          // TODO: Fails to load if this is present
          fam.registerPass([] { return COpsCounterPass(); });
        });
      }};
}

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
  return GetOpsCounterPluginInfo();
}
