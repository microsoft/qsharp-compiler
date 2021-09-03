// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "ProfilePass/Profile.hpp"
#include "Profiles/BaseProfile.hpp"
#include "Rules/Factory.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

#include <iostream>

namespace microsoft
{
namespace quantum
{

    llvm::ModulePassManager BaseProfile::createGenerationModulePass(
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
        auto    factory = RuleFactory(rule_set);

        factory.useStaticQubitArrayAllocation();
        factory.useStaticQubitAllocation();
        factory.useStaticResultAllocation();

        factory.optimiseBranchQuatumOne();
        //  factory.optimiseBranchQuatumZero();

        factory.disableReferenceCounting();
        factory.disableAliasCounting();
        factory.disableStringSupport();

        ret.addPass(ProfilePass(std::move(rule_set)));

        ret.addPass(llvm::AlwaysInlinerPass());
        ret.addPass(std::move(inliner_pass));

        return ret;
    }

    llvm::ModulePassManager BaseProfile::createValidationModulePass(
        PassBuilder&             pass_builder,
        OptimizationLevel const& optimisation_level,
        bool)
    {
        return pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
    }

    void BaseProfile::addFunctionAnalyses(FunctionAnalysisManager&) {}

} // namespace quantum
} // namespace microsoft
