#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

struct ProfilePassConfiguration
{
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Pass configuration",
                          "Configuration of the pass and its corresponding optimisations.");
    config.addParameter(delete_dead_code, "delete-dead-code", "Deleted dead code.");
    config.addParameter(clone_functions, "clone-functions",
                        "Clone functions to ensure correct qubit allocation.");

    config.addParameter(max_recursion, "max-recursion", "max-recursion");
    config.addParameter(reuse_qubits, "reuse-qubits", "reuse-qubits");

    config.addParameter(entry_point_attr, "entry-point-attr",
                        "Specifies the attribute indicating the entry point.");

    // Not implemented yet
    config.addParameter(group_measurements, "group-measurements",
                        "NOT IMPLEMENTED - group-measurements");
    config.addParameter(one_shot_measurement, "one-shot-measurement",
                        "NOT IMPLEMENTED - one-shot-measurement");
  }

  static ProfilePassConfiguration disable()
  {
    ProfilePassConfiguration ret;
    ret.delete_dead_code              = false;
    ret.clone_functions               = false;
    ret.transform_execution_path_only = false;
    ret.max_recursion                 = 512;
    ret.reuse_qubits                  = false;
    ret.group_measurements            = false;
    ret.one_shot_measurement          = false;
    return ret;
  }

  /// @{
  bool delete_dead_code{true};
  bool clone_functions{true};
  bool transform_execution_path_only{true};
  /// @}

  /// Const-expression
  /// @{
  uint64_t max_recursion{512};
  /// @}

  /// Allocation options
  /// @{
  bool reuse_qubits{true};  // NOT IMPLEMENTED
  /// @}

  /// Measurement
  /// @{
  bool group_measurements{false};   // NOT IMPLEMENTED
  bool one_shot_measurement{true};  // NOT IMPLEMENTED
                                    /// @}

  std::string entry_point_attr{"EntryPoint"};
};

}  // namespace quantum
}  // namespace microsoft
