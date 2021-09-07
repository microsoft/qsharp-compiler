// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profiles/ProfileGenerator.hpp"

#include "Llvm/Llvm.hpp"
#include "ProfilePass/Profile.hpp"
#include "Rules/Factory.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/RuleSet.hpp"

#include <iostream>

namespace microsoft {
namespace quantum {

ProfileGenerator::ProfileGenerator(ConfigurationManager const &configuration)
  : factory_config_{configuration.get<FactoryConfiguration>()}
  , profile_pass_config_{configuration.get<ProfilePassConfiguration>()}
  , llvm_config_{configuration.get<LlvmPassesConfiguration>()}
{}

ProfileGenerator::ProfileGenerator(ConfigureFunction const &configure,
                                   ProfilePassConfiguration profile_pass_config,
                                   LlvmPassesConfiguration  llvm_config)
  : configure_ruleset_{configure}
  , profile_pass_config_{profile_pass_config}
  , llvm_config_{llvm_config}
{}

llvm::ModulePassManager ProfileGenerator::createGenerationModulePass(
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
  ret.addPass(ProfilePass(std::move(rule_set), profile_pass_config_));

  // Configuring LLVM passes
  if (llvm_config_.always_inline)
  {
    ret.addPass(llvm::AlwaysInlinerPass());

    auto inliner_pass = pass_builder.buildInlinerPipeline(
        optimisation_level, llvm::PassBuilder::ThinLTOPhase::None, debug);
    ret.addPass(std::move(inliner_pass));
  }

  return ret;
}

llvm::ModulePassManager ProfileGenerator::createValidationModulePass(
    PassBuilder &pass_builder, OptimizationLevel const &optimisation_level, bool)
{
  return pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
}

void ProfileGenerator::addFunctionAnalyses(FunctionAnalysisManager &)
{}

}  // namespace quantum
}  // namespace microsoft
