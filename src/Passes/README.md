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

and then make your target

```sh
make qat
```

For full instructions on dependencies and how to build, follow [these instructions](./docs/src/DeveloperGuide/Building.md).

## Getting started

Once the project is built (see next sections), you can transform a QIR according to a profile as follows:

```sh
./Source/Apps/qat --generate --profile baseProfile -S ../examples/QirAllocationAnalysis/analysis-example.ll
```

Likewise, you can validate that a QIR follows a specification by running (Note, not implemented yet):

```sh
./Source/Apps/qat --validate --profile baseProfile -S ../examples/QirAllocationAnalysis/analysis-example.ll
```

## Documentation

Most of the documentation is available [directly on Github](./docs/src/index.md). If you need API documentation, you can build and run it by typing

```sh
make serve-docs
```

in the root folder of the passes directory.
