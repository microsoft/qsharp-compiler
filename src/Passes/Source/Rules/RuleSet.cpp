#include "Rules/RuleSet.hpp"

#include "AllocationManager/AllocationManager.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/ReplacementRule.hpp"

#include <iostream>
#include <vector>
namespace microsoft {
namespace quantum {

RuleSet::RuleSet()
{

  using namespace microsoft::quantum::patterns;

  // Shared pointer to be captured in the lambdas of the patterns
  // Note that you cannot capture this as the reference is destroyed upon
  // copy. Since PassInfoMixin requires copy, such a construct would break
  auto qubit_alloc_manager  = AllocationManager::createNew();
  auto result_alloc_manager = AllocationManager::createNew();

  // Pattern 0 - Find type
  ReplacementRule rule0;

  auto get_element =
      Call("__quantum__rt__array_get_element_ptr_1d", "arrayName"_cap = _, "index"_cap = _);
  rule0.setPattern("cast"_cap = BitCast("getElement"_cap = get_element));
  rule0.setReplacer([qubit_alloc_manager](Builder &, Value *, Captures &cap, Replacements &) {
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
  ReplacementRule rule1a;
  rule1a.setPattern(std::move(load_pattern));

  // Replacement details
  rule1a.setReplacer([qubit_alloc_manager](Builder &builder, Value *val, Captures &cap,
                                           Replacements &replacements) {
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
  rules_.emplace_back(std::move(rule1a));

  ReplacementRule rule1b;
  rule1b.setPattern(Call("__quantum__rt__qubit_allocate"));

  // Replacement details
  rule1b.setReplacer(
      [qubit_alloc_manager](Builder &builder, Value *val, Captures &, Replacements &replacements) {
        // Getting the type pointer
        auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
        if (ptr_type == nullptr)
        {
          return false;
        }

        // Allocating qubit
        qubit_alloc_manager->allocate(val->getName().str(), 1);

        // Computing the index by getting the current index value and offseting by
        // the offset at which the qubit array is allocated.
        auto offset = qubit_alloc_manager->getOffset(val->getName().str());

        // Creating a new index APInt that is shifted by the offset of the allocation
        // TODO(tfr): Get the bitwidth size from somewhere
        auto idx = llvm::APInt(64, offset);

        // Computing offset
        auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

        // TODO(tfr): Understand what the significance of the addressspace is in relation to the
        // QIR. Activate by uncommenting:
        // ptr_type = llvm::PointerType::get(ptr_type->getElementType(), 2);
        auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
        instr->takeName(val);

        // Replacing the instruction with new instruction
        replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

        return true;
      });
  rules_.emplace_back(std::move(rule1b));

  // Rule 6 - perform static allocation and delete __quantum__rt__qubit_allocate_array
  ReplacementRule rule6;
  auto            allocate_call = Call("__quantum__rt__qubit_allocate_array", "size"_cap = _);
  rule6.setPattern(std::move(allocate_call));

  rule6.setReplacer(
      [qubit_alloc_manager](Builder &, Value *val, Captures &cap, Replacements &replacements) {
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
      });

  rules_.emplace_back(std::move(rule6));

  // Rule 8 - standard array allocation
  /*
  ReplacementRule rule8;
  auto            allocate_array_call =
      Call("__quantum__rt__array_create_1d", "elementSize"_cap = _, "size"_cap = _);
  rule8.setPattern(std::move(allocate_array_call));

  rule8.setReplacer(
      [qubit_alloc_manager](Builder &, Value *val, Captures &cap, Replacements &replacements) {
        auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["size"]);
        if (cst == nullptr)
        {
          return false;
        }

        auto llvm_size = cst->getValue();
        qubit_alloc_manager->allocate(val->getName().str(), llvm_size.getZExtValue(), true);
        replacements.push_back({llvm::dyn_cast<Instruction>(val), nullptr});
        return true;
      });

  rules_.emplace_back(std::move(rule8));
*/
  // Rule 10 - track stored values
  auto get_target_element = Call("__quantum__rt__array_get_element_ptr_1d",
                                 "targetArrayName"_cap = _, "targetIndex"_cap = _);
  auto get_value_element = Call("__quantum__rt__array_get_element_ptr_1d", "valueArrayName"_cap = _,
                                "targetValue"_cap = _);
  auto target            = BitCast("target"_cap = get_target_element);
  auto value             = BitCast("value"_cap = get_element);

  auto store_pattern = Store(target, value);

  ReplacementRule rule10;
  rule10.setPattern(std::move(store_pattern));

  rule10.setReplacer([qubit_alloc_manager](Builder &, Value *, Captures &, Replacements &) {
    llvm::errs() << "Found store pattern"
                 << "\n";
    return false;
  });
  rules_.emplace_back(std::move(rule10));

  // Measurements
  auto replace_measurement = [result_alloc_manager](Builder &builder, Value *val, Captures &cap,
                                                    Replacements &replacements) {
    // Getting the type pointer
    auto ptr_type = llvm::dyn_cast<llvm::PointerType>(val->getType());
    if (ptr_type == nullptr)
    {
      return false;
    }

    // Allocating qubit
    result_alloc_manager->allocate(val->getName().str(), 1);

    // Computing the index by getting the current index value and offseting by
    // the offset at which the qubit array is allocated.
    auto offset = result_alloc_manager->getOffset(val->getName().str());

    // Creating a new index APInt that is shifted by the offset of the allocation
    // TODO(tfr): Get the bitwidth size from somewhere
    auto idx = llvm::APInt(64, offset);

    // Computing offset
    auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);

    // TODO(tfr): Understand what the significance of the addressspace is in relation to the
    // QIR. Activate by uncommenting:
    // ptr_type = llvm::PointerType::get(ptr_type->getElementType(), 2);
    auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
    instr->takeName(val);

    auto module   = llvm::dyn_cast<llvm::Instruction>(val)->getModule();
    auto function = module->getFunction("__quantum__qis__mz__body");

    std::vector<llvm::Value *> arguments;
    arguments.push_back(cap["qubit"]);
    arguments.push_back(instr);

    if (!function)
    {
      std::vector<llvm::Type *> types;
      for (auto &arg : arguments)
      {
        types.push_back(arg->getType());
      }

      auto return_type = llvm::Type::getVoidTy(val->getContext());

      llvm::FunctionType *fnc_type = llvm::FunctionType::get(return_type, types, false);
      function = llvm::Function::Create(fnc_type, llvm::Function::ExternalLinkage,
                                        "__quantum__qis__mz__body", module);
    }

    // Ensuring we are inserting after the instruction being deleted
    builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(val)->getNextNode());

    builder.CreateCall(function, arguments);

    // Replacing the instruction with new instruction
    // TODO: (tfr): insert instruction before and then replace, with new call
    replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

    return true;
  };

  rules_.emplace_back(Call("__quantum__qis__m__body", "qubit"_cap = _), replace_measurement);

  // Quantum comparisons
  auto get_one                 = Call("__quantum__rt__result_get_one");
  auto replace_branch_positive = [](Builder &builder, Value *val, Captures &cap,
                                    Replacements &replacements) {
    llvm::errs() << "Found branch"
                 << "\n";

    auto result = cap["result"];
    auto cond   = llvm::dyn_cast<llvm::Instruction>(cap["cond"]);

    // Replacing result
    auto                       module   = llvm::dyn_cast<llvm::Instruction>(val)->getModule();
    auto                       function = module->getFunction("__quantum__qir__read_result");
    std::vector<llvm::Value *> arguments;
    arguments.push_back(result);

    if (!function)
    {
      std::vector<llvm::Type *> types;
      for (auto &arg : arguments)
      {
        types.push_back(arg->getType());
      }

      auto return_type = llvm::Type::getInt1Ty(val->getContext());

      llvm::FunctionType *fnc_type = llvm::FunctionType::get(return_type, types, false);
      function = llvm::Function::Create(fnc_type, llvm::Function::ExternalLinkage,
                                        "__quantum__qir__read_result", module);
    }

    builder.SetInsertPoint(llvm::dyn_cast<llvm::Instruction>(result)->getNextNode());
    auto new_call = builder.CreateCall(function, arguments);
    new_call->takeName(cond);

    for (auto &use : cond->uses())
    {
      llvm::User *user = use.getUser();
      user->setOperand(use.getOperandNo(), new_call);
    }

    // Deleting the previous condition and function to fetch one
    replacements.push_back({cond, nullptr});
    replacements.push_back({cap["one"], nullptr});

    return false;
  };

  // Variations of get_one
  rules_.emplace_back(Branch("cond"_cap = Call("__quantum__rt__result_equal", "result"_cap = _,
                                               "one"_cap = get_one),
                             _, _),
                      replace_branch_positive);
  rules_.emplace_back(Branch("cond"_cap = Call("__quantum__rt__result_equal", "one"_cap = get_one,
                                               "result"_cap = _),
                             _, _),
                      replace_branch_positive);

  // Functions that we do not care about
  rules_.emplace_back(Call("__quantum__rt__array_update_alias_count", _, _), deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__string_update_alias_count", _, _), deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__result_update_alias_count", _, _), deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__array_update_reference_count", _, _),
                      deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__string_update_reference_count", _, _),
                      deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__result_update_reference_count", _, _),
                      deleteInstruction());

  rules_.emplace_back(Call("__quantum__rt__qubit_release_array", _), deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__qubit_release", _), deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__string_create", _), deleteInstruction());
  rules_.emplace_back(Call("__quantum__rt__string_release", _), deleteInstruction());

  rules_.emplace_back(Call("__quantum__rt__message", _), deleteInstruction());
}

bool RuleSet::matchAndReplace(Instruction *value, Replacements &replacements)
{
  Captures captures;
  for (auto const &rule : rules_)
  {
    // Checking if the rule is matched and keep track of captured nodes
    if (rule.match(value, captures))
    {

      // If it is matched, we attempt to replace it
      llvm::IRBuilder<> builder{value};
      if (rule.replace(builder, value, captures, replacements))
      {
        return true;
      }
    }
  }
  return false;
}

}  // namespace quantum
}  // namespace microsoft
