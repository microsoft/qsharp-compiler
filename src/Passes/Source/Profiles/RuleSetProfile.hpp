#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profiles/IProfile.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    class RuleSetProfile : public IProfile
    {
      public:
        using ConfigureFunction = std::function<void(RuleSet&)>;
        RuleSetProfile(ConfigureFunction const& f);
        llvm::ModulePassManager createGenerationModulePass(
            PassBuilder&             pass_builder,
            OptimizationLevel const& optimisation_level,
            bool                     debug) override;
        llvm::ModulePassManager createValidationModulePass(
            PassBuilder&             pass_builder,
            OptimizationLevel const& optimisation_level,
            bool                     debug) override;
        void addFunctionAnalyses(FunctionAnalysisManager& fam) override;

      private:
        ConfigureFunction configure_{};
    };

} // namespace quantum
} // namespace microsoft
