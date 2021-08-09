// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Passes/InstructionReplacement/InstructionReplacement.hpp"

#include <fstream>
#include <iostream>

namespace {
llvm::PassPluginLibraryInfo getInstructionReplacementPluginInfo()
{
  using namespace microsoft::quantum;
  using namespace llvm;

  return {
      LLVM_PLUGIN_API_VERSION, "InstructionReplacement", LLVM_VERSION_STRING, [](PassBuilder &pb) {
        // Registering the pass
        pb.registerPipelineParsingCallback([](StringRef name, FunctionPassManager &fpm,
                                              ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
          if (name == "instruction-replacement")
          {
            fpm.addPass(InstructionReplacementPass());
            return true;
          }

          return false;
        });
      }};
}
}  // namespace

// Interface for loading the plugin
extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
  return getInstructionReplacementPluginInfo();
}
