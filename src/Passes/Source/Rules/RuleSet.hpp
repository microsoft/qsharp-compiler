#pragma once

#include "AllocationManager/AllocationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/OperandPrototype.hpp"
#include "Rules/ReplacementRule.hpp"

#include <memory>
#include <vector>

namespace microsoft {
namespace quantum {

class RuleSet
{
public:
  using ReplacementRulePtr   = std::shared_ptr<ReplacementRule>;
  using Rules                = std::vector<ReplacementRulePtr>;
  using Replacements         = ReplacementRule::Replacements;
  using Captures             = OperandPrototype::Captures;
  using Instruction          = llvm::Instruction;
  using Value                = llvm::Value;
  using Builder              = ReplacementRule::Builder;
  using AllocationManagerPtr = AllocationManager::AllocationManagerPtr;

  /// @{
  RuleSet();
  RuleSet(RuleSet const &) = default;
  RuleSet(RuleSet &&)      = default;
  ~RuleSet()               = default;
  /// @}

  /// Operators
  /// @{
  RuleSet &operator=(RuleSet const &) = default;
  RuleSet &operator=(RuleSet &&) = default;
  // TODO(tfr): add RuleSet  operator&(RuleSet const &other);
  /// @}

  bool matchAndReplace(Instruction *value, Replacements &replacements);

private:
  Rules rules_;  ///< Rules that describes QIR mappings
};

}  // namespace quantum
}  // namespace microsoft
