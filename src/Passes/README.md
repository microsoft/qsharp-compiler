# Q# Passes for LLVM

This library defines [LLVM passes](https://llvm.org/docs/Passes.html) used for analysing, optimising and transforming the IR. The Q# pass library is a dynamic library that can be compiled and ran separately from the
rest of the project code. While it is not clear whether this possible at the moment, we hope that it will be possible to write passes that enforce the [QIR specification](https://github.com/microsoft/qsharp-language/tree/main/Specifications/QIR).

## What do LLVM passes do?

Before getting started, we here provide a few examples of classical use cases for [LLVM passes](https://llvm.org/docs/Passes.html). You find additional [instructive examples here][1].

**Example 1: Transformation**. As a first example of what [LLVM passes](https://llvm.org/docs/Passes.html) can do, we look at optimisation. Consider a compiler which
compiles

```c
double test(double x) {
    return (1+2+x)*(x+(1+2));
}
```

into following IR:

```
define double @test(double %x) {
entry:
        %addtmp = fadd double 3.000000e+00, %x
        %addtmp1 = fadd double %x, 3.000000e+00
        %multmp = fmul double %addtmp, %addtmp1
        ret double %multmp
}
```

This code is obviously inefficient as we could get rid of one operation by rewritting the code to:

```c
double test(double x) {
    double y = 3+x;
    return y * y;
}
```

One purpose of [LLVM passes](https://llvm.org/docs/Passes.html) is to allow automatic transformation from the above IR to the IR:

```
define double @test(double %x) {
entry:
        %addtmp = fadd double %x, 3.000000e+00
        %multmp = fmul double %addtmp, %addtmp
        ret double %multmp
}
```

**Example 2: Analytics**. Another example of useful passes are those generating and collecting statistics about the program. For instance, one analytics program
makes sense for classical programs is to count instructions used to implement functions. Take the C program:

```c
int foo(int x)
{
  return x;
}

void bar(int x, int y)
{
  foo(x + y);
}

int main()
{
  foo(2);
  bar(3, 2);

  return 0;
}
```

which produces follow IR (without optimisation):

```language
define dso_local i32 @foo(i32 %0) #0 {
  %2 = alloca i32, align 4
  store i32 %0, i32* %2, align 4
  %3 = load i32, i32* %2, align 4
  ret i32 %3
}

define dso_local void @bar(i32 %0, i32 %1) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i32, align 4
  store i32 %0, i32* %3, align 4
  store i32 %1, i32* %4, align 4
  %5 = load i32, i32* %3, align 4
  %6 = load i32, i32* %4, align 4
  %7 = add nsw i32 %5, %6
  %8 = call i32 @foo(i32 %7)
  ret void
}

define dso_local i32 @main() #0 {
  %1 = alloca i32, align 4
  store i32 0, i32* %1, align 4
  %2 = call i32 @foo(i32 2)
  call void @bar(i32 3, i32 2)
  ret i32 0
}
```

A stat pass for this code, would collect following statisics:

```text
Stats for 'foo'
===========================
Opcode          # Used
---------------------------
load            1
ret             1
alloca          1
store           1
---------------------------

Stats for 'bar'
===========================
Opcode          # Used
---------------------------
load            2
add             1
ret             1
alloca          2
store           2
call            1
---------------------------

Stats for 'main'
===========================
Opcode          # Used
---------------------------
ret             1
alloca          1
store           1
call            2
---------------------------
```

**Example 3: Code validation**. A third use case is code validation. For example, one could write a pass to check whether bounds are exceeded on [static arrays][2].
Note that this is a non-standard usecase as such analysis is usually made using the AST rather than at the IR level.

**References**

- [1] https://github.com/banach-space/llvm-tutor#analysis-vs-transformation-pass
- [2] https://github.com/victor-fdez/llvm-array-check-pass

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

The default target is `all`. Other valid targets are the name of the folders in `libs/` found in the passes root.

## Running a pass

You can run a pass using [opt](https://llvm.org/docs/CommandGuide/opt.html) as follows:

```sh
cd examples/ClassicalIrCommandline
make emit-llvm-bc
opt -load-pass-plugin ../../{Debug,Release}/libOpsCounter.{dylib,so} --passes="print<operation-counter>" -disable-output classical-program.bc
```

For a gentle introduction, see examples.

## Creating a new pass

To make it easy to create a new pass, we have created a few templates to get you started quickly:

```sh
% ./manage create-pass HelloWorld
Available templates:

1. Function Pass

Select a template:1
```

At the moment you only have one choice which is a function pass. Over time we will add additional templates. Once you have instantiated your template, you are ready to build it:

```sh
% mkdir Debug
% cd Debug
% cmake ..
-- The C compiler identification is AppleClang 12.0.5.12050022
-- The CXX compiler identification is AppleClang 12.0.5.12050022
(...)
-- Configuring done
-- Generating done
-- Build files have been written to: /Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/Debug

% make

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
% cd ../examples/ClassicalIrCommandline
% make
% opt -load-pass-plugin ../../Debug/libs/libHelloWorld.{dylib,so} --passes="hello-world" -disable-output classical-program.ll
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
