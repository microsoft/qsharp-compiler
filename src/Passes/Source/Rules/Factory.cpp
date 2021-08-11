// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Factory.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft {
namespace quantum {
using ReplacementRulePtr = RuleFactory::ReplacementRulePtr;

ReplacementRulePtr RuleFactory::removeFunctionCall(String const &name)
{
  (void)name;
  return nullptr;
}

}  // namespace quantum
}  // namespace microsoft
