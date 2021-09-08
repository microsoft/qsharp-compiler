// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft
{
namespace quantum
{

    void QatConfig::setup(ConfigurationManager& config)
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

    bool QatConfig::generate() const
    {
        return generate_;
    }

    bool QatConfig::validate() const
    {
        return validate_;
    }

    String QatConfig::profile() const
    {
        return profile_;
    }

    bool QatConfig::emitLlvm() const
    {
        return emit_llvm_;
    }

    bool QatConfig::opt0() const
    {
        return opt0_;
    }

    bool QatConfig::opt1() const
    {
        return opt1_;
    }

    bool QatConfig::opt2() const
    {
        return opt2_;
    }

    bool QatConfig::opt3() const
    {
        return opt3_;
    }

    bool QatConfig::verifyModule() const
    {
        return verify_module_;
    }

    bool QatConfig::debug() const
    {
        return debug_;
    }

    bool QatConfig::dumpConfig() const
    {
        return dump_config_;
    }

} // namespace quantum
} // namespace microsoft
