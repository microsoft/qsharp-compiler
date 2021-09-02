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
///              ┌───────────────────────────────┐
///              │                               │
///              │   Copy and expand functions   │
///              │                               │
///              └───────────────────────────────┘
///                              │
///                              │
///                              ▼
///              ┌───────────────────────────────┐
///              │                               │
///              │     Determine dead blocks     │
///              │                               │
///              └───────────────────────────────┘
///                              │
///                              │
///                              ▼
///              ┌───────────────────────────────┐
///              │                               │
///              │      Simplify phi nodes       │
///              │                               │
///              └───────────────────────────────┘
///                              │
///                              │
///                              ▼
///              ┌───────────────────────────────┐
///              │                               │
///              │       Delete dead code        │
///              │                               │
///              └───────────────────────────────┘
///                              │
///                              │
///                              ▼
///              ┌───────────────────────────────┐
///              │                               │
///              │          Apply ruleset        │
///              │                               │
///              └───────────────────────────────┘
///
class ModuleTransformationPass : public llvm::PassInfoMixin<ModuleTransformationPass>
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
  ModuleTransformationPass(RuleSet &&rule_set)
    : rule_set_{std::move(rule_set)}
  {}
  void Setup();

  /// Copy construction is banned.
  ModuleTransformationPass(ModuleTransformationPass const &) = delete;

  /// We allow move semantics.
  ModuleTransformationPass(ModuleTransformationPass &&) = default;

  /// Default destruction.
  ~ModuleTransformationPass() = default;
  /// @}

  /// Operators
  /// @{

  /// Copy assignment is banned.
  ModuleTransformationPass &operator=(ModuleTransformationPass const &) = delete;

  /// Move assignement is permitted.
  ModuleTransformationPass &operator=(ModuleTransformationPass &&) = default;
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
  void         runDetectDeadCode(llvm::Module &module, llvm::ModuleAnalysisManager &mam);
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

  bool onArrayReferenceUpdate(llvm::Instruction *instruction);
  bool onArrayAllocate(llvm::Instruction *instruction);

  bool onLoad(llvm::Instruction *instruction);
  bool onSave(llvm::Instruction *instruction);

  void addRule(ReplacementRule &&rule);

  Value *resolveAlias(Value *original);

  /// Whether or not this pass is required to run.
  static bool isRequired();
  /// @}

private:
  RuleSet      rule_set_{};
  Replacements replacements_;  ///< Registered replacements to be executed.

  std::unordered_map<Value *, int32_t>       qubit_reference_count_; // TODO: Not used
  std::unique_ptr<llvm::FunctionPassManager> function_pass_manager_; // TODO: Not used

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
};

}  // namespace quantum
}  // namespace microsoft
