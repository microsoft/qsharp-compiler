#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class IProfileGenerator
    {
      public:
        using PassBuilder             = llvm::PassBuilder;
        using OptimizationLevel       = PassBuilder::OptimizationLevel;
        using FunctionAnalysisManager = llvm::FunctionAnalysisManager;

        IProfileGenerator() = default;
        virtual ~IProfileGenerator();
        virtual llvm::ModulePassManager createGenerationModulePass(
            PassBuilder&             pass_builder,
            OptimizationLevel const& optimisation_level,
            bool                     debug) = 0;
        virtual llvm::ModulePassManager createValidationModulePass(
            PassBuilder&             pass_builder,
            OptimizationLevel const& optimisation_level,
            bool                     debug)                                                = 0;
        virtual void addFunctionAnalyses(FunctionAnalysisManager& fam) = 0;
    };

} // namespace quantum
} // namespace microsoft
