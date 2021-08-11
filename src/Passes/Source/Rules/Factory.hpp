#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/ReplacementRule.hpp"
#include "Rules/RuleSet.hpp"

#include <memory>

namespace microsoft {
namespace quantum {

struct RuleFactory
{
public:
  using String               = std::string;
  using ReplacementRulePtr   = std::shared_ptr<ReplacementRule>;
  using AllocationManagerPtr = AllocationManager::AllocationManagerPtr;
  using Replacements         = ReplacementRule::Replacements;
  using Captures             = OperandPrototype::Captures;
  using Instruction          = llvm::Instruction;
  using Value                = llvm::Value;
  using Builder              = ReplacementRule::Builder;

  RuleFactory(RuleSet &rule_set);

  /// Generic rules
  void removeFunctionCall(String const &name);

  /// Conventions
  /// @{
  void useStaticQuantumArrayAllocation();
  void useStaticQuantumAllocation();
  void useStaticResultAllocation();
  /// @}

  /// Optimisations
  /// @{
  void optimiseBranchQuatumOne();
  void optimiseBranchQuatumZero();
  /// @}

  /// Disabling by feature
  /// @{
  void disableReferenceCounting();
  void disableAliasCounting();
  void disableStringSupport();
  // TODO:  void disableDynamicQuantumAllocation();
  /// @}

  AllocationManagerPtr qubitAllocationManager() const;
  AllocationManagerPtr resultAllocationManager() const;

private:
  ReplacementRulePtr addRule(ReplacementRule &&rule);

  RuleSet &rule_set_;

  /// Allocation managers
  /// @{
  AllocationManagerPtr qubit_alloc_manager_{nullptr};
  AllocationManagerPtr result_alloc_manager_{nullptr};
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
