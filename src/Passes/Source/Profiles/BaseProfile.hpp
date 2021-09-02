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

    /// BaseProfile defines a profile that configures the ruleset used by the Profile
    /// pass. This profile is useful for generating dynamic profiles and is well suited for testing
    /// purposes or YAML configured transformation of the IR.
    class BaseProfile : public IProfile
    {
      public:
        using ConfigureFunction = std::function<void(RuleSet&)>;

        /// @{
        /// The constructor takes a lambda function which configures the ruleset. This
        /// function is invoked during the creation of the generation module.
        BaseProfile();
        /// @}

        /// Interface functions
        /// @{

        /// Creates a new module pass using the ConfigureFunction passed to the constructor of this
        /// profile.
        llvm::ModulePassManager createGenerationModulePass(
            PassBuilder&             pass_builder,
            OptimizationLevel const& optimisation_level,
            bool                     debug) override;

        /// Currently not supported. This function throws an exception.
        llvm::ModulePassManager createValidationModulePass(
            PassBuilder&             pass_builder,
            OptimizationLevel const& optimisation_level,
            bool                     debug) override;

        /// Currently not supported. This function throws an exception.
        void addFunctionAnalyses(FunctionAnalysisManager& fam) override;

        /// @}
      private:
    };

} // namespace quantum
} // namespace microsoft
