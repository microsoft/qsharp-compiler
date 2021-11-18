// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Factory.hpp"
#include "Rules/Notation/Notation.hpp"

#include "Llvm/Llvm.hpp"

namespace microsoft
{
namespace quantum
{
    namespace
    {
        using Instruction  = llvm::Instruction;
        using Value        = llvm::Value;
        using Builder      = ReplacementRule::Builder;
        using Captures     = ReplacementRule::Captures;
        using Replacements = ReplacementRule::Replacements;

    } // namespace

    using ReplacementRulePtr = RuleFactory::ReplacementRulePtr;
    using namespace microsoft::quantum::notation;

    RuleFactory::RuleFactory(
        RuleSet&             rule_set,
        AllocationManagerPtr qubit_alloc_manager,
        AllocationManagerPtr result_alloc_manager)
      : rule_set_{rule_set}
      , qubit_alloc_manager_{std::move(qubit_alloc_manager)}
      , result_alloc_manager_{std::move(result_alloc_manager)}
    {
    }

    void RuleFactory::usingConfiguration(FactoryConfiguration const& config)
    {
        default_integer_width_ = config.defaultIntegerWidth();

        if (config.disableReferenceCounting())
        {
            disableReferenceCounting();
        }

        if (config.disableAliasCounting())
        {
            disableAliasCounting();
        }

        if (config.disableStringSupport())
        {
            disableStringSupport();
        }

        if (config.optimiseResultOne())
        {
            optimiseResultOne();
        }

        if (config.optimiseResultZero())
        {
            optimiseResultZero();
        }

        if (config.useStaticQubitArrayAllocation())
        {
            useStaticQubitArrayAllocation();
        }

        if (config.useStaticQubitAllocation())
        {
            useStaticQubitAllocation();
        }

        if (config.useStaticResultAllocation())
        {
            useStaticResultAllocation();
        }
    }

    void RuleFactory::removeFunctionCall(String const& name)
    {
        addRule({callByNameOnly(name), deleteInstruction()});
    }

    void RuleFactory::resolveConstantArraySizes()
    {
        /// Array access replacement
        auto size_replacer = [](Builder&, Value* val, Captures& cap, Replacements& replacements) {
            // Get the index and testing that it is a constant int
            auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
            if (cst == nullptr)
            {
                // ... if not, we cannot perform the mapping.
                return false;
            }

            val->replaceAllUsesWith(cst);
            replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});

            return true;
        };
        llvm::errs() << "Creating pattern\n";

        auto create_array = call("__quantum__rt__array_create_1d", "elementSize"_cap = _, "size"_cap = _);
        auto get_size     = call("__quantum__rt__array_get_size_1d", create_array);

        addRule({std::move(get_size), size_replacer});
    }

    void RuleFactory::inlineCallables()
    {
        /// Array access replacement
        auto callable_replacer = [](Builder&, Value* val, Captures& captures, Replacements&) {
            llvm::errs() << "FOUND CALLABLE\n";
            llvm::errs() << *val << "\n";
            llvm::errs() << "Calling " << *captures["function"] << "\n";
            return false;
        };

        auto create_callable = call("__quantum__rt__callable_create", "function"_cap = _, "size"_cap = _, _);
        auto invoke          = call("__quantum__rt__callable_invoke", create_callable, "args"_cap = _, "ret"_cap = _);

        addRule({std::move(invoke), callable_replacer});
    }

    void RuleFactory::useStaticQubitArrayAllocation()
    {
        // TODO(QAT-private-issue-32): Use weak pointers to capture allocation managers
        auto qubit_alloc_manager = qubit_alloc_manager_;

        /// Allocation
        auto default_iw = default_integer_width_;
        auto allocation_replacer =
            [default_iw, qubit_alloc_manager](Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
                auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
                if (cst == nullptr)
                {
                    return false;
                }

                auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
                if (ptr_type == nullptr)
                {
                    return false;
                }

                if (cst == nullptr)
                {
                    return false;
                }

                auto llvm_size = cst->getValue();
                auto name      = val->getName().str();
                auto size      = llvm_size.getZExtValue();
                auto offset    = qubit_alloc_manager->allocate(name, size);

                // Creating a new index APInt that is shifted by the offset of the allocation
                auto idx = llvm::APInt(default_iw, offset);

                // Computing offset
                auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

                auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
                instr->takeName(val);

                // Replacing the instruction with new instruction
                auto old_instr = llvm::dyn_cast<Instruction>(val);

                // Safety precaution to ensure that we are dealing with a Instruction
                if (old_instr == nullptr)
                {
                    return false;
                }

                // Ensuring that we have replaced the instruction before
                // identifying release
                old_instr->replaceAllUsesWith(instr);

                replacements.push_back({old_instr, instr});
                return true;
            };

        /// This rule is replacing the allocate qubit array instruction
        ///
        /// %leftPreshared = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
        ///
        /// by changing it to a constant pointer
        ///
        /// %leftPreshared = inttoptr i64 0 to %Array*
        ///
        /// In this way, we use the

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

                // Computing the index by getting the current index value and offsetting by
                // the offset at which the qubit array is allocated.
                auto offset_cst = llvm::dyn_cast<llvm::ConstantInt>(cap["arrayName"]);
                if (offset_cst == nullptr)
                {
                    return false;
                }
                auto llvm_offset = offset_cst->getValue();
                auto offset      = llvm_offset.getZExtValue();

                // Creating a new index APInt that is shifted by the offset of the allocation
                auto llvm_size = cst->getValue();
                auto idx       = llvm::APInt(llvm_size.getBitWidth(), llvm_size.getZExtValue() + offset);

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

        auto get_element = call(
            "__quantum__rt__array_get_element_ptr_1d", intToPtr("arrayName"_cap = constInt()),
            "index"_cap = constInt());
        auto cast_pattern = bitCast("getElement"_cap = get_element);
        auto load_pattern = load("cast"_cap = cast_pattern);

        addRule({std::move(load_pattern), access_replacer});

        /// Release replacement
        auto deleter = deleteInstruction();

        addRule(
            {call("__quantum__rt__qubit_release_array", intToPtr("const"_cap = constInt())),
             [qubit_alloc_manager, deleter](Builder& builder, Value* val, Captures& cap, Replacements& rep) {
                 // Recovering the qubit id
                 auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["const"]);
                 if (cst == nullptr)
                 {
                     return false;
                 }
                 auto address = cst->getValue().getZExtValue();

                 // Releasing
                 qubit_alloc_manager->release(address);

                 // Deleting instruction
                 return deleter(builder, val, cap, rep);
             }});
    }

    void RuleFactory::useStaticQubitAllocation()
    {
        auto qubit_alloc_manager = qubit_alloc_manager_;
        auto default_iw          = default_integer_width_;
        auto allocation_replacer =
            [default_iw, qubit_alloc_manager](Builder& builder, Value* val, Captures&, Replacements& replacements) {
                // Getting the type pointer
                auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
                if (ptr_type == nullptr)
                {
                    return false;
                }

                auto qubit_name = val->getName().str();

                // Computing the index by getting the current index value and offseting by
                // the offset at which the qubit array is allocated.
                auto offset = qubit_alloc_manager->allocate(qubit_name);

                // Creating a new index APInt that is shifted by the offset of the allocation
                auto idx = llvm::APInt(default_iw, offset);

                // Computing offset
                auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

                auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
                instr->takeName(val);

                // Replacing the instruction with new instruction
                auto old_instr = llvm::dyn_cast<Instruction>(val);

                // Safety precaution to ensure that we are dealing with a Instruction
                if (old_instr == nullptr)
                {
                    return false;
                }

                // Ensuring that we have replaced the instruction before
                // identifying release
                old_instr->replaceAllUsesWith(instr);

                replacements.push_back({old_instr, instr});

                return true;
            };

        // Dealing with qubit allocation
        addRule({call("__quantum__rt__qubit_allocate"), allocation_replacer});

        /// Release replacement
        auto deleter = deleteInstruction();

        // Handling the case where a constant integer is cast to a pointer and the pointer
        // is used in a call to qubit_release:
        //
        // %0 = inttoptr i64 0 to %Qubit*
        // call void @__quantum__rt__qubit_release(%Qubit* %0
        //
        // The case of named addresses are also covered, by this pattern:
        // %leftMessage = inttoptr i64 0 to %Qubit*
        // call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)

        addRule(
            {call("__quantum__rt__qubit_release", intToPtr("const"_cap = constInt())),
             [qubit_alloc_manager, deleter](Builder& builder, Value* val, Captures& cap, Replacements& rep) {
                 // Recovering the qubit id
                 auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["const"]);
                 if (cst == nullptr)
                 {
                     return false;
                 }
                 auto address = cst->getValue().getZExtValue();

                 // Releasing
                 qubit_alloc_manager->release(address);

                 // Deleting instruction
                 return deleter(builder, val, cap, rep);
             }});

        // Handling where allocation is done by non-standard functions. In
        // this rule reports an error as we cannot reliably do a mapping.
        //
        // %leftMessage = call %Qubit* @__non_standard_allocator()
        // call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
        addRule(
            {call("__quantum__rt__qubit_release", "name"_cap = _),
             [qubit_alloc_manager, deleter](Builder& builder, Value* val, Captures& cap, Replacements& rep) {
                 // Getting the name
                 auto name = cap["name"]->getName().str();

                 // Returning in case the name comes out empty
                 if (name.empty())
                 {

                     // TODO(tfr): report error
                     llvm::outs() << "FAILED due to unnamed non standard allocation:\n";
                     llvm::outs() << *val << "\n\n";

                     // Deleting the instruction in order to proceed
                     // and trying to discover as many other errors as possible
                     return deleter(builder, val, cap, rep);
                 }

                 // TODO(tfr): report error
                 llvm::outs() << "FAILED due to non standard allocation:\n";
                 llvm::outs() << *cap["name"] << "\n";
                 llvm::outs() << *val << "\n\n";

                 return deleter(builder, val, cap, rep);
             }

            });
    }

    void RuleFactory::useStaticResultAllocation()
    {
        auto result_alloc_manager = result_alloc_manager_;
        auto default_iw           = default_integer_width_;
        auto replace_measurement  = [default_iw, result_alloc_manager](
                                       Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
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
            auto idx = llvm::APInt(default_iw, offset);

            // Computing offset
            auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

            auto instr = new llvm::IntToPtrInst(new_index, ptr_type);

            if (instr == nullptr)
            {
                return false;
            }

            instr->takeName(val);

            auto orig_instr = llvm::dyn_cast<llvm::Instruction>(val);
            if (orig_instr == nullptr)
            {
                return false;
            }

            auto module = orig_instr->getModule();
            auto fnc    = module->getFunction("__quantum__qis__mz__body");

            std::vector<llvm::Value*> arguments;
            arguments.push_back(cap["qubit"]);
            arguments.push_back(instr);

            if (!fnc)
            {
                std::vector<llvm::Type*> types;
                types.resize(arguments.size());
                for (uint64_t i = 0; i < types.size(); ++i)
                {
                    types[i] = arguments[i]->getType();
                }

                auto return_type = llvm::Type::getVoidTy(val->getContext());

                llvm::FunctionType* fnc_type = llvm::FunctionType::get(return_type, types, false);
                fnc                          = llvm::Function::Create(
                    fnc_type, llvm::Function::ExternalLinkage, "__quantum__qis__mz__body", module);
            }

            // Ensuring we are inserting after the instruction being deleted
            builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(val)->getNextNode());

            builder.CreateCall(fnc, arguments);

            // Replacing the instruction with new instruction
            replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

            return true;
        };

        // This rules identifies result allocations through the function "__quantum__qis__m__body".
        // As an example, the following
        //
        // %result1 = call %Result* @__quantum__qis__m__body(%Qubit* %0)
        //
        // translates into
        //
        // %result1 = inttoptr i64 0 to %Result*
        // call void @__quantum__qis__mz__body(%Qubit* %0, %Result* %result1)

        addRule({call("__quantum__qis__m__body", "qubit"_cap = _), std::move(replace_measurement)});
    }

    void RuleFactory::optimiseResultZero()
    {
        auto replace_branch_negative = [](Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
            auto cond = llvm::dyn_cast<llvm::Instruction>(val);
            if (cond == nullptr)
            {
                return false;
            }
            auto result = cap["result"];
            // Replacing result
            auto orig_instr = llvm::dyn_cast<llvm::Instruction>(val);
            if (orig_instr == nullptr)
            {
                return false;
            }

            auto                      module = orig_instr->getModule();
            auto                      fnc    = module->getFunction("__quantum__qis__read_result__body");
            std::vector<llvm::Value*> arguments;
            arguments.push_back(result);

            if (!fnc)
            {
                std::vector<llvm::Type*> types;
                types.resize(arguments.size());
                for (uint64_t i = 0; i < types.size(); ++i)
                {
                    types[i] = arguments[i]->getType();
                }

                auto return_type = llvm::Type::getInt1Ty(val->getContext());

                llvm::FunctionType* fnc_type = llvm::FunctionType::get(return_type, types, false);
                fnc                          = llvm::Function::Create(
                    fnc_type, llvm::Function::ExternalLinkage, "__quantum__qis__read_result__body", module);
            }

            builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(val));
            auto new_call = builder.CreateCall(fnc, arguments);
            auto new_cond = builder.CreateNot(new_call);
            new_cond->takeName(cond);

            for (auto& use : cond->uses())
            {
                llvm::User* user = use.getUser();
                user->setOperand(use.getOperandNo(), new_cond);
            }
            cond->replaceAllUsesWith(new_cond);

            // Deleting the previous condition and function to fetch one
            replacements.push_back({cond, nullptr});
            replacements.push_back({cap["zero"], nullptr});

            return true;
        };

        /*
          Here is an example IR for which we want to make a match:

          %1 = call %Result* @__quantum__rt__result_get_zero()
          %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
          br i1 %2, label %then0__1, label %continue__1
        */

        // Variations of get_one
        auto get_zero = call("__quantum__rt__result_get_zero");
        addRule(
            {call("__quantum__rt__result_equal", "result"_cap = _, "zero"_cap = get_zero), replace_branch_negative});

        addRule(
            {call("__quantum__rt__result_equal", "zero"_cap = get_zero, "result"_cap = _), replace_branch_negative});
    }

    void RuleFactory::optimiseResultOne()
    {
        auto replace_branch_positive = [](Builder& builder, Value* val, Captures& cap, Replacements& replacements) {
            auto cond = llvm::dyn_cast<llvm::Instruction>(val);
            if (cond == nullptr)
            {
                return false;
            }
            auto result = cap["result"];
            // Replacing result
            auto orig_instr = llvm::dyn_cast<llvm::Instruction>(val);
            if (orig_instr == nullptr)
            {
                return false;
            }

            auto                      module = orig_instr->getModule();
            auto                      fnc    = module->getFunction("__quantum__qis__read_result__body");
            std::vector<llvm::Value*> arguments;
            arguments.push_back(result);

            if (!fnc)
            {
                std::vector<llvm::Type*> types;
                types.resize(arguments.size());
                for (uint64_t i = 0; i < types.size(); ++i)
                {
                    types[i] = arguments[i]->getType();
                }

                auto return_type = llvm::Type::getInt1Ty(val->getContext());

                llvm::FunctionType* fnc_type = llvm::FunctionType::get(return_type, types, false);
                fnc                          = llvm::Function::Create(
                    fnc_type, llvm::Function::ExternalLinkage, "__quantum__qis__read_result__body", module);
            }

            builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(val));
            auto new_call = builder.CreateCall(fnc, arguments);

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

            return true;
        };

        /*
          Here is an example IR for which we want to make a match:

          %1 = call %Result* @__quantum__rt__result_get_one()
          %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
          br i1 %2, label %then0__1, label %continue__1
        */

        // Variations of get_one
        auto get_one = call("__quantum__rt__result_get_one");
        addRule({call("__quantum__rt__result_equal", "result"_cap = _, "one"_cap = get_one), replace_branch_positive});

        addRule({call("__quantum__rt__result_equal", "one"_cap = get_one, "result"_cap = _), replace_branch_positive});
    }

    void RuleFactory::disableReferenceCounting()
    {
        //  removeFunctionCall("__quantum__rt__array_update_reference_count");
        removeFunctionCall("__quantum__rt__string_update_reference_count");
        removeFunctionCall("__quantum__rt__result_update_reference_count");
    }

    void RuleFactory::disableAliasCounting()
    {
        //  removeFunctionCall("__quantum__rt__array_update_alias_count");
        removeFunctionCall("__quantum__rt__string_update_alias_count");
        removeFunctionCall("__quantum__rt__result_update_alias_count");
    }

    void RuleFactory::disableStringSupport()
    {
        removeFunctionCall("__quantum__rt__fail");
        removeFunctionCall("__quantum__rt__message");
        removeFunctionCall("__quantum__rt__string_update_alias_count");

        removeFunctionCall("__quantum__rt__string_create");
        removeFunctionCall("__quantum__rt__string_get_data");
        removeFunctionCall("__quantum__rt__string_get_length");
        removeFunctionCall("__quantum__rt__string_update_reference_count");
        removeFunctionCall("__quantum__rt__string_concatenate");
        removeFunctionCall("__quantum__rt__string_equal");

        removeFunctionCall("__quantum__rt__int_to_string");
        removeFunctionCall("__quantum__rt__double_to_string");
        removeFunctionCall("__quantum__rt__bool_to_string");
        removeFunctionCall("__quantum__rt__result_to_string");
        removeFunctionCall("__quantum__rt__pauli_to_string");
        removeFunctionCall("__quantum__rt__qubit_to_string");
        removeFunctionCall("__quantum__rt__range_to_string");
        removeFunctionCall("__quantum__rt__bigint_to_string");
    }

    ReplacementRulePtr RuleFactory::addRule(ReplacementRule&& rule)
    {
        auto ret = std::make_shared<ReplacementRule>(std::move(rule));

        rule_set_.addRule(ret);

        return ret;
    }

    void RuleFactory::setDefaultIntegerWidth(uint32_t v)
    {
        default_integer_width_ = v;
    }

} // namespace quantum
} // namespace microsoft
