# Q# Passes for LLVM

This library defines LLVM passes used for optimising and transforming the IR.

The Q# pass library is a dynamic library that can be compiled and ran separately from the
rest of the project code.

## What does LLVM passes do?

Example 1: Optimisation

```
define double @test(double %x) {
entry:
        %addtmp = fadd double 3.000000e+00, %x
        %addtmp1 = fadd double %x, 3.000000e+00
        %multmp = fmul double %addtmp, %addtmp1
        ret double %multmp
}
```

```
define double @test(double %x) {
entry:
        %addtmp = fadd double 3.000000e+00, %x
        ret double %addtmp
}
```

Example 2: Analytics

Example 3: Validation

## Out-of-source Pass

This library is build as set of out-of-source-passes. All this means is that we will not be downloading the LLVM repository and modifying this repository directly. You can read more [here](https://llvm.org/docs/CMake.html#cmake-out-of-source-pass).

# Getting started

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

## Running a pass

You can run a pass using `opt` as follows:

```sh
opt -load-pass-plugin ../../{Debug,Release}/libQSharpPasses.{dylib,so} --passes="operation-counter" -disable-output classical-program.bc
```

For a gentle introduction, see examples.

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
% opt -load-pass-plugin ../../Debug/libQSharpPasses.dylib -enable-debugify  --passes="operation-counter" -disable-output   classical-program.bc
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
