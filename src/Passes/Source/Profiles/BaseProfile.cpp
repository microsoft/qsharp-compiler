// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Profiles/BaseProfile.hpp"
#include "Rules/Factory.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    llvm::ModulePassManager BaseProfile::createGenerationModulePass(
        llvm::PassBuilder&                    pass_builder,
        llvm::PassBuilder::OptimizationLevel& optimisation_level,
        bool                                  debug)
    {
        auto ret = pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
        // buildPerModuleDefaultPipeline buildModuleOptimizationPipeline
        auto function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
            optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

        auto inliner_pass =
            pass_builder.buildInlinerPipeline(optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

        // TODO(tfr): Maybe this should be done at a module level
        function_pass_manager.addPass(ExpandStaticAllocationPass());

        RuleSet rule_set;

        // Defining the mapping
        auto factory = RuleFactory(rule_set);

        factory.useStaticQubitArrayAllocation();
        factory.useStaticQubitAllocation();
        factory.useStaticResultAllocation();

        factory.optimiseBranchQuatumOne();
        //  factory.optimiseBranchQuatumZero();

        factory.disableReferenceCounting();
        factory.disableAliasCounting();
        factory.disableStringSupport();

        function_pass_manager.addPass(TransformationRulePass(std::move(rule_set)));

        // Eliminate dead code
        function_pass_manager.addPass(llvm::DCEPass());
        function_pass_manager.addPass(llvm::ADCEPass());

        //  function_pass_manager.addPass(llvm::createCalledValuePropagationPass());
        // function_pass_manager.addPass(createSIFoldOperandsPass());

        // Legacy passes:
        // https://llvm.org/doxygen/group__LLVMCTransformsIPO.html#ga2ebfe3e0c3cca3b457708b4784ba93ff

        // https://llvm.org/docs/NewPassManager.html
        // modulePassManager.addPass(createModuleToCGSCCPassAdaptor(...));
        // InlinerPass()

        // auto &cgpm = inliner_pass.getPM();
        // cgpm.addPass(llvm::ADCEPass());

        // CGPM.addPass(createCGSCCToFunctionPassAdaptor(createFunctionToLoopPassAdaptor(LoopFooPass())));
        // CGPM.addPass(createCGSCCToFunctionPassAdaptor(FunctionFooPass()));

        ret.addPass(createModuleToFunctionPassAdaptor(std::move(function_pass_manager)));

        // TODO(tfr): Not available in 11
        // ret.addPass(llvm::createModuleToCGSCCPassAdaptor(std::move(CGPM)));

        ret.addPass(llvm::AlwaysInlinerPass());
        ret.addPass(std::move(inliner_pass));
        // ret.addPass();
        // CGSCCA pass llvm::InlinerPass()

        return ret;
    }

    llvm::ModulePassManager BaseProfile::createValidationModulePass(
        llvm::PassBuilder&,
        llvm::PassBuilder::OptimizationLevel&,
        bool)
    {
        throw std::runtime_error("Validator not implmented yet");
    }

    void BaseProfile::addFunctionAnalyses(FunctionAnalysisManager& fam)
    {
        fam.registerPass([] { return QirAllocationAnalysisAnalytics(); });
    }

} // namespace quantum
} // namespace microsoft
