// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"

#include "Llvm/Llvm.hpp"

#include <fstream>
#include <iostream>

namespace microsoft
{
namespace quantum
{
    /// This pass traverse the IR and uses the QirAllocationAnalysis to determine
    /// if a function call results in qubit and/or result allocation. If that is the case,
    /// it makes a copy of the function and replaces the function call with a call to the
    /// new function.
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
                auto& use_quantum     = fam.getResult<QirAllocationAnalysis>(*callee_function);

                if (use_quantum.value)
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
                    auto new_callee = expandFunctionCall(*callee_function, argument_constants);

                    // Replacing call if a new function was created
                    if (new_callee != nullptr)
                    {
                        llvm::IRBuilder<> builder(call_instr);

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
                        new_call->takeName(call_instr);

                        // Replace all calls to old function with calls to new function
                        instruction.replaceAllUsesWith(new_call);

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
        llvm::Function&          callee,
        ConstantArguments const& const_args)
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

        // TODO(QAT-private-issue-28): In LLVM 13 upgrade 'true' to
        // 'llvm::CloneFunctionChangeType::LocalChangesOnly'
        llvm::CloneFunctionInto(function, &callee, remapper, true, returns, "", nullptr);

        verifyFunction(*function);

        return function;
    }

    bool ExpandStaticAllocationPass::isRequired()
    {
        return true;
    }

} // namespace quantum
} // namespace microsoft
