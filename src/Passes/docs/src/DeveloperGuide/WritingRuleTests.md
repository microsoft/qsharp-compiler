# Writing rule tests

To make it easy to write tests for transformation rules, we have created two components to ease the burden of writing tests: `DefaultProfileGenerator` and `IrManipulationTestHelper`. The `DefaultProfileGenerator` is a profile that is dynamically defined when instatiated through a configuration lambda function.

## Creating the profile

Creating the profile using the `DefaultProfileGenerator` is done by first defining the lambda function and then instantiating the `DefaultProfileGenerator` with the lambda function to define the profile. Using the `RuleFactory`, a profile for transforming the single qubit allocations is created as follows:

```c++
  auto configure_profile = [](RuleSet &rule_set) {
    auto factory = RuleFactory(rule_set);

    factory.useStaticQubitAllocation();
  }

  auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));
```

This profile is intended to transform

```
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)
  ret i8 0
```

by replacing all allocations with integers and stripping all release calls

```
  %qubit = inttoptr i64 0 to %Qubit*
  tail call void @__quantum__qis__h__body(%Qubit* %qubit)
  ret i8 0
```

## Creating the IR

In order to assist the testing of the above profile, we create a helper class which defines the IR we want to work on. To this end we make use of `IrManipulationTestHelper` which provides a number of shorthand functions to generate and test IR transformations. We start by defining the IR:

```c++
  auto ir_manip = std::make_shared<IrManipulationTestHelper>();

  ir_manip->declareOpaque("Qubit");

  ir_manip->declareFunction("%Qubit* @__quantum__rt__qubit_allocate()");
  ir_manip->declareFunction("void @__quantum__rt__qubit_release(%Qubit*)");
  ir_manip->declareFunction("void @__quantum__qis__h__body(%Qubit*)");

  std::string script = R"script(
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)
  )script";

  assert(ir_manip->fromBodyString(script)); // Will fail if the IR is invalid
```

If we wish to verify the IR, we can print it by using the member function `toString` or by accessing the module directly:

```c++
  std::cout << ir_manip->toString() << std::endl;
  // OR
  llvm::errs() << *ir_manip->module() << "\n";
```

## Applying the profile to the IR

The `IrManipulationTestHelper` contains a member function to run the profile on the IR to transform the module. The default behaviour of this helper function is to run without debug output at a `O0` level to ensure that LLVM does not interfere with the intended test. The optimisation level and debug mode can be changed through the function calls second and third argument, but for the sake of simplicity, we will assume we are using `O0` here:

```c++
  ir_manip->applyProfile(profile);
```

This will run the above generated rule set on the IR we have supplied. At this point, we could print the IR to the screen and use LIT to perform that actually transformation test. However, to keep this test framework self-contained and easy to use, we supply LIT-like functionality. This has the benefit that the tests do not rely on Python and the LIT framework and that the tooling around the test is substantially simpler.

## Testing the modified IR

Like before, we can investigate the IR by printing it and as such we could write a test that compared the full IR against an expected string. However, even minor changes in the IR (such as interchanged declarations) would break the test even if the changes would not change the semantics of the code. Instead, the `IrManipulationTestHelper` has another helper function `hasInstructionSequence` which allow us to scan for a sequence of instructions in the body of the main function. In our case,
we expect following two instructions (in order):

```
  %qubit = inttoptr i64 0 to %Qubit*
  tail call void @__quantum__qis__h__body(%Qubit* %qubit)
```

The corresponding test code is as follows:

```c++
  EXPECT_TRUE(ir_manip->hasInstructionSequence({
    "%qubit = inttoptr i64 0 to %Qubit*",
    "tail call void @__quantum__qis__h__body(%Qubit* %qubit)"
  }));
```

By design, the test would pass as long as these two instructions are found (in order) within the full set of instructions of the function body. For instance, a valid IR for this test is

```
  call void printHelloWorld()
  %qubit = inttoptr i64 0 to %Qubit*
  %q2 = inttoptr i64 1 to %Qubit*
  %q3 = inttoptr i64 2 to %Qubit*
  tail call void @__quantum__qis__h__body(%Qubit* %q3)
  tail call void @__quantum__qis__h__body(%Qubit* %qubit)
  tail call void @__quantum__qis__h__body(%Qubit* %q2)
```

but would fail

```
  tail call void @__quantum__qis__h__body(%Qubit* %qubit)
  %qubit = inttoptr i64 0 to %Qubit*
```

and

```
  %qubit = inttoptr i64 0 to %Qubit*
```

as the first has the wrong order of the calls and the second is missing one instruction.
