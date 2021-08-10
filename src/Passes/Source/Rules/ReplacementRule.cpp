// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/ReplacementRule.hpp"

namespace microsoft {
namespace quantum {

ReplacementRule::ReplacementRule(OperandPrototypePtr &&pattern, ReplaceFunction &&replacer)
  : pattern_{std::move(pattern)}
  , replacer_{std::move(replacer)}
{}

void ReplacementRule::setPattern(OperandPrototypePtr &&pattern)
{
  pattern_ = std::move(pattern);
}

void ReplacementRule::setReplacer(ReplaceFunction const &replacer)
{
  replacer_ = replacer;
}

bool ReplacementRule::match(Value *value, Captures &captures) const
{
  if (pattern_ == nullptr)
  {
    return false;
  }

  return pattern_->match(value, captures);
}

bool ReplacementRule::replace(Builder &builder, Value *value, Captures &captures,
                              Replacements &replacements) const
{
  if (replacer_)
  {
    return replacer_(builder, value, captures, replacements);
  }

  return false;
}

}  // namespace quantum
}  // namespace microsoft
