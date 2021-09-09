// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
#include "RuleTransformationPass/Profile.hpp"
#include "Rules/Factory.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

#include <iostream>

namespace microsoft
{
namespace quantum
{

    DefaultProfileGenerator::DefaultProfileGenerator()
      : IProfileGenerator()
    {
        registerProfileComponent<RuleTransformationPassConfiguration>(
            "transformation-rules", [this](RuleTransformationPassConfiguration const& config, IProfileGenerator* ptr) {
                auto& ret = ptr->modulePassManager();

                // Defining the mapping
                RuleSet rule_set;
                auto    factory = RuleFactory(rule_set);
                factory.usingConfiguration(configuration_manager_.get<FactoryConfiguration>());

                // Creating profile pass
                ret.addPass(RuleTransformationPass(std::move(rule_set), config));
            });

        registerProfileComponent<LlvmPassesConfiguration>(
            "llvm-passes", [](LlvmPassesConfiguration const& config, IProfileGenerator* ptr) {
                // Configuring LLVM passes
                if (config.alwaysInline())
                {
                    auto& ret          = ptr->modulePassManager();
                    auto& pass_builder = ptr->passBuilder();
                    ret.addPass(llvm::AlwaysInlinerPass());

                    auto inliner_pass = pass_builder.buildInlinerPipeline(
                        ptr->optimisationLevel(), llvm::PassBuilder::ThinLTOPhase::None, ptr->debug());
                    ret.addPass(std::move(inliner_pass));
                }
            });
    }

    DefaultProfileGenerator::DefaultProfileGenerator(
        ConfigureFunction const&            configure,
        RuleTransformationPassConfiguration profile_pass_config,
        LlvmPassesConfiguration             llvm_config)
      : IProfileGenerator()
    {
        registerProfileComponent<RuleTransformationPassConfiguration>(
            "Transformation rules",
            [configure](RuleTransformationPassConfiguration const& config, IProfileGenerator* ptr) {
                // Defining the mapping
                auto&   ret = ptr->modulePassManager();
                RuleSet rule_set;
                auto    factory = RuleFactory(rule_set);
                configure(rule_set);

                // Creating profile pass
                ret.addPass(RuleTransformationPass(std::move(rule_set), config));
            });

        registerProfileComponent<LlvmPassesConfiguration>(
            "llvm-passes", [](LlvmPassesConfiguration const& config, IProfileGenerator* ptr) {
                // Configuring LLVM passes
                if (config.alwaysInline())
                {
                    auto& ret          = ptr->modulePassManager();
                    auto& pass_builder = ptr->passBuilder();
                    ret.addPass(llvm::AlwaysInlinerPass());

                    auto inliner_pass = pass_builder.buildInlinerPipeline(
                        ptr->optimisationLevel(), llvm::PassBuilder::ThinLTOPhase::None, ptr->debug());
                    ret.addPass(std::move(inliner_pass));
                }
            });

        configuration_manager_.setConfig(profile_pass_config);
        configuration_manager_.setConfig(llvm_config);
    }

    llvm::ModulePassManager DefaultProfileGenerator::createValidationModulePass(
        PassBuilder&             pass_builder,
        OptimizationLevel const& optimisation_level,
        bool)
    {
        return pass_builder.buildPerModuleDefaultPipeline(optimisation_level);
    }

    void DefaultProfileGenerator::addFunctionAnalyses(FunctionAnalysisManager&) {}

    FactoryConfiguration const& DefaultProfileGenerator::factoryConfig() const
    {
        return configuration_manager_.get<FactoryConfiguration>();
    }

    RuleTransformationPassConfiguration const& DefaultProfileGenerator::profilePassConfig() const
    {
        return configuration_manager_.get<RuleTransformationPassConfiguration>();
    }

    LlvmPassesConfiguration const& DefaultProfileGenerator::llvmConfig() const
    {
        return configuration_manager_.get<LlvmPassesConfiguration>();
    }

} // namespace quantum
} // namespace microsoft
