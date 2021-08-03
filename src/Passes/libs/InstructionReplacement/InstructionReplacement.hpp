#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#include "InstructionReplacement/Pattern.hpp"
#include "Llvm.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class InstructionReplacementPass : public llvm::PassInfoMixin<InstructionReplacementPass>
{
public:
  using Captures    = OperandPrototype::Captures;
  using Instruction = llvm::Instruction;
  using Rules       = std::vector<ReplacementRule>;
  using Value       = llvm::Value;

  InstructionReplacementPass()
  {

    auto array_name = std::make_shared<AnyPattern>();
    auto index      = std::make_shared<AnyPattern>();
    array_name->enableCapture("arrayName");
    index->enableCapture("index");

    auto get_element = std::make_shared<CallPattern>("__quantum__rt__array_get_element_ptr_1d");
    get_element->addChild(array_name);
    get_element->addChild(index);
    // Function name is last arg?
    get_element->addChild(std::make_shared<AnyPattern>());

    auto load_pattern = std::make_shared<LoadPattern>();
    auto cast_pattern = std::make_shared<BitCastPattern>();

    cast_pattern->addChild(get_element);
    load_pattern->addChild(cast_pattern);

    ReplacementRule rule1;
    rule1.setPattern(load_pattern);
    rule1.setReplacer([](Value *val, Captures &cap) {
      llvm::errs() << "Found qubit load access operator " << val->getName() << " = "
                   << cap["arrayName"]->getName() << "[" << *cap["index"] << "]\n";
      return true;
    });
    rules_.emplace_back(std::move(rule1));

    /*
    ReplacementRule rule2;
    rule2.setPattern(get_element);
    rules_.emplace_back(std::move(rule2));
    */
  }

  /// Constructors and destructors
  /// @{
  InstructionReplacementPass(InstructionReplacementPass const &) = default;
  InstructionReplacementPass(InstructionReplacementPass &&)      = default;
  ~InstructionReplacementPass()                                  = default;
  /// @}

  /// Operators
  /// @{
  InstructionReplacementPass &operator=(InstructionReplacementPass const &) = default;
  InstructionReplacementPass &operator=(InstructionReplacementPass &&) = default;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  static bool             isRequired();
  /// @}

  bool matchAndReplace(Value *value) const;

private:
  Rules rules_;
};

}  // namespace quantum
}  // namespace microsoft
