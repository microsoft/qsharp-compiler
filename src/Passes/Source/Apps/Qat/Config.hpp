#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{
    class QatConfig
    {
      public:
        using String = std::string;

        void setup(ConfigurationManager& config)
        {

            config.setSectionName(
                "Base configuration", "Configuration of the quantum adoption tool to execute a specific behaviour.");
            config.addParameter(
                generate_, "generate", "Transforms the IR in correspondance with the specified transformation.");
            config.addParameter(validate_, "validate", "Executes the validation produre.");
            config.addParameter(profile_, "profile", "Sets the profile.");
            config.addParameter(emit_llvm_, "S", "Emits LLVM IR to the standard output.");
            config.addParameter(opt0_, "O0", "Optimisation level 0.");
            config.addParameter(opt1_, "O1", "Optimisation level 1.");
            config.addParameter(opt2_, "O2", "Optimisation level 2.");
            config.addParameter(opt3_, "O3", "Optimisation level 3.");
            config.addParameter(verify_module_, "verify-module", "Verifies the module after transformation.");

            config.addParameter(dump_config_, "dump-config", "Prints the configuration to the standard output.");
        }

        bool generate() const
        {
            return generate_;
        }

        bool validate() const
        {
            return validate_;
        }

        String profile() const
        {
            return profile_;
        }

        bool emitLlvm() const
        {
            return emit_llvm_;
        }

        bool opt0() const
        {
            return opt0_;
        }

        bool opt1() const
        {
            return opt1_;
        }

        bool opt2() const
        {
            return opt2_;
        }

        bool opt3() const
        {
            return opt3_;
        }

        bool verifyModule() const
        {
            return verify_module_;
        }

        bool debug() const
        {
            return debug_;
        }

        bool dumpConfig() const
        {
            return dump_config_;
        }

      private:
        bool   generate_{false};
        bool   validate_{false};
        String profile_{"baseProfile"};
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
