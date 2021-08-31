#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Rules/RuleSet.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft {
namespace quantum {

/// This class applies a set of transformation rules to the IR to transform it into a new IR. The
/// rules are added using the RuleSet class which allows the developer to create one or more rules
/// on how to transform the IR.
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
  ModuleTransformationPass(llvm::FunctionPassManager &&function_pass_manager)
    : function_pass_manager_{
          std::make_unique<llvm::FunctionPassManager>(std::move(function_pass_manager))}
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

  /// Functions required by LLVM
  /// @{

  /// Implements the transformation analysis which uses the supplied ruleset to make substitutions
  /// in each function.
  llvm::PreservedAnalyses run(llvm::Module &module, llvm::ModuleAnalysisManager &mam);

  bool runOnInstruction(llvm::Instruction *instruction);
  bool runOnOperand(llvm::Value *operand);
  bool runOnFunction(llvm::Function &function);

  bool onQubitRelease(llvm::Instruction *instruction, Captures &captures);
  bool onQubitAllocate(llvm::Instruction *instruction, Captures &captures);

  bool onArrayReferenceUpdate(llvm::Instruction *instruction);
  bool onArrayAllocate(llvm::Instruction *instruction);

  bool onLoad(llvm::Instruction *instruction);
  bool onSave(llvm::Instruction *instruction);

  void addRule(ReplacementRule &&rule);

  /// Function expansion
  /// @{
  llvm::Function *expandFunctionCall(llvm::Function &         callee,
                                     ConstantArguments const &const_args = {});
  void            constantFoldFunction(llvm::Function &callee);
  /// @}

  Value *resolveAlias(Value *original);

  /// Whether or not this pass is required to run.
  static bool isRequired();
  /// @}

private:
  RuleSet      rule_set_{};
  Replacements replacements_;  ///< Registered replacements to be executed.
  uint64_t     depth_{0};

  std::unordered_map<Value *, int32_t>       qubit_reference_count_;
  std::unique_ptr<llvm::FunctionPassManager> function_pass_manager_;
};

}  // namespace quantum
}  // namespace microsoft
