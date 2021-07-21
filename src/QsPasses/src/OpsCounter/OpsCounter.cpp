// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "OpsCounter/OpsCounter.hpp"

#include "Llvm.hpp"

using namespace llvm;

llvm::PassPluginLibraryInfo GetOpsCounterPluginInfo()
{
  return {
      LLVM_PLUGIN_API_VERSION, "OpsCounter", LLVM_VERSION_STRING, [](PassBuilder &pb) {
        pb.registerPipelineParsingCallback([](StringRef name, FunctionPassManager &fpm,
                                              ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
          if (name == "operation-counter")
          {
            fpm.addPass(COpsCounterPrinter(llvm::errs()));
            return true;
          }
          return false;
        });
      }};
}

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
  return GetOpsCounterPluginInfo();
}
