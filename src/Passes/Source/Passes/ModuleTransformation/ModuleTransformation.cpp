// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/ModuleTransformation/ModuleTransformation.hpp"

#include "Llvm/Llvm.hpp"
#include "Rules/Factory.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/ReplacementRule.hpp"

#include <fstream>
#include <iostream>

namespace microsoft {
namespace quantum {

void ModuleTransformationPass::setupCopyAndExpand()
{
  using namespace microsoft::quantum::notation;
  addConstExprRule(
      {branch("cond"_cap = constInt(), "if_false"_cap = _, "if_true"_cap = _),
       [](Builder &builder, Value *val, Captures &captures, Replacements &replacements) {
         auto cst = llvm::dyn_cast<llvm::ConstantInt>(captures["cond"]);
         if (cst == nullptr)
         {
           return false;
         }

         auto instr       = llvm::dyn_cast<llvm::Instruction>(val);
         auto branch_cond = cst->getValue().getZExtValue();
         auto if_true     = llvm::dyn_cast<llvm::BasicBlock>(captures["if_true"]);
         auto if_false    = llvm::dyn_cast<llvm::BasicBlock>(captures["if_false"]);

         if (branch_cond)
         {
           builder.CreateBr(if_true);
           instr->replaceAllUsesWith(llvm::UndefValue::get(instr->getType()));
         }
         else
         {
           builder.CreateBr(if_false);
           instr->replaceAllUsesWith(llvm::UndefValue::get(instr->getType()));
         }

         replacements.push_back({val, nullptr});

         return true;
       }});
}

void ModuleTransformationPass::addConstExprRule(ReplacementRule &&rule)
{
  auto ret = std::make_shared<ReplacementRule>(std::move(rule));

  const_expr_replacements_.addRule(ret);
}

void ModuleTransformationPass::constantFoldFunction(llvm::Function &function)
{
  std::vector<llvm::Instruction *> to_delete;

  // Folding all constants
  for (auto &basic_block : function)
  {

    for (auto &instr : basic_block)
    {
      auto module = instr.getModule();
      auto dl     = module->getDataLayout();  // TODO: Move outside of loop
      auto cst    = llvm::ConstantFoldInstruction(&instr, dl, nullptr);
      if (cst != nullptr)
      {
        instr.replaceAllUsesWith(cst);
        to_delete.push_back(&instr);
      }
    }
  }

  // Deleting constants
  for (auto &x : to_delete)
  {
    x->eraseFromParent();
  }

  // Folding constant expressions
  Replacements replacements;
  for (auto &basic_block : function)
  {
    for (auto &instr : basic_block)
    {

      const_expr_replacements_.matchAndReplace(&instr, replacements);
    }
  }

  for (auto &r : replacements)
  {
    if (r.second != nullptr)
    {
      throw std::runtime_error("Real replacements not implemented.");
    }
    auto instr = llvm::dyn_cast<llvm::Instruction>(r.first);
    if (instr != nullptr)
    {
      instr->eraseFromParent();
      continue;
    }

    auto block = llvm::dyn_cast<llvm::BasicBlock>(r.first);
    if (block != nullptr)
    {
      llvm::DeleteDeadBlock(block);
    }
  }
}

llvm::Value *ModuleTransformationPass::copyAndExpand(
    llvm::Value *input, DeletableInstructions &schedule_instruction_deletion)
{
  llvm::Value *ret        = input;
  auto *       call_instr = llvm::dyn_cast<llvm::CallBase>(input);
  if (call_instr != nullptr)
  {
    auto &instr = *llvm::dyn_cast<llvm::Instruction>(input);

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
        schedule_instruction_deletion.push_back(&instr);
        // TODO: Delete instruction, instr.deleteInstru??;

        constantFoldFunction(*new_callee);

        // Recursion: Returning the new call as the instruction to be analysed
        ret = new_call;
      }

      // Deleting the function the original function if it is no longer in use
      if (callee_function->use_empty())
      {
        callee_function->eraseFromParent();
      }
    }
  }

  return ret;
}

llvm::Value *ModuleTransformationPass::detectActiveCode(llvm::Value *input, DeletableInstructions &)
{
  active_pieces_.insert(input);
  return input;
}

bool ModuleTransformationPass::runOnFunction(llvm::Function &           function,
                                             InstructionModifier const &modifier)
{
  if (depth_ >= 16)
  {
    llvm::errs() << "Exceed max recursion of 16\n";
    return false;
  }
  ++depth_;

  // Keep track of instructions scheduled for deletion
  DeletableInstructions schedule_instruction_deletion;

  // Block queue
  std::deque<llvm::BasicBlock *>         queue;
  std::unordered_set<llvm::BasicBlock *> blocks_queued;
  queue.push_back(&function.getEntryBlock());
  blocks_queued.insert(&function.getEntryBlock());

  // Executing the modifier on the function itsel
  modifier(&function, schedule_instruction_deletion);

  while (!queue.empty())
  {
    auto &basic_block = *(queue.front());
    queue.pop_front();

    // Executing the modifier on the block
    modifier(&basic_block, schedule_instruction_deletion);

    for (auto &instr : basic_block)
    {
      // Modifying instruction as needed
      auto instr_ptr = modifier(&instr, schedule_instruction_deletion);

      // In case the instruction was scheduled for deletion
      if (instr_ptr == nullptr)
      {
        continue;
      }

      // Checking if we are calling a function
      auto call_instr = llvm::dyn_cast<llvm::CallBase>(instr_ptr);
      if (call_instr != nullptr)
      {
        auto callee_function = call_instr->getCalledFunction();
        if (!callee_function->isDeclaration())
        {
          runOnFunction(*callee_function, modifier);
        }
      }

      // Following the branches to their basic blocks
      auto *br_instr = llvm::dyn_cast<llvm::BranchInst>(&instr);
      if (br_instr != nullptr)
      {
        for (uint32_t i = 0; i < br_instr->getNumOperands(); ++i)
        {
          // TODO: This may not work on multi path branches (conditional)
          // as we may accidently add the final path (contains qubit release)
          // and we cannot make assumptions since optimisation may have rearranged
          // everything. In this case, we should revert to the order they appear in the
          // function
          auto bb = llvm::dyn_cast<llvm::BasicBlock>(br_instr->getOperand(i));
          if (bb != nullptr)
          {

            // Ensuring that we are not scheduling the same block twice
            if (blocks_queued.find(bb) == blocks_queued.end())
            {
              queue.push_back(bb);
              blocks_queued.insert(bb);
            }
          }
        }
      }
    }
  }

  // Deleting constants
  for (auto &x : schedule_instruction_deletion)
  {
    x->eraseFromParent();
  }

  --depth_;

  return true;
}

bool ModuleTransformationPass::isActive(llvm::Value *value) const
{
  return active_pieces_.find(value) != active_pieces_.end();
}

void ModuleTransformationPass::runCopyAndExpand(llvm::Module &module, llvm::ModuleAnalysisManager &)
{
  replacements_.clear();
  // For every instruction in every block, we attempt a match
  // and replace.
  for (auto &function : module)
  {
    if (function.hasFnAttribute("EntryPoint"))
    {
      runOnFunction(function, [this](llvm::Value *value, DeletableInstructions &modifier) {
        return copyAndExpand(value, modifier);
      });
    }
  }

  // Dead code detection
  for (auto &function : module)
  {
    if (function.hasFnAttribute("EntryPoint"))
    {
      // Marking function as active
      active_pieces_.insert(&function);

      // Detectecting active code
      runOnFunction(function, [this](llvm::Value *value, DeletableInstructions &modifier) {
        return detectActiveCode(value, modifier);
      });
    }
  }

  applyReplacements();
}

void ModuleTransformationPass::applyReplacements()
{
  // Applying all replacements

  std::unordered_set<llvm::Value *> already_removed;
  for (auto it = replacements_.rbegin(); it != replacements_.rend(); ++it)
  {
    auto instr1 = llvm::dyn_cast<llvm::Instruction>(it->first);

    // Checking if by accident the same instruction was added
    if (already_removed.find(instr1) != already_removed.end())
    {
      llvm::errs() << "DUPLICATE instruction removal - TODO: Work out why this happens"
                   << "\n";
      continue;
    }
    already_removed.insert(instr1);

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
      if (instr1->use_empty())
      {
        instr1->eraseFromParent();
      }
    }
  }

  replacements_.clear();
}

void ModuleTransformationPass::runDetectActiveCode(llvm::Module &module,
                                                   llvm::ModuleAnalysisManager &)
{
  blocks_to_delete_.clear();
  functions_to_delete_.clear();

  for (auto &function : module)
  {
    if (isActive(&function))
    {
      for (auto &block : function)
      {
        if (!isActive(&block))
        {
          blocks_to_delete_.push_back(&block);
        }
      }
    }
    else if (!function.isDeclaration())
    {
      functions_to_delete_.push_back(&function);
    }
  }
}

void ModuleTransformationPass::runDeleteDeadCode(llvm::Module &, llvm::ModuleAnalysisManager &)
{
  std::vector<llvm::Instruction *> to_delete;

  // Removing all function references and scheduling blocks for deletion
  for (auto &function : functions_to_delete_)
  {
    // Schedule for deletion
    function->replaceAllUsesWith(llvm::UndefValue::get(function->getType()));

    function->clearGC();
    function->clearMetadata();

    for (auto &block : *function)
    {
      // Removing all instructions
      for (auto &instr : block)
      {
        instr.replaceAllUsesWith(llvm::UndefValue::get(instr.getType()));
        to_delete.push_back(&instr);
      }

      // Removing all block references
      block.replaceAllUsesWith(llvm::UndefValue::get(block.getType()));

      // Scheduling block deletion
      blocks_to_delete_.push_back(&block);
    }
  }

  // Removing all instructions
  for (auto &instr : to_delete)
  {
    instr->eraseFromParent();
  }

  // Deleting all blocks
  for (auto block : blocks_to_delete_)
  {
    block->replaceAllUsesWith(llvm::UndefValue::get(block->getType()));
    if (block->use_empty())
    {
      block->eraseFromParent();
    }
  }

  // Removing functions
  for (auto &function : functions_to_delete_)
  {
    if (function->isDeclaration() && function->use_empty())
    {
      function->eraseFromParent();
    }
  }
}

void ModuleTransformationPass::runReplacePhi(llvm::Module &module, llvm::ModuleAnalysisManager &)
{
  using namespace microsoft::quantum::notation;
  auto                             rule = phi("b1"_cap = _, "b2"_cap = _);
  IOperandPrototype::Captures      captures;
  std::vector<llvm::Instruction *> to_delete;

  for (auto &function : module)
  {
    for (auto &block : function)
    {
      for (auto &instr : block)
      {
        if (rule->match(&instr, captures))
        {
          auto phi  = llvm::dyn_cast<llvm::PHINode>(&instr);
          auto val1 = captures["b1"];
          auto val2 = captures["b2"];

          auto block1 = phi->getIncomingBlock(0);  // TODO: Make sure that block1 matches val1
          auto block2 = phi->getIncomingBlock(1);

          if (!isActive(block1))
          {
            val2->takeName(&instr);
            instr.replaceAllUsesWith(val2);
            to_delete.push_back(&instr);
          }
          else if (!isActive(block2))
          {
            val1->takeName(&instr);
            instr.replaceAllUsesWith(val1);
            to_delete.push_back(&instr);
          }

          captures.clear();
        }
      }
    }
  }

  for (auto &x : to_delete)
  {
    x->eraseFromParent();
  }
}

void ModuleTransformationPass::runApplyRules(llvm::Module &module, llvm::ModuleAnalysisManager &)
{
  replacements_.clear();

  std::unordered_set<llvm::Value *> already_visited;
  for (auto &function : module)
  {
    if (function.hasFnAttribute("EntryPoint"))
    {
      runOnFunction(function,
                    [this, &already_visited](llvm::Value *value, DeletableInstructions &) {
                      auto instr = llvm::dyn_cast<llvm::Instruction>(value);

                      // Sanity check
                      if (already_visited.find(value) != already_visited.end())
                      {
                        throw std::runtime_error("Already visited");
                      }
                      already_visited.insert(value);

                      // Checking if we should analyse
                      if (instr != nullptr)
                      {
                        rule_set_.matchAndReplace(instr, replacements_);
                      }
                      return value;
                    });
    }
  }

  applyReplacements();
}

llvm::PreservedAnalyses ModuleTransformationPass::run(llvm::Module &               module,
                                                      llvm::ModuleAnalysisManager &mam)
{

  // In case the module is istructed to clone functions,
  if (clone_functions_)
  {
    setupCopyAndExpand();
    runCopyAndExpand(module, mam);
  }

  // Deleting dead code if configured to do so. This process consists
  // of three steps: detecting dead code, removing phi nodes (and references)
  // and finally deleting the code. This implementation is aggressive in the sense
  // that any code that we cannot prove to be active is considered dead.
  if (delete_dead_code_)
  {
    runDetectActiveCode(module, mam);
    runReplacePhi(module, mam);
    runDeleteDeadCode(module, mam);
  }

  // Applying rule set
  runApplyRules(module, mam);

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
