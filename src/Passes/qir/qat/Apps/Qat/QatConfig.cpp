// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Apps/Qat/QatConfig.hpp"
#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    void QatConfig::setup(ConfigurationManager& config)
    {

        config.setSectionName(
            "Base configuration", "Configuration of the quantum adoption tool to execute a specific behaviour.");
        config.addParameter(load_, "load", "Load component.");
        config.addParameter(
            generate_, "apply", "Applies a profile to transform the IR in correspondence with the profile.");
        config.addParameter(validate_, "validate", "Executes the validation procedure.");
        config.addParameter(profile_, "profile", "Sets the profile.");
        config.addParameter(emit_llvm_, "S", "Emits LLVM IR to the standard output.");
        config.addParameter(opt0_, "O0", "Optimisation level 0.");
        config.addParameter(opt1_, "O1", "Optimisation level 1.");
        config.addParameter(opt2_, "O2", "Optimisation level 2.");
        config.addParameter(opt3_, "O3", "Optimisation level 3.");

        config.addParameter(verify_module_, "verify-module", "Verifies the module after transformation.");

        config.addParameter(dump_config_, "dump-config", "Prints the configuration to the standard output.");
    }

    bool QatConfig::shouldGenerate() const
    {
        return generate_;
    }

    bool QatConfig::shouldValidate() const
    {
        return validate_;
    }

    String QatConfig::profile() const
    {
        return profile_;
    }

    bool QatConfig::shouldEmitLlvm() const
    {
        return emit_llvm_;
    }

    bool QatConfig::isOpt0Enabled() const
    {
        return opt0_;
    }

    bool QatConfig::isOpt1Enabled() const
    {
        return opt1_;
    }

    bool QatConfig::isOpt2Enabled() const
    {
        return opt2_;
    }

    bool QatConfig::isOpt3Enabled() const
    {
        return opt3_;
    }

    bool QatConfig::verifyModule() const
    {
        return verify_module_;
    }

    bool QatConfig::isDebugMode() const
    {
        return debug_;
    }

    bool QatConfig::shouldDumpConfig() const
    {
        return dump_config_;
    }

    String QatConfig::load() const
    {
        return load_;
    }

} // namespace quantum
} // namespace microsoft
