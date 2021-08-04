#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#include "InstructionReplacement/Pattern.hpp"
#include "InstructionReplacement/QubitAllocationManager.hpp"
#include "Llvm.hpp"

#include <vector>

namespace microsoft {
namespace quantum {

class InstructionReplacementPass : public llvm::PassInfoMixin<InstructionReplacementPass>
{
public:
  using Captures                  = OperandPrototype::Captures;
  using Replacements              = ReplacementRule::Replacements;
  using Instruction               = llvm::Instruction;
  using Rules                     = std::vector<ReplacementRule>;
  using Value                     = llvm::Value;
  using Builder                   = ReplacementRule::Builder;
  using QubitAllocationManagerPtr = QubitAllocationManager::QubitAllocationManagerPtr;

  InstructionReplacementPass()
    : allocation_manager_{QubitAllocationManager::createNew()}
  {
    auto alloc_manager = allocation_manager_;

    auto array_name = std::make_shared<AnyPattern>();
    auto index      = std::make_shared<AnyPattern>();
    array_name->enableCapture("arrayName");
    index->enableCapture("index");

    auto get_element = std::make_shared<CallPattern>("__quantum__rt__array_get_element_ptr_1d");
    get_element->addChild(array_name);
    get_element->addChild(index);
    get_element->enableCapture("getelement");

    // Function name is last arg?
    get_element->addChild(std::make_shared<AnyPattern>());

    auto load_pattern = std::make_shared<LoadPattern>();
    auto cast_pattern = std::make_shared<BitCastPattern>();

    cast_pattern->addChild(get_element);
    cast_pattern->enableCapture("cast");

    load_pattern->addChild(cast_pattern);

    ReplacementRule rule1;
    rule1.setPattern(load_pattern);
    rule1.setReplacer(
        [alloc_manager](Builder &builder, Value *val, Captures &cap, Replacements &replacements) {
          auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
          if (ptr_type == nullptr)
          {
            llvm::errs() << "Failed to cast type\n";
            return false;
          }

          auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["index"]);
          if (cst == nullptr)
          {
            return false;
          }

          auto llvm_size = cst->getValue();
          auto offset    = alloc_manager->getOffset(cap["arrayName"]->getName().str());

          // Creating a new index APInt that is shifted by the offset of the allocation
          auto idx = llvm::APInt(llvm_size.getBitWidth(), llvm_size.getZExtValue() + offset);

          // Computing offset
          auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

          // TODO(tfr): Understand what the significance of the addressspace is in relation to the
          // QIR. Activate by uncommenting:
          // ptr_type = llvm::PointerType::get(ptr_type->getElementType(), 2);
          auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
          instr->takeName(val);

          // Replacing the instruction with new instruction
          replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

          // Deleting the getelement and cast operations
          replacements.push_back({llvm::dyn_cast<Instruction>(cap["getelement"]), nullptr});
          replacements.push_back({llvm::dyn_cast<Instruction>(cap["cast"]), nullptr});

          return true;
        });
    rules_.emplace_back(std::move(rule1));

    ReplacementRule rule2;
    auto alias_count = std::make_shared<CallPattern>("__quantum__rt__array_update_alias_count");
    rule2.setPattern(alias_count);
    rule2.setReplacer([](Builder &, Value *val, Captures &, Replacements &replacements) {
      replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
      return true;
    });
    rules_.emplace_back(std::move(rule2));

    ReplacementRule rule3;
    auto release_call = std::make_shared<CallPattern>("__quantum__rt__qubit_release_array");
    rule3.setPattern(release_call);
    rule3.setReplacer([](Builder &, Value *val, Captures &, Replacements &replacements) {
      replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
      return true;
    });
    rules_.emplace_back(std::move(rule3));

    ReplacementRule rule4;
    auto allocate_call = std::make_shared<CallPattern>("__quantum__rt__qubit_allocate_array");

    auto size = std::make_shared<AnyPattern>();

    size->enableCapture("size");
    allocate_call->addChild(size);
    allocate_call->addChild(std::make_shared<AnyPattern>());

    rule4.setPattern(allocate_call);

    rule4.setReplacer(
        [alloc_manager](Builder &, Value *val, Captures &cap, Replacements &replacements) {
          auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
          if (cst == nullptr)
          {
            return false;
          }

          auto llvm_size = cst->getValue();
          alloc_manager->allocate(val->getName().str(), llvm_size.getZExtValue());
          replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
          return true;
        });

    rules_.emplace_back(std::move(rule4));
  }

  /// Constructors and destructors
  /// @{
  InstructionReplacementPass(InstructionReplacementPass const &) = delete;
  InstructionReplacementPass(InstructionReplacementPass &&)      = default;
  ~InstructionReplacementPass()                                  = default;
  /// @}

  /// Operators
  /// @{
  InstructionReplacementPass &operator=(InstructionReplacementPass const &) = delete;
  InstructionReplacementPass &operator=(InstructionReplacementPass &&) = default;
  /// @}

  /// Functions required by LLVM
  /// @{
  llvm::PreservedAnalyses run(llvm::Function &function, llvm::FunctionAnalysisManager &fam);
  static bool             isRequired();
  /// @}

  bool matchAndReplace(Instruction *value);

private:
  Rules        rules_;
  Replacements replacements_;

  QubitAllocationManagerPtr allocation_manager_{nullptr};
};

}  // namespace quantum
}  // namespace microsoft
