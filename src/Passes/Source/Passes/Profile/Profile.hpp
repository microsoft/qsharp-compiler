#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/RuleSet.hpp"

#include <functional>
#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

/// This class applies a set of transformation rules to the IR to transform it into a new IR. The
/// rules are added using the RuleSet class which allows the developer to create one or more rules
/// on how to transform the IR.
///
///
/// The module execute following steps:
///
///
///
///           ┌─────────────────┐
///           │  Apply profile  │
///           └─────────────────┘
///                    │
///                    │
///                    │
///                    │
///                    │                ┌───────────────────────────────┐
///                    │                │                               │
///                    ├───────────────▶│   Copy and expand functions   │──┐
///                    │     clone      │                               │  │
///                    │   functions?   └───────────────────────────────┘  │
///                    │                                │ delete dead      │
///                    │                                │    code?         │
///                    │                                ▼                  │
///                    │                ┌───────────────────────────────┐  │
///                    │                │                               │  │
///                    ├───────────────▶│     Determine active code     │  │
///                    │  delete dead   │                               │  │
///                    │     code?      └───────────────────────────────┘  │
///                    │                                │                  │
///                    │                                │                  │
///                    │                                ▼                  │
///                    │                ┌───────────────────────────────┐  │  leave dead
///                    │                │                               │  │    code?
///                    │                │      Simplify phi nodes       │  │
///                    │                │                               │  │
///                    │                └───────────────────────────────┘  │
///                    │                                │                  │
///                    │                                │                  │
///                    │                                ▼                  │
///                    │                ┌───────────────────────────────┐  │
///                    │                │                               │  │
///                    │                │       Delete dead code        │  │
///                    │                │                               │  │
///                    │                └───────────────────────────────┘  │
///                    │                                │                  │
///                    │                                │                  │
///                    │                                ▼                  │
///                    │                ┌───────────────────────────────┐  │
///                    │    fallback    │                               │  │
///                    └───────────────▶│          Apply rules          │◀─┘
///                                     │                               │
///                                     └───────────────────────────────┘
///
class ProfilePass : public llvm::PassInfoMixin<ProfilePass>
{
public:
  using Replacements         = ReplacementRule::Replacements;
  using Instruction          = llvm::Instruction;
  using Rules                = std::vector<ReplacementRule>;
  using Value                = llvm::Value;
  using Builder              = ReplacementRule::Builder;
  using AllocationManagerPtr = AllocationManager::AllocationManagerPtr;
  using Captures             = RuleSet::Captures;
  using String               = std::string;
  using ConstantArguments    = std::unordered_map<std::string, llvm::ConstantInt *>;

  /// Construction and destruction configuration.
  /// @{

  /// Custom default constructor
  ProfilePass(RuleSet &&rule_set, bool clone_functions = true, bool delete_dead_code = true,
              bool apply_to_inactive_code = false, uint64_t max_recursion = 512)
    : rule_set_{std::move(rule_set)}
    , clone_functions_{clone_functions}
    , delete_dead_code_{delete_dead_code}
    , apply_to_inactive_code_{apply_to_inactive_code}
    , max_recursion_{max_recursion}
  {}

  /// Copy construction is banned.
  ProfilePass(ProfilePass const &) = delete;

  /// We allow move semantics.
  ProfilePass(ProfilePass &&) = default;

  /// Default destruction.
  ~ProfilePass() = default;
  /// @}

  /// Operators
  /// @{

  /// Copy assignment is banned.
  ProfilePass &operator=(ProfilePass const &) = delete;

  /// Move assignement is permitted.
  ProfilePass &operator=(ProfilePass &&) = default;
  /// @}

  /// Implements the transformation analysis which uses the supplied ruleset to make substitutions
  /// in each function.
  llvm::PreservedAnalyses run(llvm::Module &module, llvm::ModuleAnalysisManager &mam);

  using DeletableInstructions = std::vector<llvm::Instruction *>;
  using InstructionModifier = std::function<llvm::Value *(llvm::Value *, DeletableInstructions &)>;

  /// Generic helper funcntions
  /// @{
  bool runOnFunction(llvm::Function &function, InstructionModifier const &modifier);
  void applyReplacements();
  /// @}

  /// Copy and expand
  /// @{
  void            runCopyAndExpand(llvm::Module &module, llvm::ModuleAnalysisManager &mam);
  void            setupCopyAndExpand();
  void            addConstExprRule(ReplacementRule &&rule);
  llvm::Value *   copyAndExpand(llvm::Value *input, DeletableInstructions &);
  llvm::Function *expandFunctionCall(llvm::Function &         callee,
                                     ConstantArguments const &const_args = {});
  void            constantFoldFunction(llvm::Function &callee);
  /// @}

  /// Dead code detection
  /// @{
  void         runDetectActiveCode(llvm::Module &module, llvm::ModuleAnalysisManager &mam);
  void         runDeleteDeadCode(llvm::Module &module, llvm::ModuleAnalysisManager &mam);
  llvm::Value *detectActiveCode(llvm::Value *input, DeletableInstructions &);
  llvm::Value *deleteDeadCode(llvm::Value *input, DeletableInstructions &);
  bool         isActive(llvm::Value *value) const;
  /// @}

  /// @{
  void runReplacePhi(llvm::Module &module, llvm::ModuleAnalysisManager &mam);
  /// @}

  /// Rules
  /// @{
  void runApplyRules(llvm::Module &module, llvm::ModuleAnalysisManager &mam);
  bool onQubitRelease(llvm::Instruction *instruction, Captures &captures);
  bool onQubitAllocate(llvm::Instruction *instruction, Captures &captures);
  /// @}

  /// Whether or not this pass is required to run.
  static bool isRequired();
  /// @}

private:
  /// Pass configuration
  /// @{
  RuleSet      rule_set_{};
  bool clone_functions_{true};
  bool delete_dead_code_{true};
  bool apply_to_inactive_code_{false};
  uint64_t max_recursion_{512};
  /// @}


  /// Generic
  /// @{
  uint64_t     depth_{0};
  /// @}


  /// Copy and expand
  /// @{
  RuleSet const_expr_replacements_{};
  /// @}

  /// Dead code
  /// @{
  std::unordered_set< Value * > active_pieces_{};
  std::vector<llvm::BasicBlock *> blocks_to_delete_;
  std::vector<llvm::Function *>   functions_to_delete_;  
  /// @}

  // Phi detection

  /// Applying rules
  /// @{
  Replacements replacements_;  ///< Registered replacements to be executed.
  /// @}
};

}  // namespace quantum
}  // namespace microsoft
