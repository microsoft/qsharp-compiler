#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "Profile/Profile.hpp"

namespace microsoft {
namespace quantum {

class IProfileGenerator
{
public:
  // LLVM types
  //
  using PassBuilder             = llvm::PassBuilder;
  using OptimizationLevel       = PassBuilder::OptimizationLevel;
  using FunctionAnalysisManager = llvm::FunctionAnalysisManager;

  // Types used for defining a component
  //
  using String = std::string;

  /// Setup function that uses a configuration type R to
  /// configure the profile and/or generator.
  template <typename R>
  using SetupFunction = std::function<void(R const &, IProfileGenerator *, Profile &)>;

  /// Wrapper function type for invoking the profile setup function
  using SetupFunctionWrapper = std::function<void(IProfileGenerator *, Profile &)>;

  /// List of components to be configured.
  using Components = std::vector<std::pair<String, SetupFunctionWrapper>>;

  // Construction, moves and copies
  //

  IProfileGenerator()                          = default;
  ~IProfileGenerator()                         = default;
  IProfileGenerator(IProfileGenerator const &) = delete;
  IProfileGenerator(IProfileGenerator &&)      = delete;
  IProfileGenerator &operator=(IProfileGenerator const &) = delete;
  IProfileGenerator &operator=(IProfileGenerator &&) = delete;

  // Profile generation interface
  //

  /// Reference to configuration manager. This property allows to access and modify configurations
  /// of the generator. This property is intended for managing the configuration.
  ConfigurationManager &configurationManager();

  /// Constant reference to the configuration manager. This property allows read access to the
  /// configuration manager and is intended for profile generation.
  ConfigurationManager const &configurationManager() const;

  /// Creates a new profile based on the registered components, optimisation level and debug
  /// requirements. The returned profile can be applied to an IR to transform it in accordance with
  /// the configurations given.
  Profile newProfile(OptimizationLevel const &optimisation_level, bool debug);

  // Defining the generator
  //

  /// Registers a new profile component with a given configuration R. The profile component is given
  /// a name and a setup function which is responsible for configuring the profile in accordance
  /// with the configuration.
  template <typename R>
  void registerProfileComponent(String const &name, SetupFunction<R> setup);

  // Support properties for generators
  //

  /// Returns the module pass manager.
  llvm::ModulePassManager &modulePassManager();

  /// Returns the pass builder.
  llvm::PassBuilder &passBuilder();

  /// Returns the optimisation level.
  OptimizationLevel optimisationLevel() const;

  /// Flag indicating whether we are operating in debug mode or not.
  bool debug() const;

protected:
  llvm::ModulePassManager createGenerationModulePass(Profile &                profile,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug);

  llvm::ModulePassManager createValidationModulePass(PassBuilder &            pass_builder,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug);

  void addFunctionAnalyses(FunctionAnalysisManager &fam);

private:
  ConfigurationManager configuration_manager_;
  Components           components_;

  llvm::ModulePassManager *module_pass_manager_{nullptr};
  llvm::PassBuilder *      pass_builder_{nullptr};
  OptimizationLevel        optimisation_level_{OptimizationLevel::O0};
  bool                     debug_{false};
};

template <typename R>
void IProfileGenerator::registerProfileComponent(String const &name, SetupFunction<R> setup)
{
  configuration_manager_.addConfig<R>();

  auto setup_wrapper = [setup](IProfileGenerator *ptr, Profile &profile) {
    auto &config = ptr->configuration_manager_.get<R>();
    setup(config, ptr, profile);
  };

  components_.push_back({name, std::move(setup_wrapper)});
}

}  // namespace quantum
}  // namespace microsoft
