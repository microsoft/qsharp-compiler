#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Logging/ILogger.hpp"
#include "RuleTransformationPass/Configuration.hpp"
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
/// The module executes the following steps:
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
class RuleTransformationPass : public llvm::PassInfoMixin<RuleTransformationPass>
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
  using ILoggerPtr           = std::shared_ptr<ILogger>;

  /// Construction and destruction configuration.
  /// @{

  /// Custom default constructor
  explicit RuleTransformationPass(RuleSet &&                                 rule_set,
                                  RuleTransformationPassConfiguration const &config)
    : rule_set_{std::move(rule_set)}
    , config_{config}
  {}

  /// Copy construction is banned.
  RuleTransformationPass(RuleTransformationPass const &) = delete;

  /// We allow move semantics.
  RuleTransformationPass(RuleTransformationPass &&) = default;

  /// Default destruction.
  ~RuleTransformationPass() = default;
  /// @}

  /// Operators
  /// @{

  /// Copy assignment is banned.
  RuleTransformationPass &operator=(RuleTransformationPass const &) = delete;

  /// Move assignement is permitted.
  RuleTransformationPass &operator=(RuleTransformationPass &&) = default;
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

  /// Logger
  /// @{
  void setLogger(ILoggerPtr logger);
  /// @}
private:
  /// Pass configuration
  /// @{
  RuleSet                             rule_set_{};
  RuleTransformationPassConfiguration config_{};
  /// @}

  ILoggerPtr logger_{nullptr};

  /// Generic
  /// @{
  uint64_t depth_{0};
  /// @}

  /// Copy and expand
  /// @{
  RuleSet const_expr_replacements_{};
  /// @}

  /// Dead code
  /// @{
  std::unordered_set<Value *>     active_pieces_{};
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
