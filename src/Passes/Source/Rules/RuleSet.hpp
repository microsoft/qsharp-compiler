#pragma once

#include "AllocationManager/AllocationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/OperandPrototype.hpp"
#include "Rules/ReplacementRule.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class RuleSet
{
public:
  using Rules                = std::vector<ReplacementRule>;
  using Replacements         = ReplacementRule::Replacements;
  using Captures             = OperandPrototype::Captures;
  using Instruction          = llvm::Instruction;
  using Value                = llvm::Value;
  using Builder              = ReplacementRule::Builder;
  using AllocationManagerPtr = AllocationManager::AllocationManagerPtr;

  /// @{
  RuleSet();
  RuleSet(RuleSet const &) = delete;
  RuleSet(RuleSet &&)      = default;
  ~RuleSet()               = default;
  /// @}

  /// Operators
  /// @{
  RuleSet &operator=(RuleSet const &) = delete;
  RuleSet &operator=(RuleSet &&) = default;
  /// @}

  bool matchAndReplace(Instruction *value, Replacements &replacements);

private:
  Rules rules_;  ///< Rules that describes QIR mappings
};

}  // namespace quantum
}  // namespace microsoft
