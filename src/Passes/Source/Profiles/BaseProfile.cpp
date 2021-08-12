#include "Profiles/BaseProfile.hpp"

#include "Llvm/Llvm.hpp"
#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QubitAllocationAnalysis/QubitAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Rules/Factory.hpp"

namespace microsoft {
namespace quantum {

llvm::ModulePassManager BaseProfile::createGenerationModulePass(
    llvm::PassBuilder &pass_builder, llvm::PassBuilder::OptimizationLevel &optimisation_level,
    bool debug)
{
  auto ret = pass_builder.buildPerModuleDefaultPipeline(llvm::PassBuilder::OptimizationLevel::O1);
  // buildPerModuleDefaultPipeline buildModuleOptimizationPipeline
  auto function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
      optimisation_level, llvm::PassBuilder::ThinLTOPhase::PreLink, debug);

  // TODO: Maybe this should be done at a module level
  function_pass_manager.addPass(ExpandStaticAllocationPass());

  RuleSet rule_set;

  // Defining the mapping
  auto factory = RuleFactory(rule_set);

  factory.useStaticQuantumArrayAllocation();
  factory.useStaticQuantumAllocation();
  factory.useStaticResultAllocation();

  factory.optimiseBranchQuatumOne();
  //  factory.optimiseBranchQuatumZero();

  factory.disableReferenceCounting();
  factory.disableAliasCounting();
  factory.disableStringSupport();

  function_pass_manager.addPass(TransformationRulePass(std::move(rule_set)));

  // https://llvm.org/docs/NewPassManager.html
  // modulePassManager.addPass(createModuleToCGSCCPassAdaptor(...));
  // InlinerPass()

  ret.addPass(createModuleToFunctionPassAdaptor(std::move(function_pass_manager)));

  ret.addPass(llvm::AlwaysInlinerPass());

  return ret;
}

llvm::ModulePassManager BaseProfile::createValidationModulePass(
    llvm::PassBuilder &, llvm::PassBuilder::OptimizationLevel &, bool)
{
  throw std::runtime_error("Validator not implmented yet");
}

void BaseProfile::addFunctionAnalyses(FunctionAnalysisManager &fam)
{
  fam.registerPass([] { return QubitAllocationAnalysisAnalytics(); });
}

}  // namespace quantum
}  // namespace microsoft
