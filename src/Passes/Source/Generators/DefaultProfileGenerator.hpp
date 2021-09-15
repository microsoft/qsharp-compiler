#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Generators/IProfileGenerator.hpp"
#include "Generators/LlvmPassesConfig.hpp"
#include "RuleTransformationPass/Configuration.hpp"
#include "Rules/FactoryConfig.hpp"
#include "Rules/RuleSet.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{

    /// DefaultProfileGenerator defines a profile that configures the ruleset used by the Profile
    /// pass. This profile is useful for generating dynamic profiles and is well suited for testing
    /// purposes or YAML configured transformation of the IR.
    class DefaultProfileGenerator : public IProfileGenerator
    {
      public:
        using ConfigureFunction = std::function<void(RuleSet&)>;

        /// @{
        /// The constructor takes a lambda function which configures the ruleset. This
        /// function is invoked during the creation of the generation module.
        explicit DefaultProfileGenerator();
        explicit DefaultProfileGenerator(
            ConfigureFunction const&                   configure,
            RuleTransformationPassConfiguration const& profile_pass_config =
                RuleTransformationPassConfiguration::disable(),
            LlvmPassesConfiguration const& llvm_config = LlvmPassesConfiguration::disable());
        /// @}

        FactoryConfiguration const&                factoryConfig() const;
        RuleTransformationPassConfiguration const& profilePassConfig() const;
        LlvmPassesConfiguration const&             llvmConfig() const;
    };

} // namespace quantum
} // namespace microsoft
