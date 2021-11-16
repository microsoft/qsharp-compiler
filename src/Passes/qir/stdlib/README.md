# qir-stdlib

## How to link and build an executable

Building the standard library uses the following LLVM tools: `llvm-as`, `llc` and `llvm-link` alongside with `clang`. The test suite consists of two sets of tests: C tests and IR tests. Each of these sets needs a slightly different flow to build them.

To manually build the C test,

```
  mkdir -p target/

  # Creating the stdlib LL file
  clang -O1 -mllvm -disable-llvm-optzns -S -emit-llvm  -c src/stdlib.c -o lib/stdlib.ll

  # Converting it to bitcode
  llvm-as lib/stdlib.ll -o target/stdlib.bc

  # Converting it to an object
  llc -filetype=obj -o target/stdlib.o target/stdlib.bc

  # Linking it against our C test
  clang -O1 target/stdlib.o tests/csuite/test.c -o target/c-test-suite
```

To build the IR test write following commands:

```
  mkdir -p target/

  # Creating the stdlib LL file
  clang -O1 -mllvm -disable-llvm-optzns -S -emit-llvm  -c src/stdlib.c -o lib/stdlib.ll

  # Linking against the LL test
  llvm-link tests/irsuite/test.ll lib/stdlib.ll  -o target/ir-test-suite.bc

  # Converting it to a bytecode
  llc -filetype=obj -o target/ir-test-suite.o target/ir-test-suite.bc

  # Creating the executable
  clang -O1 target/ir-test-suite.o -o target/ir-test-suite
```
