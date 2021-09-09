#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {

class IProfileGenerator
{
public:
  using PassBuilder             = llvm::PassBuilder;
  using OptimizationLevel       = PassBuilder::OptimizationLevel;
  using FunctionAnalysisManager = llvm::FunctionAnalysisManager;
  using String                  = std::string;
  using SetupFunctionWrapper    = std::function<void(llvm::ModulePassManager &)>;
  template <typename R>
  using SetupFunction = std::function<void(R const &, llvm::ModulePassManager &)>;
  using Components    = std::vector<std::pair<String, SetupFunctionWrapper>>;

  template <typename R>
  void registerProfileComponent(String const &name, SetupFunction<R> setup)
  {
    if (configuration_ == nullptr)
    {
      throw std::runtime_error(
          "You cannor register components with generator that does not have a configuration "
          "manager.");
    }

    configuration_->addConfig<R>();

    auto setup_wrapper = [setup, this](llvm::ModulePassManager &module) {
      auto &config = configuration_->get<R>();
      setup(config, module);
    };

    components_.emplace_back({name, std::move(setup_wrapper)});
  }

  IProfileGenerator();
  explicit IProfileGenerator(ConfigurationManager *configuration);

  virtual ~IProfileGenerator();
  virtual llvm::ModulePassManager createGenerationModulePass(
      PassBuilder &pass_builder, OptimizationLevel const &optimisation_level, bool debug) = 0;
  virtual llvm::ModulePassManager createValidationModulePass(
      PassBuilder &pass_builder, OptimizationLevel const &optimisation_level, bool debug) = 0;
  virtual void addFunctionAnalyses(FunctionAnalysisManager &fam)                          = 0;

protected:
  ConfigurationManager *configuration_{nullptr};
  Components            components_;
};

}  // namespace quantum
}  // namespace microsoft
