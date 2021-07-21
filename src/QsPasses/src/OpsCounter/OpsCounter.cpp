// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "OpsCounter/OpsCounter.hpp"

#include "Llvm.hpp"

using namespace llvm;

namespace
{

void Visitor(Function& f)
{
    errs() << "(operation-counter) " << f.getName() << "\n";
    errs() << "(operation-counter)   number of arguments: " << f.arg_size() << "\n";
}

struct OpsCounterPass : PassInfoMixin<OpsCounterPass>
{
    static auto run(Function& f, FunctionAnalysisManager& /*unused*/) -> PreservedAnalyses // NOLINT
    {
        Visitor(f);

        return PreservedAnalyses::all();
    }
};

class CLegacyOpsCounterPass : public FunctionPass
{
  public:
    static char ID;
    CLegacyOpsCounterPass()
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

llvm::PassPluginLibraryInfo GetOpsCounterPluginInfo()
{
    return {LLVM_PLUGIN_API_VERSION, "OpsCounter", LLVM_VERSION_STRING, [](PassBuilder& pb) {
                pb.registerPipelineParsingCallback(
                    [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
                        if (name == "operation-counter")
                        {
                            fpm.addPass(OpsCounterPass());
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

char                                       CLegacyOpsCounterPass::ID = 0;
static RegisterPass<CLegacyOpsCounterPass> LegacyOpsCounterRegistration(
    "legacy-operation-counter",
    "Gate Counter Pass",
    true,
    false);
