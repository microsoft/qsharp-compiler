#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Generators/IProfileGenerator.hpp"
#include "Generators/LlvmPassesConfig.hpp"
#include "Llvm/Llvm.hpp"
#include "RuleTransformationPass/Configuration.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/RuleSet.hpp"

namespace microsoft {
namespace quantum {

/// DefaultProfileGenerator defines a profile that configures the ruleset used by the Profile
/// pass. This profile is useful for generating dynamic profiles and is well suited for testing
/// purposes or YAML configured transformation of the IR.
class DefaultProfileGenerator : public IProfileGenerator
{
public:
  using ConfigureFunction = std::function<void(RuleSet &)>;

  /// @{
  /// The constructor takes a lambda function which configures the ruleset. This
  /// function is invoked during the creation of the generation module.
  explicit DefaultProfileGenerator(ConfigurationManager const &configuration);
  explicit DefaultProfileGenerator(
      ConfigureFunction const &           configure,
      RuleTransformationPassConfiguration profile_pass_config =
          RuleTransformationPassConfiguration::disable(),
      LlvmPassesConfiguration llvm_config = LlvmPassesConfiguration::disable());
  /// @}

  /// Interface functions
  /// @{

  /// Creates a new module pass using the ConfigureFunction passed to the constructor of this
  /// profile.
  llvm::ModulePassManager createGenerationModulePass(PassBuilder &            pass_builder,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug) override;

  /// Currently not supported. This function throws an exception.
  llvm::ModulePassManager createValidationModulePass(PassBuilder &            pass_builder,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug) override;

  /// Currently not supported. This function throws an exception.
  void addFunctionAnalyses(FunctionAnalysisManager &fam) override;

  /// @}

  /// @{
  FactoryConfiguration const &               factoryConfig() const;
  RuleTransformationPassConfiguration const &profilePassConfig() const;
  LlvmPassesConfiguration const &            llvmConfig() const;
  /// @}
private:
  FactoryConfiguration factory_config_{};
  ConfigureFunction    configure_ruleset_{nullptr};

  RuleTransformationPassConfiguration profile_pass_config_{};
  LlvmPassesConfiguration             llvm_config_{};
};

}  // namespace quantum
}  // namespace microsoft
