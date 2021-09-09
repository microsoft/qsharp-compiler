// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"

#include "Llvm/Llvm.hpp"
#include "RuleTransformationPass/Profile.hpp"
#include "Rules/Factory.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/RuleSet.hpp"

#include <iostream>

namespace microsoft {
namespace quantum {

DefaultProfileGenerator::DefaultProfileGenerator(ConfigurationManager *configuration)
  : IProfileGenerator(configuration)
  , factory_config_{configuration->get<FactoryConfiguration>()}
  , profile_pass_config_{configuration->get<RuleTransformationPassConfiguration>()}
  , llvm_config_{configuration->get<LlvmPassesConfiguration>()}
{}

DefaultProfileGenerator::DefaultProfileGenerator(
    ConfigureFunction const &configure, RuleTransformationPassConfiguration profile_pass_config,
    LlvmPassesConfiguration llvm_config)
  : IProfileGenerator()
  , configure_ruleset_{configure}
  , profile_pass_config_{std::move(profile_pass_config)}
  , llvm_config_{llvm_config}
{}

llvm::ModulePassManager DefaultProfileGenerator::createGenerationModulePass(
    PassBuilder &pass_builder, OptimizationLevel const &optimisation_level, bool debug)
{
  llvm::ModulePassManager ret = pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
  auto                    function_pass_manager = pass_builder.buildFunctionSimplificationPipeline(
      optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);

  // Defining the mapping
  RuleSet rule_set;
  if (configure_ruleset_)
  {
    configure_ruleset_(rule_set);
  }
  else
  {
    auto factory = RuleFactory(rule_set);
    factory.usingConfiguration(factory_config_);
  }

  // Creating profile pass
  ret.addPass(RuleTransformationPass(std::move(rule_set), profile_pass_config_));

  // Configuring LLVM passes
  if (llvm_config_.alwaysInline())
  {
    ret.addPass(llvm::AlwaysInlinerPass());

    auto inliner_pass = pass_builder.buildInlinerPipeline(
        optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);
    ret.addPass(std::move(inliner_pass));
  }

  return ret;
}

llvm::ModulePassManager DefaultProfileGenerator::createValidationModulePass(
    PassBuilder &pass_builder, OptimizationLevel const &optimisation_level, bool)
{
  return pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
}

void DefaultProfileGenerator::addFunctionAnalyses(FunctionAnalysisManager &)
{}

FactoryConfiguration const &DefaultProfileGenerator::factoryConfig() const
{
  return factory_config_;
}

RuleTransformationPassConfiguration const &DefaultProfileGenerator::profilePassConfig() const
{
  return profile_pass_config_;
}

LlvmPassesConfiguration const &DefaultProfileGenerator::llvmConfig() const
{
  return llvm_config_;
}
}  // namespace quantum
}  // namespace microsoft
