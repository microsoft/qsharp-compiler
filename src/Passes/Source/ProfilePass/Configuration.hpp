#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Profiles/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

struct ProfilePassConfiguration
{
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Pass configuration",
                          "Configuration of the pass and its corresponding optimisations.");
    config.addParameter(always_inline, "always-inline", "Aggresively inline function calls.");
    config.addParameter(delete_dead_code, "delete-dead-code", "Deleted dead code");
    config.addParameter(max_recursion, "max-recursion", "max-recursion");
    config.addParameter(reuse_qubits, "reuse-qubits", "reuse-qubits");

    // Not implemented yet
    config.addParameter(group_measurements, "group-measurements",
                        "NOT IMPLEMENTED - group-measurements");
    config.addParameter(one_shot_measurement, "one-shot-measurement",
                        "NOT IMPLEMENTED - one-shot-measurement");
  }
  bool always_inline{false};
  bool delete_dead_code{true};

  /// Const-expression
  /// @{
  int32_t max_recursion{512};
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
};

}  // namespace quantum
}  // namespace microsoft
