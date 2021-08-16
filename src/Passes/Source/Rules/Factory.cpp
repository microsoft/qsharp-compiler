// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Factory.hpp"
#include "Rules/Notation/Notation.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{
    using ReplacementRulePtr = RuleFactory::ReplacementRulePtr;
    using namespace microsoft::quantum::notation;

    RuleFactory::RuleFactory(RuleSet& rule_set)
      : rule_set_{rule_set}
      , qubit_alloc_manager_{AllocationManager::createNew()}
      , result_alloc_manager_{AllocationManager::createNew()}
    {
    }

    RuleFactory::AllocationManagerPtr RuleFactory::qubitAllocationManager() const
    {
        return qubit_alloc_manager_;
    }

    RuleFactory::AllocationManagerPtr RuleFactory::resultAllocationManager() const
    {
        return result_alloc_manager_;
    }

    void RuleFactory::removeFunctionCall(String const& name)
    {
        addRule({callByNameOnly(name), deleteInstruction()});
    }

    void RuleFactory::useStaticQubitArrayAllocation()
    {
        // TODO(QAT-private-issue-32): Use weak pointers to capture allocation managers
        auto qubit_alloc_manager = qubit_alloc_manager_;

        /// Allocation
        auto allocation_replacer =
            [qubit_alloc_manager](Builder&, Value* val, Captures& cap, Replacements& replacements) {
                auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
                if (cst == nullptr)
                {
                    return false;
                }

                auto llvm_size = cst->getValue();
                auto name      = val->getName().str();
                qubit_alloc_manager->allocate(name, llvm_size.getZExtValue());

                replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
                return true;
            };

        addRule({call("__quantum__rt__qubit_allocate_array", "size"_cap = _), allocation_replacer});

        /// Array access replacement
        auto access_replacer =
            [qubit_alloc_manager](Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
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
                auto offset    = qubit_alloc_manager->getOffset(cap["arrayName"]->getName().str());

                // Creating a new index APInt that is shifted by the offset of the allocation
                auto idx = llvm::APInt(llvm_size.getBitWidth(), llvm_size.getZExtValue() + offset);

                // Computing offset
                auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

                // Converting pointer
                auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
                instr->takeName(val);

                // Replacing the instruction with new instruction
                replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

                // Deleting the getelement and cast operations
                replacements.push_back({llvm::dyn_cast<Instruction>(cap["getElement"]), nullptr});
                replacements.push_back({llvm::dyn_cast<Instruction>(cap["cast"]), nullptr});

                return true;
            };

        auto get_element  = call("__quantum__rt__array_get_element_ptr_1d", "arrayName"_cap = _, "index"_cap = _);
        auto cast_pattern = bitCast("getElement"_cap = get_element);
        auto load_pattern = load("cast"_cap = cast_pattern);

        addRule({std::move(load_pattern), access_replacer});

        /// Release replacement
        auto deleter = deleteInstruction();
        addRule(
            {call("__quantum__rt__qubit_release_array", "name"_cap = _),
             [qubit_alloc_manager, deleter](Builder& builder, Value* val, Captures& cap, Replacements& rep) {
                 qubit_alloc_manager->release(cap["name"]->getName().str());
                 return deleter(builder, val, cap, rep);
             }

            });
    }

    void RuleFactory::useStaticQubitAllocation()
    {
        auto qubit_alloc_manager = qubit_alloc_manager_;
        auto allocation_replacer =
            [qubit_alloc_manager](Builder& builder, Value* val, Captures&, Replacements& replacements) {
                // Getting the type pointer
                auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
                if (ptr_type == nullptr)
                {
                    return false;
                }

                // Computing the index by getting the current index value and offseting by
                // the offset at which the qubit array is allocated.
                auto offset = qubit_alloc_manager->allocate();

                // Creating a new index APInt that is shifted by the offset of the allocation
                // TODO(QAT-private-issue-32): Make default integer width a module parameter or extract from QIR
                auto idx = llvm::APInt(64, offset);

                // Computing offset
                auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

                auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
                instr->takeName(val);

                // Replacing the instruction with new instruction
                replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

                return true;
            };
        addRule({call("__quantum__rt__qubit_allocate"), allocation_replacer});

        /// Release replacement
        auto deleter = deleteInstruction();
        addRule(
            {call("__quantum__rt__qubit_release", "name"_cap = _),
             [qubit_alloc_manager, deleter](Builder& builder, Value* val, Captures& cap, Replacements& rep) {
                 qubit_alloc_manager->release();
                 return deleter(builder, val, cap, rep);
             }

            });
    }

    void RuleFactory::useStaticResultAllocation()
    {
        auto result_alloc_manager = result_alloc_manager_;
        auto replace_measurement =
            [result_alloc_manager](Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
                // Getting the type pointer
                auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
                if (ptr_type == nullptr)
                {
                    return false;
                }

                // Computing the index by getting the current index value and offseting by
                // the offset at which the qubit array is allocated.
                auto offset = result_alloc_manager->allocate();

                // Creating a new index APInt that is shifted by the offset of the allocation
                // TODO(QAT-private-issue-32): Make default integer width a module parameter or extract from QIR
                auto idx = llvm::APInt(64, offset);

                // Computing offset
                auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

                auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
                instr->takeName(val);

                auto module   = llvm::dyn_cast<llvm::Instruction>(val)->getModule();
                auto function = module->getFunction("__quantum__qis__mz__body");

                std::vector<llvm::Value*> arguments;
                arguments.push_back(cap["qubit"]);
                arguments.push_back(instr);

                if (!function)
                {
                    std::vector<llvm::Type*> types;
                    types.resize(arguments.size());
                    for (uint64_t i = 0; i < types.size(); ++i)
                    {
                        types[i] = arguments[i]->getType();
                    }

                    auto return_type = llvm::Type::getVoidTy(val->getContext());

                    llvm::FunctionType* fnc_type = llvm::FunctionType::get(return_type, types, false);
                    function                     = llvm::Function::Create(
                        fnc_type, llvm::Function::ExternalLinkage, "__quantum__qis__mz__body", module);
                }

                // Ensuring we are inserting after the instruction being deleted
                builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(val)->getNextNode());

                builder.CreateCall(function, arguments);

                // Replacing the instruction with new instruction
                replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

                return true;
            };

        addRule({call("__quantum__qis__m__body", "qubit"_cap = _), std::move(replace_measurement)});
    }

    void RuleFactory::optimiseBranchQuatumOne()
    {
        auto replace_branch_positive = [](Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
            auto result = cap["result"];
            auto cond   = llvm::dyn_cast<llvm::Instruction>(cap["cond"]);
            // Replacing result
            auto                      module   = llvm::dyn_cast<llvm::Instruction>(val)->getModule();
            auto                      function = module->getFunction("__quantum__qir__read_result");
            std::vector<llvm::Value*> arguments;
            arguments.push_back(result);

            if (!function)
            {
                std::vector<llvm::Type*> types;
                types.resize(arguments.size());
                for (uint64_t i = 0; i < types.size(); ++i)
                {
                    types[i] = arguments[i]->getType();
                }

                auto return_type = llvm::Type::getInt1Ty(val->getContext());

                llvm::FunctionType* fnc_type = llvm::FunctionType::get(return_type, types, false);
                function                     = llvm::Function::Create(
                    fnc_type, llvm::Function::ExternalLinkage, "__quantum__qir__read_result", module);
            }

            builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(val));
            auto new_call = builder.CreateCall(function, arguments);
            new_call->takeName(cond);

            for (auto& use : cond->uses())
            {
                llvm::User* user = use.getUser();
                user->setOperand(use.getOperandNo(), new_call);
            }
            cond->replaceAllUsesWith(new_call);

            // Deleting the previous condition and function to fetch one
            replacements.push_back({cond, nullptr});
            replacements.push_back({cap["one"], nullptr});

            return false;
        };

        /*
          Here is an example IR for which we want to make a match:

          %1 = call %Result* @__quantum__rt__result_get_one()
          %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
          br i1 %2, label %then0__1, label %continue__1
        */

        // Variations of get_one
        auto get_one = call("__quantum__rt__result_get_one");
        addRule(
            {branch("cond"_cap = call("__quantum__rt__result_equal", "result"_cap = _, "one"_cap = get_one), _, _),
             replace_branch_positive});

        addRule(
            {branch("cond"_cap = call("__quantum__rt__result_equal", "one"_cap = get_one, "result"_cap = _), _, _),
             replace_branch_positive});
    }

    void RuleFactory::disableReferenceCounting()
    {
        removeFunctionCall("__quantum__rt__array_update_reference_count");
        removeFunctionCall("__quantum__rt__string_update_reference_count");
        removeFunctionCall("__quantum__rt__result_update_reference_count");
    }

    void RuleFactory::disableAliasCounting()
    {
        removeFunctionCall("__quantum__rt__array_update_alias_count");
        removeFunctionCall("__quantum__rt__string_update_alias_count");
        removeFunctionCall("__quantum__rt__result_update_alias_count");
    }

    void RuleFactory::disableStringSupport()
    {
        removeFunctionCall("__quantum__rt__string_create");
        removeFunctionCall("__quantum__rt__string_release");
        removeFunctionCall("__quantum__rt__message");
    }

    ReplacementRulePtr RuleFactory::addRule(ReplacementRule&& rule)
    {
        auto ret = std::make_shared<ReplacementRule>(std::move(rule));

        rule_set_.addRule(ret);

        return ret;
    }

} // namespace quantum
} // namespace microsoft
