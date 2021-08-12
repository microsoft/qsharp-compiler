#include "Profiles/BaseProfile.hpp"

#include "Llvm/Llvm.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Rules/Factory.hpp"

namespace microsoft {
namespace quantum {

llvm::ModulePassManager BaseProfile::createGenerationModulePass(
    llvm::PassBuilder &pass_builder, llvm::PassBuilder::OptimizationLevel &optimisation_level)
{
  auto functionPassManager = pass_builder.buildFunctionSimplificationPipeline(
      optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, true);

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

  functionPassManager.addPass(TransformationRulePass(std::move(rule_set)));

  // https://llvm.org/docs/NewPassManager.html
  // modulePassManager.addPass(createModuleToCGSCCPassAdaptor(...));
  // InlinerPass()

  auto ret = pass_builder.buildPerModuleDefaultPipeline(llvm::PassBuilder::OptimizationLevel::O1);
  ret.addPass(createModuleToFunctionPassAdaptor(std::move(functionPassManager)));

  return ret;
}

llvm::ModulePassManager BaseProfile::createValidationModulePass(
    llvm::PassBuilder &, llvm::PassBuilder::OptimizationLevel &)
{
  throw std::runtime_error("Validator not implmented yet");
}

}  // namespace quantum
}  // namespace microsoft
