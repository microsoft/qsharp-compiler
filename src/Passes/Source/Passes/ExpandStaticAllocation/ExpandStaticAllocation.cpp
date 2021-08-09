// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm.hpp"

#include "ExpandStaticAllocation/ExpandStaticAllocation.hpp"

#include <fstream>
#include <iostream>

namespace microsoft
{
namespace quantum
{
    llvm::PreservedAnalyses ExpandStaticAllocationPass::run(
        llvm::Function&                function,
        llvm::FunctionAnalysisManager& fam)
    {
        // Pass body
        for (auto& basic_block : function)
        {
            // Keeping track of instructions to remove in each block
            std::vector<llvm::Instruction*> to_remove;

            for (auto& instruction : basic_block)
            {
                // Finding calls
                auto* call_instr = llvm::dyn_cast<llvm::CallBase>(&instruction);
                if (call_instr == nullptr)
                {
                    continue;
                }

                ConstantArguments     argument_constants{};
                std::vector<uint32_t> remaining_arguments{};

                auto  callee_function = call_instr->getCalledFunction();
                auto& depenency_graph = fam.getResult<QubitAllocationAnalysisAnalytics>(*callee_function);

                if (depenency_graph.size() > 0)
                {
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

                    // Checking which arrays are constant for this
                    auto new_callee = expandFunctionCall(depenency_graph, *callee_function, argument_constants);

                    // Replacing call if a new function was created
                    if (new_callee != nullptr)
                    {
                        llvm::IRBuilder<> builder(call_instr);
                        (void)call_instr;

                        // List with new call arguments
                        std::vector<llvm::Value*> new_arguments;
                        for (auto const& i : remaining_arguments)
                        {
                            // Getting the i'th argument
                            llvm::Value* arg = call_instr->getArgOperand(i);

                            // Adding arguments that were not constant
                            if (argument_constants.find(arg->getName().str()) == argument_constants.end())
                            {
                                new_arguments.push_back(arg);
                            }
                        }

                        // Creating a new call
                        llvm::Value* new_call = builder.CreateCall(new_callee, new_arguments);

                        // Replace all calls to old function with calls to new function
                        for (auto& use : call_instr->uses())
                        {
                            llvm::User* user = use.getUser();
                            user->setOperand(use.getOperandNo(), new_call);
                        }

                        // Schedule original instruction for deletion
                        to_remove.push_back(&instruction);
                    }
                }
            }

            // Removing instructions
            for (auto& instruction : to_remove)
            {
                if (!instruction->use_empty())
                {
                    instruction->replaceAllUsesWith(llvm::UndefValue::get(instruction->getType()));
                }
                instruction->eraseFromParent();
            }
        }

        return llvm::PreservedAnalyses::none();
    }

    llvm::Function* ExpandStaticAllocationPass::expandFunctionCall(
        QubitAllocationResult const& depenency_graph,
        llvm::Function&              callee,
        ConstantArguments const&     const_args)
    {
        bool should_replace_function = false;
        if (!depenency_graph.empty())
        {
            // Checking that any of all allocations in the function
            // body becomes static from replacing constant function arguments
            for (auto const& allocation : depenency_graph)
            {
                // Ignoring non-static allocations
                if (!allocation.is_possibly_static)
                {
                    continue;
                }

                // Ignoring trivial allocations
                if (allocation.depends_on.empty())
                {
                    continue;
                }

                // Checking all dependencies are constant
                bool all_const = true;
                for (auto& name : allocation.depends_on)
                {
                    all_const = all_const && (const_args.find(name) != const_args.end());
                }

                // In case that all dependencies are constant for this
                // allocation, we should replace the function with one where
                // the arguments are eliminated.
                if (all_const)
                {
                    should_replace_function = true;
                }
            }
        }

        // Replacing function if needed
        if (should_replace_function)
        {
            auto              module  = callee.getParent();
            auto&             context = module->getContext();
            llvm::IRBuilder<> builder(context);

            // Copying the original function
            llvm::ValueToValueMapTy  remapper;
            std::vector<llvm::Type*> arg_types;

            // The user might be deleting arguments to the function by specifying them in
            // the VMap.  If so, we need to not add the arguments to the arg ty vector
            //
            for (auto const& arg : callee.args())
            {
                // Skipping constant arguments

                if (const_args.find(arg.getName().str()) != const_args.end())
                {
                    continue;
                }

                arg_types.push_back(arg.getType());
            }

            // Creating a new function
            llvm::FunctionType* function_type = llvm::FunctionType::get(
                callee.getFunctionType()->getReturnType(), arg_types, callee.getFunctionType()->isVarArg());
            auto function = llvm::Function::Create(
                function_type, callee.getLinkage(), callee.getAddressSpace(), callee.getName(), module);

            // Copying the non-const arguments
            auto dest_args_it = function->arg_begin();

            for (auto const& arg : callee.args())
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

            llvm::SmallVector<llvm::ReturnInst*, 8> returns; // Ignore returns cloned.

            // TODO(tfr): In LLVM 13 upgrade 'true' to 'llvm::CloneFunctionChangeType::LocalChangesOnly'
            llvm::CloneFunctionInto(function, &callee, remapper, true, returns, "", nullptr);

            verifyFunction(*function);

            return function;
        }

        return nullptr;
    }

    bool ExpandStaticAllocationPass::isRequired()
    {
        return true;
    }

} // namespace quantum
} // namespace microsoft
