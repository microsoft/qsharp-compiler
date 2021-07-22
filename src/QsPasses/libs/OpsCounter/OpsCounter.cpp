// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "OpsCounter/OpsCounter.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>
using namespace llvm;

namespace Microsoft
{
namespace Quantum
{
    COpsCounterAnalytics::Result COpsCounterAnalytics::run(
        llvm::Function& function,
        llvm::FunctionAnalysisManager& /*unused*/)
    {
        COpsCounterAnalytics::Result opcode_map;
        for (auto& basic_block : function)
        {
            for (auto& instruction : basic_block)
            {
                if (instruction.isDebugOrPseudoInst())
                {
                    continue;
                }
                auto name = instruction.getOpcodeName();

                if (opcode_map.find(name) == opcode_map.end())
                {
                    opcode_map[instruction.getOpcodeName()] = 1;
                }
                else
                {
                    opcode_map[instruction.getOpcodeName()]++;
                }
            }
        }

        return opcode_map;
    }

    COpsCounterPrinter::COpsCounterPrinter(llvm::raw_ostream& out_stream)
        : out_stream_(out_stream)
    {
    }

    llvm::PreservedAnalyses COpsCounterPrinter::run(llvm::Function& function, llvm::FunctionAnalysisManager& fam)
    {
        auto& opcode_map = fam.getResult<COpsCounterAnalytics>(function);

        out_stream_ << "Stats for '" << function.getName() << "'\n";
        out_stream_ << "===========================\n";

        constexpr auto str1 = "Opcode";
        constexpr auto str2 = "# Used";
        out_stream_ << llvm::format("%-15s %-8s\n", str1, str2);
        out_stream_ << "---------------------------"
                    << "\n";

        for (auto const& instruction : opcode_map)
        {
            out_stream_ << llvm::format("%-15s %-8lu\n", instruction.first().str().c_str(), instruction.second);
        }
        out_stream_ << "---------------------------"
                    << "\n\n";

        return llvm::PreservedAnalyses::all();
    }

    bool COpsCounterPrinter::isRequired()
    {
        return true;
    }

    llvm::AnalysisKey COpsCounterAnalytics::Key;
} // namespace Quantum
} // namespace Microsoft

// Interface to plugin
namespace
{
llvm::PassPluginLibraryInfo GetOpsCounterPluginInfo()
{
    using namespace Microsoft::Quantum;

    return {
        LLVM_PLUGIN_API_VERSION, "OpsCounter", LLVM_VERSION_STRING,
        [](PassBuilder& pb)
        {
            // Registering the printer
            pb.registerPipelineParsingCallback(
                [](StringRef name, FunctionPassManager& fpm, ArrayRef<PassBuilder::PipelineElement> /*unused*/)
                {
                    if (name == "print<operation-counter>")
                    {
                        fpm.addPass(COpsCounterPrinter(llvm::errs()));
                        return true;
                    }
                    return false;
                });

            pb.registerVectorizerStartEPCallback(
                [](llvm::FunctionPassManager& fpm, llvm::PassBuilder::OptimizationLevel /*level*/)
                { fpm.addPass(COpsCounterPrinter(llvm::errs())); });

            // Registering the analysis module
            pb.registerAnalysisRegistrationCallback([](FunctionAnalysisManager& fam)
                                                    { fam.registerPass([] { return COpsCounterAnalytics(); }); });
        }};
}
} // namespace

extern "C" LLVM_ATTRIBUTE_WEAK ::llvm::PassPluginLibraryInfo llvmGetPassPluginInfo()
{
    return GetOpsCounterPluginInfo();
}
