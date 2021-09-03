// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ProfilePass/Profile.hpp"
#include "Profiles/RuleSetProfile.hpp"
#include "Rules/Factory.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

#include <iostream>

namespace microsoft
{
namespace quantum
{

    RuleSetProfile::RuleSetProfile(ConfigureFunction const& configure)
      : configure_{configure}
    {
    }
    llvm::ModulePassManager RuleSetProfile::createGenerationModulePass(
        PassBuilder&             pass_builder,
        OptimizationLevel const& optimisation_level,
        bool                     debug)
    {
        llvm::ModulePassManager ret                   = pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
        auto                    function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
            optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

        auto inliner_pass =
            pass_builder.buildInlinerPipeline(optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

        // Defining the mapping
        RuleSet rule_set;
        configure_(rule_set);

        ret.addPass(ProfilePass(std::move(rule_set), false, false, true));
        //  ret.addPass(llvm::AlwaysInlinerPass());
        //  ret.addPass(std::move(inliner_pass));

        return ret;
    }

    llvm::ModulePassManager RuleSetProfile::createValidationModulePass(
        PassBuilder&             pass_builder,
        OptimizationLevel const& optimisation_level,
        bool)
    {
        return pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
    }

    void RuleSetProfile::addFunctionAnalyses(FunctionAnalysisManager&) {}

} // namespace quantum
} // namespace microsoft
