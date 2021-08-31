// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/ModuleTransformation/ModuleTransformation.hpp"

#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/ReplacementRule.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {
void ModuleTransformationPass::Setup()
{
  using namespace microsoft::quantum::notation;
  rule_set_.clear();
  addRule({call("__quantum__rt__qubit_update_reference_count", "name"_cap = _, "value"_cap = _),
           [this](Builder &, Value *val, Captures &captures, Replacements &) {
             return onQubitReferenceUpdate(llvm::dyn_cast<llvm::Instruction>(val), captures);
           }});

  addRule({call("__quantum__rt__qubit_allocate"),
           [this](Builder &, Value *val, Captures &captures, Replacements &) {
             return onQubitAllocate(llvm::dyn_cast<llvm::Instruction>(val), captures);
           }});

  // Load and save
  auto get_element =
      call("__quantum__rt__array_get_element_ptr_1d", "arrayName"_cap = _, "index"_cap = _);
  auto cast_pattern = bitCast("getElement"_cap = get_element);
  auto load_pattern = load("cast"_cap = cast_pattern);

  addRule({std::move(load_pattern), [this](Builder &, Value *val, Captures &, Replacements &) {
             return onLoad(llvm::dyn_cast<llvm::Instruction>(val));
           }});

  auto store_pattern = store("cast"_cap = cast_pattern, "value"_cap = _);
  addRule({std::move(store_pattern),
           [this](Builder &, Value *val, RuleSet::Captures &, Replacements &) {
             return onSave(llvm::dyn_cast<llvm::Instruction>(val));
           }});
}

void ModuleTransformationPass::addRule(ReplacementRule &&rule)
{
  auto ret = std::make_shared<ReplacementRule>(std::move(rule));

  rule_set_.addRule(ret);
}

bool ModuleTransformationPass::onLoad(llvm::Instruction *instruction)
{
  llvm::errs() << " --> Load " << *instruction << "\n";
  return true;
}

bool ModuleTransformationPass::onSave(llvm::Instruction *instruction)
{
  llvm::errs() << " --> Save " << *instruction << "\n";
  return true;
}

bool ModuleTransformationPass::onQubitReferenceUpdate(llvm::Instruction *instruction,
                                                      Captures &         captures)
{
  llvm::errs() << " --> Qubit reference update " << *instruction << "\n";
  llvm::errs() << "                            " << *captures["name"] << "\n";
  llvm::errs() << "                            " << *captures["value"] << "\n";

  auto it = qubit_reference_count_.find(resolveAlias(captures["name"]));
  if (it == qubit_reference_count_.end())
  {
    llvm::errs() << "ERROR: Qubit not found"
                 << "\n";
    return false;
  }

  auto cst       = llvm::dyn_cast<llvm::ConstantInt>(captures["value"]);
  auto llvm_size = cst->getValue();
  it->second += llvm_size.getZExtValue();
  if (it->second == 0)
  {
    llvm::errs() << "DEALLOCATING\n";
  }
  return true;
}

ModuleTransformationPass::Value *ModuleTransformationPass::resolveAlias(Value *original)
{
  return original;
}

bool ModuleTransformationPass::onQubitAllocate(llvm::Instruction *instruction, Captures &)
{
  llvm::errs() << " --> Qubit allocation: " << instruction->getName() << " " << instruction << "\n";
  assert(instruction != nullptr);
  qubit_reference_count_[resolveAlias(static_cast<Value *>(instruction))] = 1;
  return true;
}

bool ModuleTransformationPass::onArrayReferenceUpdate(llvm::Instruction *instruction)
{
  llvm::errs() << " --> Array reference update " << *instruction << "\n";
  return true;
}

bool ModuleTransformationPass::onArrayAllocate(llvm::Instruction *instruction)
{
  llvm::errs() << " --> Array allocation: " << *instruction << "\n";
  return true;
}

bool ModuleTransformationPass::runOnInstruction(llvm::Instruction *instruction)
{
  auto callptr = llvm::dyn_cast<llvm::CallBase>(instruction);
  if (callptr)
  {
    auto callee_function = callptr->getCalledFunction();
    auto name            = callee_function->getName().str();

    if (name == "__quantum__rt__array_create_1d")
    {
      onArrayAllocate(instruction);
    }
    else if (name.size() >= 9 && name.substr(0, 9) == "__quantum")
    {

      // llvm::errs() << "Ignoring " << name << "\n";
    }
    else
    {
      runOnFunction(*callee_function);
    }
  }
  else
  {
    for (auto operand = instruction->operands().begin(); operand != instruction->operands().end();
         ++operand)
    {
      runOnOperand(operand->get());
    }
  }
  return true;
}

bool ModuleTransformationPass::runOnOperand(llvm::Value *operand)
{
  operand->printAsOperand(llvm::errs(), true);

  // ... do something else ...
  auto *instruction = llvm::dyn_cast<llvm::Instruction>(operand);

  if (nullptr != instruction)
  {
    llvm::errs() << " >> dep > " << *instruction << "\n";
    return runOnInstruction(instruction);
  }
  else
  {
    return false;
  }
}

bool ModuleTransformationPass::runOnFunction(llvm::Function &function)
{
  llvm::errs() << "Entering " << function.getName() << "\n";
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      llvm::errs() << "run: " << instr << "\n";

      if (!rule_set_.matchAndReplace(&instr, replacements_))
      {
        runOnInstruction(&instr);
      }
    }
  }
  llvm::errs() << "Leaving " << function.getName() << "\n";
  return true;
}

llvm::PreservedAnalyses ModuleTransformationPass::run(llvm::Module &module,
                                                      llvm::ModuleAnalysisManager & /*mam*/)
{
  Setup();

  llvm::errs() << "start: " << this << " " << &rule_set_ << "\n";
  replacements_.clear();
  // For every instruction in every block, we attempt a match
  // and replace.
  for (auto &function : module)
  {
    // Idenfying entrypoint
    runOnFunction(function);
    llvm::errs() << "\n\n";

    /*
    for (auto &basic_block : function)
    {
      for (auto &instr : basic_block)
      {
        llvm::errs() << instr << "\n";
        //        rule_set_.matchAndReplace(&instr, replacements_);
      }

    }
    */
  }

  // Applying all replacements
  /*
  for (auto it = replacements_.rbegin(); it != replacements_.rend(); ++it)
  {
    auto instr1 = llvm::dyn_cast<llvm::Instruction>(it->first);
    if (instr1 == nullptr)
    {
      llvm::errs() << "; WARNING: cannot deal with non-instruction replacements\n";
      continue;
    }

    // Cheking if have a replacement for the instruction
    if (it->second != nullptr)
    {
      // ... if so, we just replace it,
      auto instr2 = llvm::dyn_cast<llvm::Instruction>(it->second);
      if (instr2 == nullptr)
      {
        llvm::errs() << "; WARNING: cannot replace instruction with non-instruction\n";
        continue;
      }
      llvm::ReplaceInstWithInst(instr1, instr2);
    }
    else
    {
      // ... otherwise we delete the the instruction
      // Removing all uses
      if (!instr1->use_empty())
      {
        instr1->replaceAllUsesWith(llvm::UndefValue::get(instr1->getType()));
      }

      // And finally we delete the instruction
      instr1->eraseFromParent();
    }
  }
*/

  // Cleaning all references
  qubit_reference_count_.clear();

  // If we did not change the IR, we report that we preserved all
  if (replacements_.empty())
  {
    return llvm::PreservedAnalyses::all();
  }

  // ... and otherwise, we report that we preserved none.
  return llvm::PreservedAnalyses::none();
}

bool ModuleTransformationPass::isRequired()
{
  return true;
}

}  // namespace quantum
}  // namespace microsoft
