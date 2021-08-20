// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Profiles/RuleSetProfile.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

#include <iostream>

namespace microsoft
{
namespace quantum
{

    RuleSetProfile::RuleSetProfile(ConfigureFunction const& f)
      : configure_{f}
    {
    }
    llvm::ModulePassManager RuleSetProfile::createGenerationModulePass(
        PassBuilder&             pass_builder,
        OptimizationLevel const& optimisation_level,
        bool                     debug)
    {
        auto ret                   = pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
        auto function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
            optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

        // Defining the mapping
        RuleSet rule_set;
        configure_(rule_set);

        function_pass_manager.addPass(TransformationRulePass(std::move(rule_set)));
        ret.addPass(createModuleToFunctionPassAdaptor(std::move(function_pass_manager)));

        return ret;
    }

    llvm::ModulePassManager RuleSetProfile::createValidationModulePass(PassBuilder&, OptimizationLevel const&, bool)
    {
        throw std::runtime_error("Validator not supported for rule set");
    }

    void RuleSetProfile::addFunctionAnalyses(FunctionAnalysisManager& fam)
    {
        fam.registerPass([] { return QirAllocationAnalysis(); });
    }

} // namespace quantum
} // namespace microsoft
