// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace
{
// Interface to plugin
llvm::PassPluginLibraryInfo getQirAllocationAnalysisPluginInfo()
{
    using namespace microsoft::quantum;
    using namespace llvm;

    return {LLVM_PLUGIN_API_VERSION, "QirAllocationAnalysis", LLVM_VERSION_STRING, [](PassBuilder& pb) {
                // Registering a printer for the anaylsis
                pb.registerPipelineParsingCallback(
                    [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
                        if (name == "print<qubit-allocation-analysis>")
                        {
                            fpm.addPass(QirAllocationAnalysisPrinter(llvm::errs()));
                            return true;
                        }
                        return false;
                    });

                pb.registerVectorizerStartEPCallback(
                    [](llvm::FunctionPassManager& fpm, llvm::PassBuilder::OptimizationLevel /*level*/) {
                        fpm.addPass(QirAllocationAnalysisPrinter(llvm::errs()));
                    });

                // Registering the analysis module
                pb.registerAnalysisRegistrationCallback([](FunctionAnalysisManager& fam) {
                    fam.registerPass([] { return QirAllocationAnalysisAnalytics(); });
                });
            }};
}

} // namespace

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
    return getQirAllocationAnalysisPluginInfo();
}
