#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

/// Main configuration class for the qat command-line program.
class QatConfig
{
public:
  using String = std::string;

  // Functions required by configuration manager
  //

  /// Setup function that binds instance variables to the command-line/configuration entries.
  /// This function also provide descriptions of each of the properties below.
  void setup(ConfigurationManager &config);

  // Flags and options
  //

  /// List of dynamic libraries to load.
  String load() const;

  /// Flag that indicates whether or not we are generating a new QIR by applying a profile.
  bool generate() const;

  /// Flag to indicate whether or not to verify that the (Q)IR is a valid LLVM IR.
  bool verifyModule() const;

  /// Flag to indicate whether or not to validate the compliance with the QIR profile.
  bool validate() const;

  /// String to request a specific profile name. Default is base.
  String profile() const;

  /// Indicates whether or not the QIR adaptor tool should emit LLVM IR to the standard output.
  bool emitLlvm() const;

  /// Enables optimisation level 0. Note higher OX override lower ones.
  bool opt0() const;

  /// Enables optimisation level 1. Note higher OX override lower ones.
  bool opt1() const;

  /// Enables optimisation level 2. Note higher OX override lower ones.
  bool opt2() const;

  /// Enables optimisation level 3. Note higher OX override lower ones.
  bool opt3() const;

  /// Enables debug output.
  bool debug() const;

  /// Request the full configuration to be dumped to the screen.
  bool dumpConfig() const;

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
}  // namespace quantum
}  // namespace microsoft
