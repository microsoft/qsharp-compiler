// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/RuleSet.hpp"

#include "AllocationManager/AllocationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/Factory.hpp"
#include "Rules/ReplacementRule.hpp"

#include <iostream>
#include <vector>
namespace microsoft {
namespace quantum {

bool RuleSet::matchAndReplace(Instruction *value, Replacements &replacements)
{
  Captures captures;
  for (auto const &rule : rules_)
  {
    // Checking if the rule is matched and keep track of captured nodes
    if (rule->match(value, captures))
    {

      // If it is matched, we attempt to replace it
      llvm::IRBuilder<> builder{value};
      if (rule->replace(builder, value, captures, replacements))
      {
        return true;
      }
      else
      {
        captures.clear();
      }
    }
  }
  return false;
}

void RuleSet::addRule(ReplacementRulePtr const &rule)
{
  rules_.push_back(rule);
}

void RuleSet::clear()
{
  rules_.clear();
}
}  // namespace quantum
}  // namespace microsoft
