// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "Llvm.hpp"

using namespace llvm;

namespace
{

void Visitor(Function& f)
{
    errs() << "(gate-counter) " << f.getName() << "\n";
    errs() << "(gate-counter)   number of arguments: " << f.arg_size() << "\n";
}

struct GateCounterPass : PassInfoMixin<GateCounterPass>
{
    static auto run(Function& f, FunctionAnalysisManager& /*unused*/) -> PreservedAnalyses // NOLINT
    {
        Visitor(f);

        return PreservedAnalyses::all();
    }
};

class CLegacyGateCounterPass : public FunctionPass
{
  public:
    static char ID;
    CLegacyGateCounterPass()
        : FunctionPass(ID)
    {
    }

    auto runOnFunction(Function& f) -> bool override
    {
        Visitor(f);
        return false;
    }
};
} // namespace

auto GetGateCounterPluginInfo() -> llvm::PassPluginLibraryInfo
{
    return {LLVM_PLUGIN_API_VERSION, "GateCounter", LLVM_VERSION_STRING, [](PassBuilder& pb) {
                pb.registerPipelineParsingCallback(
                    [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
                        if (name == "gate-counter")
                        {
                            fpm.addPass(GateCounterPass());
                            return true;
                        }
                        return false;
                    });
            }};
}

extern "C" LLVM_ATTRIBUTE_WEAK auto llvmGetPassPluginInfo() -> ::llvm::PassPluginLibraryInfo
{
    return GetGateCounterPluginInfo();
}

char                                        CLegacyGateCounterPass::ID = 0;
static RegisterPass<CLegacyGateCounterPass> LegacyGateCounterRegistration(
    "legacy-gate-counter",
    "Gate Counter Pass",
    true,
    false);
