# Profile adoption tool

# Getting started

## Quick start

Once the project is built (see next sections), you can generate a new QIR as follows:

```sh
./Source/Apps/qat --generate --profile baseProfile ../examples/QubitAllocationAnalysis/analysis-example.ll
```

Likewise, you can validate that a QIR follows a specification by running:

```sh
./Source/Apps/qat --validate --profile baseProfile ../examples/QubitAllocationAnalysis/analysis-example.ll
```

## Dependencies

This library is written in C++ and depends on:

- LLVM

Additional development dependencies include:

- CMake
- clang-format
- clang-tidy

## Building the passes

To build the passes, create a new build directory and switch to that directory:

```sh
mkdir Debug
cd Debug/
```

To build the library, first configure CMake from the build directory

```sh
cmake ..
```

and then make your target

```sh
make [target]
```

The default target is `all`. Other valid targets are the name of the folders in `libs/` found in the passes root.

# Profile adoption tool

## Building QAT

First

```sh
cd Debug
make qat
```

then

```sh
./Source/Apps/qat
```

## Implementing a profile pass

As an example of how one can implement a new profile pass, we here show the implementational details of our example pass which allows mapping the teleportation code to the base profile:

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

### Capturing patterns

The pattern defined in this snippet matches IR like:

```c++
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 0)
  %1 = bitcast i8* %0 to %Qubit**
  %2 = load %Qubit*, %Qubit** %1, align 8
```

In the above rule, the first and a second argument of `__quantum__rt__array_get_element_ptr_1d` is captured as `arrayName` and `index`, respectively. Likewise, the bitcast instruction is captured as `cast`. Each of these captures will be available inside the replacement function `access_replacer`.

### Implementing replacement logic

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

# Passes

## Running a pass

You can run a pass using [opt](https://llvm.org/docs/CommandGuide/opt.html) as follows:

```sh
cd examples/ClassicalIrCommandline
make emit-llvm-bc
opt -load-pass-plugin ../../{Debug,Release}/libOpsCounter.{dylib,so} --passes="print<operation-counter>" -disable-output classical-program.bc
```

For a detailed tutorial, see examples.

## Creating a new pass

To make it easy to create a new pass, we have created a few templates to get you started quickly:

```sh
./manage create-pass HelloWorld
Available templates:

1. Function Pass

Select a template:1
```

At the moment you only have one choice which is a function pass. Over time we will add additional templates. Once you have instantiated your template, you are ready to build it:

```sh
mkdir Debug
cd Debug
cmake ..
-- The C compiler identification is AppleClang 12.0.5.12050022
-- The CXX compiler identification is AppleClang 12.0.5.12050022
(...)
-- Configuring done
-- Generating done
-- Build files have been written to: ./qsharp-compiler/src/Passes/Debug

make

[ 25%] Building CXX object libs/CMakeFiles/OpsCounter.dir/OpsCounter/OpsCounter.cpp.o
[ 50%] Linking CXX shared library libOpsCounter.dylib
[ 50%] Built target OpsCounter
[ 75%] Building CXX object libs/CMakeFiles/HelloWorld.dir/HelloWorld/HelloWorld.cpp.o
[100%] Linking CXX shared library libHelloWorld.dylib
[100%] Built target HelloWorld
```

Your new pass is ready to be implemented. Open `libs/HelloWorld/HelloWorld.cpp` to implement the details of the pass. At the moment, the
template will not do much except for print the function names of your code. To test your new pass go to the directory `examples/ClassicalIrCommandline`,
build an IR and run the pass:

```sh
cd ../examples/ClassicalIrCommandline
make
opt -load-pass-plugin ../../Debug/libs/libHelloWorld.{dylib,so} --passes="hello-world" -disable-output classical-program.ll
```

If everything worked, you should see output like this:

```sh
Implement your pass here: foo
Implement your pass here: bar
Implement your pass here: main
```

## CI

Before making a pull request with changes to this library, please ensure that style checks passes, that the code compiles,
unit test passes and that there are no erros found by the static analyser.

To setup the CI environment, run following commands

```sh
source develop.env
virtualenv develop__venv
source develop__venv/bin/activate
pip install -r requirements.txt
```

These adds the necessary environment variables to ensure that you have the `TasksCI` package and all required dependencies.

To check the style, run

```sh
./manage stylecheck
```

To test that the code compiles and tests passes run

```sh
./manage test
```

Finally, to analyse the code, run

```sh
./manage lint
```

You can run all processes by running:

```sh
./manage runci
```

As `clang-tidy` and `clang-format` acts slightly different from version to version and on different platforms, it is recommended
that you use a docker image to perform these steps. TODO(TFR): The docker image is not added yet and this will be documented in the future.

# Developer FAQ

## Pass does not load

One error that you may encounter is that an analysis pass does not load with output similar to this:

```sh
opt -load-pass-plugin ../../Debug/libQSharpPasses.dylib -enable-debugify  --passes="operation-counter" -disable-output   classical-program.bc
Failed to load passes from '../../Debug/libQSharpPasses.dylib'. Request ignored.
opt: unknown pass name 'operation-counter'
```

This is likely becuase you have forgotten to instantiate static class members. For instance, in the case of an instance of `llvm::AnalysisInfoMixin` you are required to have static member `Key`:

```cpp
class COpsCounterPass :  public llvm::AnalysisInfoMixin<COpsCounterPass> {
private:
  static llvm::AnalysisKey Key; //< REQUIRED by llvm registration
  friend struct llvm::AnalysisInfoMixin<COpsCounterPass>;
};
```

If you forget to instantiate this variable in your corresponding `.cpp` file,

```cpp
// llvm::AnalysisKey COpsCounterPass::Key; //< Uncomment this line to make everything work
```

everything will compile, but the pass will fail to load. There will be no linking errors either.

# Notes on QIR Profile Tool (QIR Adaptor Tool)

Target:

```
./qat -profile=base-profile.yml -S file.ir > adapted.ir
```

## Loading IR

https://stackoverflow.com/questions/22239801/how-to-load-llvm-bitcode-file-from-an-ifstream/22241953

## Load passes LLVM passes

https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/LangImpl04.html

## Load custom passes

## How to run analysis and transformation

https://stackoverflow.com/questions/53501830/running-standard-optimization-passes-on-a-llvm-module

## Profile specification

```yaml
name: profile-name
displayName: Profile Name
pipeline:
  - passName: loopUnroll
  - passName: functionInline
  - passName: staticQubitAllocation
  - passName: staticMemory
  - passName: ignoreCall
    config:
      functionName:
specification:
  - passName: requireNoArithmetic
  - passName: requireNoStaticAllocation
  - passName: requireReducedFunctionsAvailability
    config:
      functions:
        -
```

Decent YAML library: https://github.com/jbeder/yaml-cpp
