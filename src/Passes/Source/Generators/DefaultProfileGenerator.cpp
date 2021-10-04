// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
#include "RuleTransformationPass/RulePass.hpp"
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
    {
        registerProfileComponent<RuleTransformationPassConfiguration>(
            "transformation-rules",
            [this](RuleTransformationPassConfiguration const& config, IProfileGenerator* ptr, Profile& profile) {
                auto& ret = ptr->modulePassManager();

                // Defining the mapping
                RuleSet rule_set;
                auto    factory =
                    RuleFactory(rule_set, profile.getQubitAllocationManager(), profile.getResultAllocationManager());
                factory.usingConfiguration(configurationManager().get<FactoryConfiguration>());

                // Creating profile pass
                ret.addPass(RuleTransformationPass(std::move(rule_set), config, &profile));
            });

        registerProfileComponent<LlvmPassesConfiguration>(
            "llvm-passes", [](LlvmPassesConfiguration const& config, IProfileGenerator* ptr, Profile&) {
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
        ConfigureFunction const&                   configure,
        RuleTransformationPassConfiguration const& profile_pass_config,
        LlvmPassesConfiguration const&             llvm_config)
    {
        registerProfileComponent<RuleTransformationPassConfiguration>(
            "transformation-rules",
            [configure](RuleTransformationPassConfiguration const& config, IProfileGenerator* ptr, Profile& profile) {
                // Defining the mapping
                auto&   ret = ptr->modulePassManager();
                RuleSet rule_set;
                auto    factory =
                    RuleFactory(rule_set, profile.getQubitAllocationManager(), profile.getResultAllocationManager());
                configure(rule_set);

                // Creating profile pass
                ret.addPass(RuleTransformationPass(std::move(rule_set), config, &profile));
            });

        registerProfileComponent<LlvmPassesConfiguration>(
            "llvm-passes", [](LlvmPassesConfiguration const& config, IProfileGenerator* ptr, Profile&) {
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

        configurationManager().setConfig(profile_pass_config);
        configurationManager().setConfig(llvm_config);
    }

    FactoryConfiguration const& DefaultProfileGenerator::factoryConfig() const
    {
        return configurationManager().get<FactoryConfiguration>();
    }

    RuleTransformationPassConfiguration const& DefaultProfileGenerator::profilePassConfig() const
    {
        return configurationManager().get<RuleTransformationPassConfiguration>();
    }

    LlvmPassesConfiguration const& DefaultProfileGenerator::llvmConfig() const
    {
        return configurationManager().get<LlvmPassesConfiguration>();
    }

} // namespace quantum
} // namespace microsoft
