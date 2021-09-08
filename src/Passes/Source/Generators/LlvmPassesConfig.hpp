#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"

namespace microsoft {
namespace quantum {

class LlvmPassesConfiguration
{
public:
  void setup(ConfigurationManager &config);

  static LlvmPassesConfiguration disable();
  bool                           alwaysInline() const;

  bool isDisabled() const;

  bool isDefault() const;

private:
  bool always_inline_{false};
};

}  // namespace quantum
}  // namespace microsoft
