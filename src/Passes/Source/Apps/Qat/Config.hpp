#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {
struct QatConfig
{
  using String = std::string;

  void setup(ConfigurationManager &config)
  {

    config.setSectionName(
        "Base configuration",
        "Configuration of the quantum adoption tool to execute a specific behaviour.");
    config.addParameter(generate, "generate",
                        "Transforms the IR in correspondance with the specified transformation.");
    config.addParameter(validate, "validate", "Executes the validation produre.");
    config.addParameter(profile, "profile", "Sets the profile.");
    config.addParameter(emit_llvm, "S", "Emits LLVM IR to the standard output.");
    config.addParameter(opt0, "O0", "Optimisation level 0.");
    config.addParameter(opt1, "O1", "Optimisation level 1.");
    config.addParameter(opt2, "O2", "Optimisation level 2.");
    config.addParameter(opt3, "O3", "Optimisation level 3.");
    config.addParameter(verify_module, "verify-module",
                        "Verifies the module after transformation.");

    config.addParameter(dump_config, "dump-config",
                        "Prints the configuration to the standard output.");
  }

  bool   generate{false};
  bool   validate{false};
  String profile{"baseProfile"};
  bool   emit_llvm{false};
  bool   opt0{false};
  bool   opt1{false};
  bool   opt2{false};
  bool   opt3{false};
  bool   verify_module{false};

  bool debug{false};
  bool dump_config{false};
};
}  // namespace quantum
}  // namespace microsoft
