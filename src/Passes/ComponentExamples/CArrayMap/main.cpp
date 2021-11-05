// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Generators/ProfileGenerator.hpp"
#include "Llvm/Llvm.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/RuleSet.hpp"
#include "TransformationRulesPass/TransformationRulesPass.hpp"

using namespace microsoft::quantum;

extern "C" void loadComponent(ProfileGenerator *generator);
void            activateAllocatorReplacement(RuleSet &ruleset);
void            removeArrayCopies(RuleSet &ruleset);
void            replaceAccess(RuleSet &ruleset);

class CArrayMapConfig
{
public:
  using String = std::string;

  void setup(ConfigurationManager &config)
  {
    config.setSectionName("CArrayMap", "Transformations to enable C-style array allocation.");
    config.addParameter(replace_allocators_, "replace-allocators",
                        "Replace allocators with C++ allocators.");

    config.addParameter(remove_array_copies_, "remove-array-copies",
                        "Remove all array copies and replace them with the original array.");

    config.addParameter(repalce_access_operators_, "replace-access-operators",
                        "Assumes that allocators use continuous memory.");
  }

  bool removeArrayCopies() const
  {
    return remove_array_copies_;
  }

  bool replaceAccess() const
  {
    return repalce_access_operators_;
  }

  bool replaceAllocators() const
  {
    return replace_allocators_;
  }

private:
  bool replace_allocators_{true};
  bool remove_array_copies_{true};
  bool repalce_access_operators_{true};
};

void activateAllocatorReplacement(RuleSet &ruleset)
{

  using namespace microsoft::quantum::notation;
  auto replacer = [](ReplacementRule::Builder &builder, ReplacementRule::Value *val,
                     ReplacementRule::Captures &    captures,
                     ReplacementRule::Replacements &replacements) {
    auto instr = llvm::dyn_cast<llvm::Instruction>(val);

    if (instr == nullptr)
    {
      return false;
    }

    auto size_cst = llvm::dyn_cast<llvm::ConstantInt>(captures["size"]);
    if (size_cst == nullptr)
    {
      return false;
    }

    auto element_count = captures["count"];
    if (element_count == nullptr)
    {
      return false;
    }

    builder.SetInsertPoint(instr->getNextNode());

    // dest_type ===
    auto sext       = builder.CreateSExt(size_cst, element_count->getType());
    auto total_size = builder.CreateNSWMul(element_count, sext);

    auto module   = instr->getModule();
    auto function = module->getFunction("_Znam");

    std::vector<llvm::Value *> arguments;
    arguments.push_back(total_size);

    if (!function)
    {
      std::vector<llvm::Type *> types;
      types.resize(arguments.size());
      for (uint64_t i = 0; i < types.size(); ++i)
      {
        types[i] = arguments[i]->getType();
      }

      auto return_type = llvm::PointerType::getUnqual(llvm::Type::getInt8Ty(val->getContext()));

      llvm::FunctionType *fnc_type = llvm::FunctionType::get(return_type, types, false);
      function = llvm::Function::Create(fnc_type, llvm::Function::ExternalLinkage, "_Znam", module);
    }

    auto new_allocator_call = builder.CreateCall(function, arguments);
    auto bitcast            = builder.CreateBitCast(new_allocator_call, instr->getType());

    instr->replaceAllUsesWith(bitcast);

    replacements.push_back({llvm::dyn_cast<llvm::Instruction>(val), nullptr});
    return true;
  };

  ruleset.addRule(
      {call("__quantum__rt__array_create_1d", "size"_cap = _, "count"_cap = _), replacer});
}

void replaceAccess(RuleSet &ruleset)
{

  using namespace microsoft::quantum::notation;
  auto replacer = [](ReplacementRule::Builder &builder, ReplacementRule::Value *val,
                     ReplacementRule::Captures &    captures,
                     ReplacementRule::Replacements &replacements) {
    auto instr = llvm::dyn_cast<llvm::Instruction>(val);
    if (instr == nullptr)
    {
      return false;
    }

    auto size_cst = llvm::dyn_cast<llvm::ConstantInt>(captures["size"]);
    if (size_cst == nullptr)
    {
      return false;
    }

    auto index = llvm::dyn_cast<llvm::ConstantInt>(captures["index"]);
    if (index == nullptr)
    {
      return false;
    }

    auto i8typeptr = llvm::dyn_cast<llvm::PointerType>(val->getType());
    if (i8typeptr == nullptr)
    {
      return false;
    }

    builder.SetInsertPoint(instr->getNextNode());
    auto array = captures["array"];

    auto i8array_ptr = builder.CreateBitCast(array, i8typeptr);
    auto sext        = builder.CreateSExt(size_cst, index->getType());
    auto position    = builder.CreateNSWMul(index, sext);
    auto element = builder.CreateInBoundsGEP(i8typeptr->getElementType(), i8array_ptr, position);

    instr->replaceAllUsesWith(element);

    replacements.push_back({instr, nullptr});
    return true;
  };

  ruleset.addRule(
      {call("__quantum__rt__array_get_element_ptr_1d",
            "array"_cap = call("__quantum__rt__array_create_1d", "size"_cap = _, "count"_cap = _),
            "index"_cap = _),
       replacer});

  ruleset.addRule(
      {callByNameOnly("__quantum__rt__array_update_reference_count"), deleteInstruction()});
  ruleset.addRule({callByNameOnly("__quantum__rt__array_update_alias_count"), deleteInstruction()});
}

void removeArrayCopies(RuleSet &ruleset)
{
  using namespace microsoft::quantum::notation;
  auto replacer = [](ReplacementRule::Builder &, ReplacementRule::Value *val,
                     ReplacementRule::Captures &    captures,
                     ReplacementRule::Replacements &replacements) {
    auto instr = llvm::dyn_cast<llvm::Instruction>(val);
    if (instr == nullptr)
    {
      return false;
    }

    auto orig_array = llvm::dyn_cast<llvm::Instruction>(captures["array"]);
    if (orig_array == nullptr)
    {
      return false;
    }

    instr->replaceAllUsesWith(orig_array);

    replacements.push_back({llvm::dyn_cast<llvm::Instruction>(val), nullptr});
    return true;
  };

  ruleset.addRule({call("__quantum__rt__array_copy", "array"_cap = _, _), replacer});
}

extern "C" void loadComponent(ProfileGenerator *generator)
{
  generator->registerProfileComponent<CArrayMapConfig>(
      "c-array-map", [](CArrayMapConfig const &cfg, ProfileGenerator *ptr, Profile &profile) {
        auto &ret = ptr->modulePassManager();

        if (cfg.removeArrayCopies())
        {
          RuleSet rule_set;
          removeArrayCopies(rule_set);
          auto config = TransformationRulesPassConfiguration::createDisabled();
          ret.addPass(TransformationRulesPass(std::move(rule_set), config, &profile));
        }

        if (cfg.replaceAccess())
        {
          RuleSet rule_set;
          replaceAccess(rule_set);
          auto config = TransformationRulesPassConfiguration::createDisabled();
          ret.addPass(TransformationRulesPass(std::move(rule_set), config, &profile));
        }

        if (cfg.replaceAllocators())
        {
          RuleSet rule_set;
          activateAllocatorReplacement(rule_set);
          auto config = TransformationRulesPassConfiguration::createDisabled();
          ret.addPass(TransformationRulesPass(std::move(rule_set), config, &profile));
        }
      });
}
