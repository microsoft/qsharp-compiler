#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

class ProfilePassConfiguration
{
public:
  void                            setup(ConfigurationManager &config);
  static ProfilePassConfiguration disable();

  bool        deleteDeadCode() const;
  bool        cloneFunctions() const;
  bool        transformExecutionPathOnly() const;
  uint64_t    maxRecursion() const;
  bool        reuseQubits() const;
  bool        groupMeasurements() const;
  bool        oneShotMeasurement() const;
  std::string entryPointAttr() const;

  /// @{
  bool isDisabled() const;
  bool isDefault() const;
  /// @}
private:
  /// @{
  bool delete_dead_code_{true};
  bool clone_functions_{true};
  bool transform_execution_path_only_{true};
  /// @}

  /// Const-expression
  /// @{
  uint64_t max_recursion_{512};
  /// @}

  /// Allocation options
  /// @{
  bool reuse_qubits_{true};  // NOT IMPLEMENTED
  /// @}

  /// Measurement
  /// @{
  bool group_measurements_{false};   // NOT IMPLEMENTED
  bool one_shot_measurement_{true};  // NOT IMPLEMENTED
                                     /// @}

  std::string entry_point_attr_{"EntryPoint"};
};

}  // namespace quantum
}  // namespace microsoft
