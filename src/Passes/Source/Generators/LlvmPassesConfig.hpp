#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    class LlvmPassesConfiguration
    {
      public:
        // Setup and pre-fabricated configurations

        /// Setup function that registers the different LLVM passes available via LLVM component.
        void setup(ConfigurationManager& config);

        /// Static function creates a new configuration where all transformations/validation requirements
        /// are disabled. Using this configuration is equivalent to
        static LlvmPassesConfiguration disable();

        // Configuration interpretation
        //

        /// Returns true if the configuration disables all effects of this component. The effect of this
        /// function being true is that registered component should have no effect on transformation
        /// and/or validation of the QIR.
        bool isDisabled() const;

        /// Returns true if the current configuration is equivalent to the default configuration.
        bool isDefault() const;

        // Flags and options
        //

        /// Whether or not the LLVM AlwaysInline pass should be added to the profile.
        bool alwaysInline() const;

      private:
        // Variables that enables or disables the adding of specific passes
        //

        bool always_inline_{false};
    };

} // namespace quantum
} // namespace microsoft
