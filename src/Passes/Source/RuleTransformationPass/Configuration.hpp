#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

class RuleTransformationPassConfiguration
{
public:
  // Setup and construction
  //

  /// Setup function that attached the configuration to the ConfigurationManager.
  void setup(ConfigurationManager &config);

  /// Creates a configuration where all functionality is disabled.
  static RuleTransformationPassConfiguration disable();

  // Configuration classes
  //

  /// Testing whether this is the default configuration.
  bool isDisabled() const;

  /// Tests whether all functionality is disabled for this component.
  bool isDefault() const;

  // Properties
  //

  /// Whether or not the component should delete dead code.
  bool deleteDeadCode() const;

  /// Whether or not the component should clone functions. This is relevant in relation to qubit
  /// allocation if execution paths are expanded.
  bool cloneFunctions() const;

  /// Whether or not we assume that the code does not throw at runtime.
  bool assumeNoExceptions() const;

  /// Whether or not the component should follow the execution path only or it should be applied to
  /// all parts of the code. For statically allocated qubits one generally wants to follow the
  /// execution path whereas it makes more sense to apply to all parts of the code for dynamic qubit
  /// allocation.
  bool transformExecutionPathOnly() const;

  /// The maximum recursion acceptable when unrolling the execution path.
  uint64_t maxRecursion() const;

  /// Whether or not to reuse qubits.
  bool reuseQubits() const;

  /// Whether or not to annotate entry point with the number of qubits they use.
  bool annotateQubitUse() const;

  /// Whether or not the component should attempt to group measurements.
  bool groupMeasurements() const;

  /// Whether or not the target supports measurement (and result interpretation) during the circuit
  /// execution.
  bool oneShotMeasurement() const;

  /// Attribute which indicate that a function is the entry point.
  std::string entryPointAttr() const;

private:
  // Code expansion and trimming
  //

  bool        delete_dead_code_{true};
  bool        clone_functions_{true};
  bool        transform_execution_path_only_{true};
  uint64_t    max_recursion_{512};
  std::string entry_point_attr_{"EntryPoint"};

  // Branching
  bool assume_no_exceptions_{false};

  // Allocation options
  //
  bool reuse_qubits_{true};
  bool annotate_qubit_use_{true};

  // Measurement
  //
  bool group_measurements_{false};
  bool one_shot_measurement_{true};
};

}  // namespace quantum
}  // namespace microsoft
