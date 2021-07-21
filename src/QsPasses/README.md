# Q# Passes for LLVM

This library defines LLVM passes used for optimising and transforming the IR.

## Getting started

The Q# pass library is a dynamic library that can be compiled and ran separately from the
rest of the project code.

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
make stylecheck
```

To test that the code compiles and tests passes run

```sh
make tests
```

Finally, to analyse the code, run

```sh
make lint
```

You can run all processes by running:

```sh
make runci
```

As `clang-tidy` and `clang-format` acts slightly different from version to version and on different platforms, it is recommended
that you use a docker image to perform these steps.

# TODOs

Look at https://github.com/llvm-mirror/clang-tools-extra/blob/master/clang-tidy/tool/run-clang-tidy.py
