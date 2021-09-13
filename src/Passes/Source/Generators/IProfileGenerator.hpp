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
  using PassBuilder             = llvm::PassBuilder;
  using OptimizationLevel       = PassBuilder::OptimizationLevel;
  using FunctionAnalysisManager = llvm::FunctionAnalysisManager;
  using String                  = std::string;
  using SetupFunctionWrapper    = std::function<void(IProfileGenerator *, Profile &)>;
  template <typename R>
  using SetupFunction = std::function<void(R const &, IProfileGenerator *, Profile &)>;
  using Components    = std::vector<std::pair<String, SetupFunctionWrapper>>;

  template <typename R>
  void registerProfileComponent(String const &name, SetupFunction<R> setup)
  {
    configuration_manager_.addConfig<R>();

    auto setup_wrapper = [setup](IProfileGenerator *ptr, Profile &profile) {
      auto &config = ptr->configuration_manager_.get<R>();
      setup(config, ptr, profile);
    };

    components_.push_back({name, std::move(setup_wrapper)});
  }

  IProfileGenerator()  = default;
  ~IProfileGenerator() = default;

  Profile newProfile(OptimizationLevel const &optimisation_level, bool debug);

  llvm::ModulePassManager createGenerationModulePass(Profile &                profile,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug);

  llvm::ModulePassManager createValidationModulePass(PassBuilder &            pass_builder,
                                                     OptimizationLevel const &optimisation_level,
                                                     bool                     debug);

  void addFunctionAnalyses(FunctionAnalysisManager &fam);

  llvm::ModulePassManager &   modulePassManager();
  llvm::PassBuilder &         passBuilder();
  ConfigurationManager &      configurationManager();
  ConfigurationManager const &configurationManager() const;
  OptimizationLevel           optimisationLevel() const;
  bool                        debug() const;

private:
  ConfigurationManager configuration_manager_;
  Components           components_;

  llvm::ModulePassManager *module_pass_manager_{nullptr};
  llvm::PassBuilder *      pass_builder_{nullptr};
  OptimizationLevel        optimisation_level_{OptimizationLevel::O0};
  bool                     debug_{false};
};

}  // namespace quantum
}  // namespace microsoft
