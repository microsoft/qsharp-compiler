#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "QatTypes/QatTypes.hpp"

namespace microsoft
{
namespace quantum
{

    /// Main configuration class for the qat command-line program.
    class QatConfig
    {
      public:
        // Functions required by configuration manager
        //

        /// Setup function that binds instance variables to the command-line/configuration entries.
        /// This function also provide descriptions of each of the properties below.
        void setup(ConfigurationManager& config);

        // Flags and options
        //

        /// List of dynamic libraries to load.
        String load() const;

        /// Flag that indicates whether or not we are generating a new QIR by applying a profile.
        bool shouldGenerate() const;

        /// Flag to indicate whether or not to verify that the (Q)IR is a valid LLVM IR.
        bool verifyModule() const;

        /// Flag to indicate whether or not to validate the compliance with the QIR profile.
        bool shouldValidate() const;

        /// String to request a specific profile name. Default is base.
        String profile() const;

        /// Indicates whether or not the QIR adaptor tool should emit LLVM IR to the standard output.
        bool shouldEmitLlvm() const;

        /// Tells if the optimisation level 0 is enabled. Note higher OX override lower ones.
        bool isOpt0Enabled() const;

        /// Tells if the optimisation level 1 is enabled. Note higher OX override lower ones.
        bool isOpt1Enabled() const;

        /// Tells if the optimisation level 2 is enabled. Note higher OX override lower ones.
        bool isOpt2Enabled() const;

        /// Tells if the optimisation level 3 is enabled. Note higher OX override lower ones.
        bool isOpt3Enabled() const;

        /// Enables debug output.
        bool isDebugMode() const;

        /// Request the full configuration to be dumped to the screen.
        bool shouldDumpConfig() const;

      private:
        // Variables to be bound to the configuration manager
        //
        String load_{""};
        bool   generate_{false};
        bool   validate_{false};
        String profile_{"generic"};
        bool   emit_llvm_{false};
        bool   opt0_{false};
        bool   opt1_{false};
        bool   opt2_{false};
        bool   opt3_{false};
        bool   verify_module_{false};

        bool debug_{false};
        bool dump_config_{false};
    };
} // namespace quantum
} // namespace microsoft
