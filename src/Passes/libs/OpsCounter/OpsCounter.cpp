// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include "OpsCounter/OpsCounter.hpp"

#include <fstream>
#include <iostream>

namespace microsoft
{
namespace quantum
{
    OpsCounterAnalytics::Result OpsCounterAnalytics::run(
        llvm::Function& function,
        llvm::FunctionAnalysisManager& /*unused*/)
    {
        OpsCounterAnalytics::Result opcode_map;
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

    OpsCounterPrinter::OpsCounterPrinter(llvm::raw_ostream& out_stream)
      : out_stream_(out_stream)
    {
    }

    llvm::PreservedAnalyses OpsCounterPrinter::run(llvm::Function& function, llvm::FunctionAnalysisManager& fam)
    {
        auto& opcode_map = fam.getResult<OpsCounterAnalytics>(function);

        out_stream_ << "Stats for '" << function.getName() << "'\n";
        out_stream_ << "===========================\n";

        constexpr auto STR1 = "Opcode";
        constexpr auto STR2 = "# Used";
        out_stream_ << llvm::format("%-15s %-8s\n", STR1, STR2);
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

    bool OpsCounterPrinter::isRequired()
    {
        return true;
    }

    llvm::AnalysisKey OpsCounterAnalytics::Key;

} // namespace quantum
} // namespace microsoft
