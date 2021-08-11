#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/ReplacementRule.hpp"

#include <memory>

namespace microsoft {
namespace quantum {

struct RuleFactory
{
  using String             = std::string;
  using ReplacementRulePtr = std::shared_ptr<ReplacementRule>;

  /// Single rules

  static ReplacementRulePtr removeFunctionCall(String const &name);

  ///
};

}  // namespace quantum
}  // namespace microsoft
