// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profiles/BaseProfile.hpp"

#include "Llvm/Llvm.hpp"
#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Rules/Factory.hpp"

namespace microsoft {
namespace quantum {

llvm::ModulePassManager BaseProfile::createGenerationModulePass(
    llvm::PassBuilder &pass_builder, llvm::PassBuilder::OptimizationLevel &optimisation_level,
    bool debug)
{
  auto ret = pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
  // buildPerModuleDefaultPipeline buildModuleOptimizationPipeline
  auto function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
      optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

  auto inliner_pass = pass_builder.buildInlinerPipeline(
      optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

  // TODO(QAT-private-issue-29): Determine if static expansion should happen as a module pass
  // instead of a function pass
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

  ret.addPass(createModuleToFunctionPassAdaptor(std::move(function_pass_manager)));

  // TODO(QAT-private-issue-30): Mordernise: Upon upgrading to LLVM 12 or 13, change CGPM to
  // ret.addPass(llvm::createModuleToCGSCCPassAdaptor(std::move(CGPM)));

  ret.addPass(llvm::AlwaysInlinerPass());
  ret.addPass(std::move(inliner_pass));

  return ret;
}

llvm::ModulePassManager BaseProfile::createValidationModulePass(
    llvm::PassBuilder &, llvm::PassBuilder::OptimizationLevel &, bool)
{
  throw std::runtime_error("Validator not implmented yet");
}

void BaseProfile::addFunctionAnalyses(FunctionAnalysisManager &fam)
{
  fam.registerPass([] {
    std::cout << "Registering pass" << std::endl;
    return QirAllocationAnalysis();
  });
}

}  // namespace quantum
}  // namespace microsoft
