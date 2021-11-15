# QIR Passes

This document contains a brief introduction on how to use the QIR passes. A more comprehensive [walk-through is found here](./docs/src/index.md).

## Building

To build the tool, create a new build directory and switch to that directory:

```sh
mkdir Debug
cd Debug/
```

To build the library, first configure CMake from the build directory

```sh
cmake ..
```

and then make your target:

```sh
make qat
```

For full instructions on dependencies and how to build, follow [these instructions](./docs/src/UserGuide/BuildingLibrary.md).

## Getting started

Once the project is built (see next sections), you can transform a QIR according to a profile as follows:

```sh
./qir/qat/Apps/qat --generate --profile base -S path/to/example.ll
```

Likewise, you can validate that a QIR follows a specification by running (Note, not implemented yet):

```sh
./qir/qat/Apps/qat --validate --profile base -S path/to/example.ll
```

## Documentation

Most of the documentation is available [directly on Github](./docs/src/index.md). If you need API documentation, you can build and run it by typing

```sh
make serve-docs
```

in the root folder of the passes directory.
