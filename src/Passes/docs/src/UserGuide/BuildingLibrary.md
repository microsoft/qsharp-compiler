# Building the tool

## Dependencies

This library is written in C++ and depends on:

- LLVM 11

Additional development dependencies include:

- CMake
- clang-format
- clang-tidy
- Python 3

## Configuring the build directory

To build the tool, create a new build directory and switch to that directory:

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

## Building the documentation

To build the documentation Docker image, run:

```
make documentation
```

To serve the documentation locally, run:

```
make serve-docs
```
