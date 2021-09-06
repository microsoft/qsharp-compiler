#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

struct FactoryConfiguration
{
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Transformation rules",
                          "Rules used to transform instruction sequences in the QIR.");
    config.addParameter(disable_reference_counting, "disable-reference-counting",
                        "Disables reference counting by instruction removal.");

    config.addParameter(disable_reference_counting, "disable-reference-counting",
                        "Disables reference counting by instruction removal.");
    config.addParameter(disable_alias_counting, "disable-alias-counting",
                        "Disables alias counting by instruction removal.");
    config.addParameter(disable_string_support, "disable-string-support",
                        "Disables string support by instruction removal.");
    config.addParameter(
        optimise_branch_quatum_one, "optimise-branch-quatum-one",
        "Maps branching based on quantum measurements compared to one to base profile "
        "type measurement.");
    config.addParameter(
        optimise_branch_quatum_zero, "optimise-branch-quatum-zero",
        "Maps branching based on quantum measurements compared to zero to base profile "
        "type measurement.");
    config.addParameter(use_static_qubit_array_allocation, "use-static-qubit-array-allocation",
                        "Maps allocation of qubit arrays to static array allocation.");
    config.addParameter(use_static_qubit_allocation, "use-static-qubit-allocation",
                        "Maps qubit allocation to static allocation.");
    config.addParameter(use_static_result_allocation, "use-static-result-allocation",
                        "Maps result allocation to static allocation.");
  }

  /// Factory Configuration
  /// @{
  bool disable_reference_counting{true};
  bool disable_alias_counting{true};
  bool disable_string_support{true};
  /// @}

  /// Optimisations
  /// @{
  bool optimise_branch_quatum_one{true};
  bool optimise_branch_quatum_zero{true};
  /// @}

  bool use_static_qubit_array_allocation{true};
  bool use_static_qubit_allocation{true};
  bool use_static_result_allocation{true};
};

}  // namespace quantum
}  // namespace microsoft
