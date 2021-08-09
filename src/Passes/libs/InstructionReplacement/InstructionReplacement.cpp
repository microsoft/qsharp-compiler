// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "InstructionReplacement/InstructionReplacement.hpp"

#include "Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {

InstructionReplacementPass::InstructionReplacementPass()
{
  using namespace microsoft::quantum::patterns;

  // Shared pointer to be captured in the lambdas of the patterns
  // Note that you cannot capture this as the reference is destroyed upon
  // copy. Since PassInfoMixin requires copy, such a construct would break
  auto alloc_manager = QubitAllocationManager::createNew();

  // Pattern 0 - Find type
  ReplacementRule rule0;

  auto get_element =
      Call("__quantum__rt__array_get_element_ptr_1d", "arrayName"_cap = _, "index"_cap = _);
  rule0.setPattern("cast"_cap = BitCast("getElement"_cap = get_element));
  rule0.setReplacer([alloc_manager](Builder &, Value *, Captures &cap, Replacements &) {
    llvm::errs() << "Identified an access attempt"
                 << "\n";

    auto type = cap["cast"]->getType();

    // This rule only deals with access to arrays of opaque types
    auto ptr_type = llvm::dyn_cast<llvm::PointerType>(type);
    if (ptr_type == nullptr)
    {
      return false;
    }

    auto array = cap["arrayName"];

    llvm::errs() << *array->getType() << " of " << *type << " " << type->isPointerTy() << " "
                 << *type->getPointerElementType() << " " << type->isArrayTy() << "\n";
    return false;
  });
  rules_.emplace_back(std::move(rule0));

  // Pattern 1 - Get array index

  //  auto get_element =
  //      Call("__quantum__rt__array_get_element_ptr_1d", "arrayName"_cap = _, "index"_cap = _);
  auto cast_pattern = BitCast("getElement"_cap = get_element);
  auto load_pattern = Load("cast"_cap = cast_pattern);

  // Rule 1
  ReplacementRule rule1;
  rule1.setPattern(std::move(load_pattern));

  // Replacement details
  rule1.setReplacer(
      [alloc_manager](Builder &builder, Value *val, Captures &cap, Replacements &replacements) {
        // Getting the type pointer
        auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
        if (ptr_type == nullptr)
        {
          return false;
        }

        // Get the index and testing that it is a constant int
        auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["index"]);
        if (cst == nullptr)
        {
          // ... if not, we cannot perform the mapping.
          return false;
        }

        // Computing the index by getting the current index value and offseting by
        // the offset at which the qubit array is allocated.
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
        replacements.push_back({llvm::dyn_cast<Instruction>(cap["getElement"]), nullptr});
        replacements.push_back({llvm::dyn_cast<Instruction>(cap["cast"]), nullptr});

        return true;
      });
  rules_.emplace_back(std::move(rule1));

  // Rule 2 - delete __quantum__rt__array_update_alias_count
  ReplacementRule rule2;
  auto alias_count = std::make_shared<CallPattern>("__quantum__rt__array_update_alias_count");
  rule2.setPattern(alias_count);
  rule2.setReplacer([](Builder &, Value *val, Captures &, Replacements &replacements) {
    replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
    return true;
  });
  rules_.emplace_back(std::move(rule2));

  // Rule 3 - delete __quantum__rt__qubit_release_array
  ReplacementRule rule3;
  auto release_call = std::make_shared<CallPattern>("__quantum__rt__qubit_release_array");
  rule3.setPattern(release_call);
  rule3.setReplacer([](Builder &, Value *val, Captures &, Replacements &replacements) {
    replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
    return true;
  });
  rules_.emplace_back(std::move(rule3));

  // Rule 4 - perform static allocation and delete __quantum__rt__qubit_allocate_array
  ReplacementRule rule4;
  auto            allocate_call = Call("__quantum__rt__qubit_allocate_array", "size"_cap = _);
  rule4.setPattern(std::move(allocate_call));

  rule4.setReplacer(
      [alloc_manager](Builder &, Value *val, Captures &cap, Replacements &replacements) {
        auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
        if (cst == nullptr)
        {
          return false;
        }

        auto llvm_size = cst->getValue();
        auto name      = val->getName().str();
        alloc_manager->allocate(name, llvm_size.getZExtValue());

        replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
        return true;
      });

  rules_.emplace_back(std::move(rule4));

  // Rule 5 - standard array allocation
  ReplacementRule rule5;
  auto            allocate_array_call =
      Call("__quantum__rt__array_create_1d", "elementSize"_cap = _, "size"_cap = _);
  rule5.setPattern(std::move(allocate_array_call));

  rule5.setReplacer(
      [alloc_manager](Builder &, Value *val, Captures &cap, Replacements &replacements) {
        auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
        if (cst == nullptr)
        {
          return false;
        }

        auto llvm_size = cst->getValue();
        alloc_manager->allocate(val->getName().str(), llvm_size.getZExtValue(), true);
        replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
        return true;
      });

  rules_.emplace_back(std::move(rule5));

  // Rule 6 - track stored values

  auto get_target_element = Call("__quantum__rt__array_get_element_ptr_1d",
                                 "targetArrayName"_cap = _, "targetIndex"_cap = _);
  auto get_value_element = Call("__quantum__rt__array_get_element_ptr_1d", "valueArrayName"_cap = _,
                                "targetValue"_cap = _);
  auto target            = BitCast("target"_cap = get_target_element);
  auto value             = BitCast("value"_cap = get_element);

  auto store_pattern = Store(target, value);

  ReplacementRule rule6;
  rule6.setPattern(std::move(store_pattern));

  rule6.setReplacer([alloc_manager](Builder &, Value *, Captures &, Replacements &) {
    llvm::errs() << "Found store pattern"
                 << "\n";
    return false;
  });
  rules_.emplace_back(std::move(rule6));
}

llvm::PreservedAnalyses InstructionReplacementPass::run(llvm::Function &function,
                                                        llvm::FunctionAnalysisManager & /*fam*/)
{
  replacements_.clear();

  // For every instruction in every block, we attempt a match
  // and replace.
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      matchAndReplace(&instr);
    }
  }

  // Applying all replacements
  for (auto it = replacements_.rbegin(); it != replacements_.rend(); ++it)
  {
    // Cheking if have a replacement for the instruction
    if (it->second != nullptr)
    {
      // ... if so, we just replace it,
      llvm::ReplaceInstWithInst(it->first, it->second);
    }
    else
    {
      // ... otherwise we delete the the instruction
      auto instruction = it->first;

      // Removing all uses
      if (!instruction->use_empty())
      {
        instruction->replaceAllUsesWith(llvm::UndefValue::get(instruction->getType()));
      }

      // And finally we delete the instruction
      instruction->eraseFromParent();
    }
  }

  // If we did not change the IR, we report that we preserved all
  if (replacements_.empty())
  {
    return llvm::PreservedAnalyses::all();
  }

  // ... and otherwise, we report that we preserved none.
  return llvm::PreservedAnalyses::none();
}

bool InstructionReplacementPass::isRequired()
{
  return true;
}

bool InstructionReplacementPass::matchAndReplace(Instruction *value)
{
  Captures captures;
  for (auto const &rule : rules_)
  {
    // Checking if the rule is matched and keep track of captured nodes
    if (rule.match(value, captures))
    {

      // If it is matched, we attempt to replace it
      llvm::IRBuilder<> builder{value};
      if (rule.replace(builder, value, captures, replacements_))
      {
        return true;
      }
    }
  }
  return false;
}

}  // namespace quantum
}  // namespace microsoft
