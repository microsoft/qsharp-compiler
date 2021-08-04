#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "InstructionReplacement/Pattern.hpp"
#include "Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {
class ReplacementRule
{
public:
  using Captures            = OperandPrototype::Captures;
  using Instruction         = llvm::Instruction;
  using Value               = llvm::Value;
  using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;
  using Builder             = llvm::IRBuilder<>;
  using Replacements        = std::vector<std::pair<Instruction *, Instruction *>>;
  using ReplaceFunction     = std::function<bool(Builder &, Value *, Captures &, Replacements &)>;

  /// Rule configuration
  /// @{
  void setPattern(OperandPrototypePtr &&pattern);
  void setReplacer(ReplaceFunction const &replacer);
  /// @}

  /// Operation
  /// @{
  bool match(Value *value, Captures &captures) const;
  bool replace(Builder &builder, Value *value, Captures &captures,
               Replacements &replacements) const;
  /// @}
private:
  OperandPrototypePtr pattern_{nullptr};
  ReplaceFunction     replacer_{nullptr};
};

}  // namespace quantum
}  // namespace microsoft
