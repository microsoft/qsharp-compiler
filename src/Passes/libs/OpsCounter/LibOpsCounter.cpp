// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include "OpsCounter/OpsCounter.hpp"

#include <fstream>
#include <iostream>

namespace
{
// Interface to plugin
llvm::PassPluginLibraryInfo getOpsCounterPluginInfo()
{
    using namespace microsoft::quantum;
    using namespace llvm;

    return {LLVM_PLUGIN_API_VERSION, "OpsCounter", LLVM_VERSION_STRING, [](PassBuilder& pb) {
                // Registering the printer
                pb.registerPipelineParsingCallback(
                    [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
                        if (name == "print<operation-counter>")
                        {
                            fpm.addPass(OpsCounterPrinter(llvm::errs()));
                            return true;
                        }
                        return false;
                    });

                pb.registerVectorizerStartEPCallback(
                    [](llvm::FunctionPassManager& fpm, llvm::PassBuilder::OptimizationLevel /*level*/) {
                        fpm.addPass(OpsCounterPrinter(llvm::errs()));
                    });

                // Registering the analysis module
                pb.registerAnalysisRegistrationCallback(
                    [](FunctionAnalysisManager& fam) { fam.registerPass([] { return OpsCounterAnalytics(); }); });
            }};
}

} // namespace

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
    return getOpsCounterPluginInfo();
}
