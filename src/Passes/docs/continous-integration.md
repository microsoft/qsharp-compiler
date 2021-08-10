# Running tests

In order to run the tests, you first need to build the library. Assuming that this is already done and the corresponding build is in `Debug/`, run the tests from the `Debug` folder:

```
lit tests/ -v
-- Testing: 2 tests, 2 workers --
PASS: Quantum-Passes :: QubitAllocationAnalysis/case1.ll (1 of 2)
PASS: Quantum-Passes :: QubitAllocationAnalysis/case2.ll (2 of 2)

Testing Time: 0.27s
  Passed: 2
```

# Continuous integration

This component is the largest part of this PR. The continuous integration component includes:

1. Style formatting to ensure that everything looks the same. This includes checking that relevant copyrights are in place.
2. Static analysis
3. Unit testing

The automatic style enforcement is configurable with the ability to easily add or remove rules. Currently the source pipelines are defined as:

```python
SOURCE_PIPELINES = [
    {
        "name": "C++ Main",
        "src": path.join(PROJECT_ROOT, "libs"),

        "pipelines": {
            "hpp": [
                require_pragma_once,
                enforce_cpp_license,
                enforce_formatting
            ],
            "cpp": [
                enforce_cpp_license,
                enforce_formatting
            ]
        }
    },
    # ...
]
```

This part defines pipelines for `.hpp` files and `.cpp` files allowing the developer to add such requirements such as having copyright in the op of the source file and ensure that formatting follows that given by `.clang-format`.

Each of these CI stages can executed individually using `./manage` or you can run the entire CI process by invoking `./manage runci`. An example of what this may look like is here:

```zsh
./manage runci

2021-07-21 14:38:04,896 - FormatChecker - ERROR - /Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/src/OpsCounter/OpsCounter.cpp was not correctly formatted.
2021-07-21 14:38:04,899 - FormatChecker - ERROR - Your code did not pass formatting.

./manage stylecheck --fix-issues
./manage runci

-- Found LLVM 11.1.0
-- Using LLVMConfig.cmake in: /usr/local/opt/llvm@11/lib/cmake/llvm
-- Configuring done
-- Generating done
-- Build files have been written to: /Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/Debug
Consolidate compiler generated dependencies of target QSharpPasses
[ 50%] Building CXX object CMakeFiles/QSharpPasses.dir/src/OpsCounter/OpsCounter.cpp.o
[100%] Linking CXX shared library libQSharpPasses.dylib
ld: warning: directory not found for option '-L/usr/local/opt/llvm/lib'
[100%] Built target QSharpPasses
/Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/src/OpsCounter/OpsCounter.cpp:29:7: error: invalid case style for class 'LegacyOpsCounterPass' [readability-identifier-naming,-warnings-as-errors]
class LegacyOpsCounterPass : public FunctionPass
      ^~~~~~~~~~~~~~~~~~~~
      CLegacyOpsCounterPass
113345 warnings generated.
Suppressed 113345 warnings (113344 in non-user code, 1 NOLINT).
Use -header-filter=.* to display errors from all non-system headers. Use -system-headers to display errors from system headers as well.
1 warning treated as error
2021-07-21 14:38:40,191 - Linter - ERROR - /Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/src/OpsCounter/OpsCounter.cpp failed static analysis

# ISSUES FIXED MANUALLY
./manage runci

-- Found LLVM 11.1.0
-- Using LLVMConfig.cmake in: /usr/local/opt/llvm@11/lib/cmake/llvm
-- Configuring done
-- Generating done
-- Build files have been written to: /Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/Debug
Consolidate compiler generated dependencies of target QSharpPasses
[ 50%] Building CXX object CMakeFiles/QSharpPasses.dir/src/OpsCounter/OpsCounter.cpp.o
[100%] Linking CXX shared library libQSharpPasses.dylib
ld: warning: directory not found for option '-L/usr/local/opt/llvm/lib'
[100%] Built target QSharpPasses
-- Found LLVM 11.1.0
-- Using LLVMConfig.cmake in: /usr/local/opt/llvm@11/lib/cmake/llvm
-- Configuring done
-- Generating done
-- Build files have been written to: /Users/tfr/Documents/Projects/qsharp-compiler/src/QsPasses/Debug
Consolidate compiler generated dependencies of target QSharpPasses
[100%] Built target QSharpPasses
*********************************
No test configuration file found!
*********************************
```

The key idea here is to make it extremely easy to be complaint with the style guide, correct any issues that might come as a result of static analysis and at the same time enforce this when a PR is made.
