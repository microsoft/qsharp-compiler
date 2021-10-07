# Creating a profile transformation in C++

## Profile transformation

TODO(tfr): Yet to be written

## Profile transformation as pass

As an example of how one can implement a new profile pass, we here show the implementation details of our example pass which allows mapping the teleportation code to the base profile:

```c++
        pb.registerPipelineParsingCallback([](StringRef name, FunctionPassManager &fpm,
                                              ArrayRef<PassBuilder::PipelineElement> /*unused*/) {
          // Base profile
          if (name == "restrict-qir<base-profile>")
          {
            RuleSet rule_set;

            // Defining the mapping
            auto factory = RuleFactory(rule_set);

            factory.useStaticQuantumArrayAllocation();
            factory.useStaticQuantumAllocation();
            factory.useStaticResultAllocation();

            factory.optimiseBranchQuatumOne();
            //  factory.optimiseBranchQuatumZero();

            factory.disableReferenceCounting();
            factory.disableAliasCounting();
            factory.disableStringSupport();

            fpm.addPass(TransformationRulePass(std::move(rule_set)));
            return true;
          }

          return false;
        });
      }};
```

Transformations of the IR will happen on the basis of what rules are added to the rule set. The purpose of the factory is to make easy to add rules that serve a single purpose as well as making a basis for making rules unit testable.

## Implementing new rules

Implementing new rules consists of two steps: Defining a pattern that one wish to replace and implementing the corresponding replacement logic. Inside a factory member function, this look as follows:

```c++
  auto get_element =
      Call("__quantum__rt__array_get_element_ptr_1d", "arrayName"_cap = _, "index"_cap = _);
  auto cast_pattern = BitCast("getElement"_cap = get_element);
  auto load_pattern = Load("cast"_cap = cast_pattern);

  addRule({std::move(load_pattern), access_replacer});
```

where `addRule` adds the rule to the current rule set.

## Capturing patterns

The pattern defined in this snippet matches IR like:

```c++
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 0)
  %1 = bitcast i8* %0 to %Qubit**
  %2 = load %Qubit*, %Qubit** %1, align 8
```

In the above rule, the first and a second argument of `__quantum__rt__array_get_element_ptr_1d` is captured as `arrayName` and `index`, respectively. Likewise, the bitcast instruction is captured as `cast`. Each of these captures will be available inside the replacement function `access_replacer`.

## Implementing replacement logic

After a positive match is found, the lead instruction alongside a IRBuilder, a capture table and a replacement table is passed to the replacement function. Here is an example on how one can access the captured variables to perform a transformation of the IR:

```c++
  auto access_replacer = [qubit_alloc_manager](Builder &builder, Value *val, Captures &cap,
                                               Replacements &replacements) {
    // ...
    auto cst = llvm::dyn_cast<llvm::ConstantInt>(cap["index"]);
    // ...
    auto llvm_size = cst->getValue();
    auto offset    = qubit_alloc_manager->getOffset(cap["arrayName"]->getName().str());

    auto idx = llvm::APInt(llvm_size.getBitWidth(), llvm_size.getZExtValue() + offset);
    auto new_index = llvm::ConstantInt::get(builder.getContext(), idx);
    auto instr = new llvm::IntToPtrInst(new_index, ptr_type);
    instr->takeName(val);

    // Replacing the lead instruction with a the new instruction
    replacements.push_back({llvm::dyn_cast<Instruction>(val), instr});

    // Deleting the getelement and cast operations
    replacements.push_back({llvm::dyn_cast<Instruction>(cap["getElement"]), nullptr});
    replacements.push_back({llvm::dyn_cast<Instruction>(cap["cast"]), nullptr});

    return true;
  };
```
