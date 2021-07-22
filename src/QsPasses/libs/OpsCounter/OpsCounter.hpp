#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

class COpsCounterPass : public llvm::AnalysisInfoMixin<COpsCounterPass>
{
  public:
    using Result = llvm::StringMap<unsigned>;

    Result run(llvm::Function& function, llvm::FunctionAnalysisManager& /*unused*/)
    {
        COpsCounterPass::Result opcode_map;
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

  private:
    static llvm::AnalysisKey Key;
    friend struct llvm::AnalysisInfoMixin<COpsCounterPass>;
};

class COpsCounterPrinter : public llvm::PassInfoMixin<COpsCounterPrinter>
{
  public:
    explicit COpsCounterPrinter(llvm::raw_ostream& out_stream)
        : out_stream_(out_stream)
    {
    }

    auto run(llvm::Function& function, llvm::FunctionAnalysisManager& fam) -> llvm::PreservedAnalyses // NOLINT
    {
        auto& opcode_map = fam.getResult<COpsCounterPass>(function);

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

    /*
    TODO(TFR): Documentation suggests that there such be a isRequired, however, comes out as
    unused after compilation
    */

    static bool isRequired()
    {
        return true;
    }

  private:
    llvm::raw_ostream& out_stream_;
};

auto GetOpsCounterPluginInfo() -> llvm::PassPluginLibraryInfo;
