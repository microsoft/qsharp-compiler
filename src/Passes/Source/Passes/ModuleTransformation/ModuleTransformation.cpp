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
  addRule({call("__quantum__rt__qubit_release", "name"_cap = _),
           [this](Builder &, Value *val, Captures &captures, Replacements &) {
             return onQubitRelease(llvm::dyn_cast<llvm::Instruction>(val), captures);
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

bool ModuleTransformationPass::onQubitRelease(llvm::Instruction *instruction, Captures &captures)
{
  llvm::errs() << " --> Qubit release " << *instruction << "\n";
  llvm::errs() << "                   " << *captures["name"] << "\n";

  auto it = qubit_reference_count_.find(resolveAlias(captures["name"]));
  if (it == qubit_reference_count_.end())
  {
    llvm::errs() << "ERROR: Qubit not found"
                 << "\n";
    return false;
  }

  qubit_reference_count_.erase(it);

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
      // runOnFunction(*callee_function);
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

void ModuleTransformationPass::constantFoldFunction(llvm::Function &function)
{
  std::vector<llvm::Instruction *> to_delete;
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      auto module = instr.getModule();
      auto dl     = module->getDataLayout();  // TODO: Move outside of loop
      auto cst    = llvm::ConstantFoldInstruction(&instr, dl, nullptr);
      if (cst != nullptr)
      {
        llvm::errs() << "CAN CONST FOLD: " << instr << " -> " << *cst << "\n";
        instr.replaceAllUsesWith(cst);
        to_delete.push_back(&instr);
        // TODO: Schedule for deletion
        //        instr.eraseFromParent();
      }
    }
  }

  for (auto &x : to_delete)
  {
    x->eraseFromParent();
  }
}

bool ModuleTransformationPass::runOnFunction(llvm::Function &function)
{
  if (depth_ >= 16)
  {
    llvm::errs() << "Exceed max recursion of 16\n";
    return false;
  }
  ++depth_;

  llvm::errs() << "\n\n----> Entering " << function.getName() << "\n";
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {
      // TODO: Identify loops
      auto *call_instr = llvm::dyn_cast<llvm::CallBase>(&instr);
      if (call_instr != nullptr)
      {

        auto callee_function = call_instr->getCalledFunction();
        if (!callee_function->isDeclaration())
        {
          ConstantArguments     argument_constants{};
          std::vector<uint32_t> remaining_arguments{};

          uint32_t idx = 0;
          auto     n   = static_cast<uint32_t>(callee_function->arg_size());

          // Finding argument constants
          while (idx < n)
          {
            auto arg   = callee_function->getArg(idx);
            auto value = call_instr->getArgOperand(idx);

            auto cst = llvm::dyn_cast<llvm::ConstantInt>(value);
            if (cst != nullptr)
            {
              argument_constants[arg->getName().str()] = cst;
            }
            else
            {
              remaining_arguments.push_back(idx);
            }

            ++idx;
          }

          // Making a function copy
          auto new_callee = expandFunctionCall(*callee_function, argument_constants);

          // Replacing call if a new function was created
          if (new_callee != nullptr)
          {

            llvm::IRBuilder<> builder(call_instr);

            // List with new call arguments
            std::vector<llvm::Value *> new_arguments;
            for (auto const &i : remaining_arguments)
            {
              // Getting the i'th argument
              llvm::Value *arg = call_instr->getArgOperand(i);

              // Adding arguments that were not constant
              if (argument_constants.find(arg->getName().str()) == argument_constants.end())
              {
                new_arguments.push_back(arg);
              }
            }

            // Creating a new call
            auto *new_call = builder.CreateCall(new_callee, new_arguments);
            new_call->takeName(call_instr);

            // Replace all calls to old function with calls to new function
            instr.replaceAllUsesWith(new_call);

            // Deleting instruction
            // TODO: Delete instruction, instr.deleteInstru??;

            constantFoldFunction(*new_callee);

            // Recursion
            runOnFunction(*new_callee);
          }

          if (callee_function->use_empty())
          {
            callee_function->eraseFromParent();
          }

          continue;
        }

        //        llvm::errs() << "Cannot follow " << instr << "\n";

        /*
        void  deleteBody ()
          deleteBody - This method deletes the body of the function, and converts the linkage to
        external. More...

        void  removeFromParent ()
          removeFromParent - This method unlinks 'this' from the containing module, but does not
        delete it. More...

        void  eraseFromParent ()
          eraseFromParent - This method unlinks 'this' from the containing module and deletes it.
        More...
          */

        continue;
      }

      //      llvm::errs() << "run: " << instr << "\n";

      if (!rule_set_.matchAndReplace(&instr, replacements_))
      {
        runOnInstruction(&instr);
      }
    }
  }
  llvm::errs() << "<<<< ----- Leaving " << function.getName() << "\n\n";
  --depth_;
  return true;
}

llvm::PreservedAnalyses ModuleTransformationPass::run(llvm::Module &module,
                                                      llvm::ModuleAnalysisManager &)
{

  Setup();

  llvm::errs() << "start: " << this << " " << &rule_set_ << "\n";
  replacements_.clear();
  // For every instruction in every block, we attempt a match
  // and replace.
  for (auto &function : module)
  {
    if (function.hasFnAttribute("EntryPoint"))
    {
      llvm::errs() << function.getName() << " is the entrypoint"
                   << "\n";
      runOnFunction(function);
      llvm::errs() << "\n\n";
    }
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

llvm::Function *ModuleTransformationPass::expandFunctionCall(llvm::Function &         callee,
                                                             ConstantArguments const &const_args)
{
  auto              module  = callee.getParent();
  auto &            context = module->getContext();
  llvm::IRBuilder<> builder(context);

  // Copying the original function
  llvm::ValueToValueMapTy   remapper;
  std::vector<llvm::Type *> arg_types;

  // The user might be deleting arguments to the function by specifying them in
  // the VMap.  If so, we need to not add the arguments to the arg ty vector
  //
  for (auto const &arg : callee.args())
  {
    // Skipping constant arguments

    if (const_args.find(arg.getName().str()) != const_args.end())
    {
      continue;
    }

    arg_types.push_back(arg.getType());
  }

  // Creating a new function
  llvm::FunctionType *function_type = llvm::FunctionType::get(
      callee.getFunctionType()->getReturnType(), arg_types, callee.getFunctionType()->isVarArg());
  auto function = llvm::Function::Create(function_type, callee.getLinkage(),
                                         callee.getAddressSpace(), callee.getName(), module);

  // Copying the non-const arguments
  auto dest_args_it = function->arg_begin();

  for (auto const &arg : callee.args())
  {
    auto const_it = const_args.find(arg.getName().str());
    if (const_it == const_args.end())
    {
      // Mapping remaining function arguments
      dest_args_it->setName(arg.getName());
      remapper[&arg] = &*dest_args_it++;
    }
    else
    {
      remapper[&arg] = llvm::ConstantInt::get(context, const_it->second->getValue());
    }
  }

  llvm::SmallVector<llvm::ReturnInst *, 8> returns;  // Ignore returns cloned.

  // TODO(QAT-private-issue-28): In LLVM 13 upgrade 'true' to
  // 'llvm::CloneFunctionChangeType::LocalChangesOnly'
  llvm::CloneFunctionInto(function, &callee, remapper, true, returns, "", nullptr);

  verifyFunction(*function);

  return function;
}

bool ModuleTransformationPass::isRequired()
{
  return true;
}

}  // namespace quantum
}  // namespace microsoft
