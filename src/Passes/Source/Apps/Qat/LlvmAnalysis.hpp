#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class LlvmAnalyser
    {
      public:
        /// Constructors
        /// @{
        explicit LlvmAnalyser(bool debug);

        // Default construction not allowed as this leads
        // to invalid configuration of the managers.
        LlvmAnalyser() = delete;

        // Copy construction prohibited due to restrictions
        // on the member variables.
        LlvmAnalyser(LlvmAnalyser const&) = delete;

        // Prefer move construction at all times.
        LlvmAnalyser(LlvmAnalyser&&) = default;

        // Default deconstruction.
        ~LlvmAnalyser() = default;
        /// @}

        /// Acccess member functions
        /// @{
        llvm::PassBuilder&             passBuilder();
        llvm::LoopAnalysisManager&     loopAnalysisManager();
        llvm::FunctionAnalysisManager& functionAnalysisManager();
        llvm::CGSCCAnalysisManager&    gsccAnalysisManager();
        llvm::ModuleAnalysisManager&   moduleAnalysisManager();
        /// @}
      private:
        /// Objects used to run a set of passes
        /// @{
        llvm::PassBuilder             pass_builder_;
        llvm::LoopAnalysisManager     loop_analysis_manager_;
        llvm::FunctionAnalysisManager function_analysis_manager_;
        llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
        llvm::ModuleAnalysisManager   module_analysis_manager_;
        /// @}
    };

} // namespace quantum
} // namespace microsoft
