#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

class ValidationPassConfiguration
{
public:
  // Setup and construction
  //

  /// Setup function that attached the configuration to the ConfigurationManager.
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Validation configuration", "");
    config.addParameter(allow_internal_calls_, "allow-internal-calls",
                        "Whether or not internal calls are allowed.");
  }

  std::unordered_set<std::string> const &allowedOpcodes() const
  {
    return opcodes_;
  }

  std::unordered_set<std::string> const &allowedExternalCallNames() const
  {
    return external_calls_;
  }

  bool allowInternalCalls() const
  {
    return allow_internal_calls_;
  }

private:
  std::unordered_set<std::string> opcodes_{"br", "call", "unreachable", "ret"};
  std::unordered_set<std::string> external_calls_{
      "__quantum__qis__mz__body",    "__quantum__qir__read_result",
      "__quantum__qis__reset__body", "__quantum__qis__z__body",
      "__quantum__qis__s__adj",      "__quantum__qis__dumpregister__body",
      "__quantum__qis__y__body",     "__quantum__qis__x__body",
      "__quantum__qis__t__body",     "__quantum__qis__cz__body",
      "__quantum__qis__s__body",     "__quantum__qis__h__body",
      "__quantum__qis__cnot__body"};

  bool allow_internal_calls_{false};
};

}  // namespace quantum
}  // namespace microsoft
