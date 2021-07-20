// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "Llvm.hpp"

using namespace llvm;

namespace {

void visitor(Function &F)
{
  errs() << "(gate-counter) " << F.getName() << "\n";
  errs() << "(gate-counter)   number of arguments: " << F.arg_size() << "\n";
}

struct GateCounterPass : PassInfoMixin<GateCounterPass>
{
  PreservedAnalyses run(Function &F, FunctionAnalysisManager &)
  {
    visitor(F);

    return PreservedAnalyses::all();
  }
};

struct LegacyGateCounterPass : public FunctionPass
{
  static char ID;
  LegacyGateCounterPass()
    : FunctionPass(ID)
  {}

  bool runOnFunction(Function &F) override
  {
    visitor(F);
    return false;
  }
};
}  // namespace

llvm::PassPluginLibraryInfo getGateCounterPluginInfo()
{
  return {LLVM_PLUGIN_API_VERSION, "GateCounter", LLVM_VERSION_STRING, [](PassBuilder &PB) {
            PB.registerPipelineParsingCallback([](StringRef Name, FunctionPassManager &FPM,
                                                  ArrayRef<PassBuilder::PipelineElement>) {
              if (Name == "gate-counter")
              {
                FPM.addPass(GateCounterPass());
                return true;
              }
              return false;
            });
          }};
}

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
  return getGateCounterPluginInfo();
}

char                                       LegacyGateCounterPass::ID = 0;
static RegisterPass<LegacyGateCounterPass> LegacyGateCounterRegistration("legacy-gate-counter",
                                                                         "Gate Counter Pass", true,
                                                                         false);
