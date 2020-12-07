# QIR Pre-Release Instructions

This file contains directions for using the pre-release version of the QIR
code generation.
This code is still in flux and will change frequently.
In particular, some features are still missing.

See [this list, below](#to-dos) for some specific open issues and work items.

## Important Notes

- The [Llvm.NET ](https://github.com/UbiquityDotNET/Llvm.NET) library does not currently
  work on Linux or Mac. Currently QIR must be generated on a Windows machine. This will be
  fixed before release.

## Branch Structure

QIR development is on the `feature/qir` branch.

## Using the Q# Compiler to Generate QIR

Build the QsCompiler solution locally.

From the repository's root directory, you can run the following:

```bash
dotnet run --project src/QsCompiler/CommandLineTool/ build --qir QIR s --input <input-file> examples/QIR/QirCore.qs examples/QIR/QirTarget.qs --proj <output-file>
```

Note that if the Q# file you're compiling contains an operation with the `@EntryPoint` attribute,
you will need to add a `--build-exe` switch to the command:

```bash
dotnet run --project src/QsCompiler/CommandLineTool/ build --qir QIR s --build-exe --input <input-file> examples/QIR/QirCore.qs examples/QIR/QirTarget.qs --proj <output-file>
```

Alternatively, you can go to the build output directory and run the Q# compiler executable
from there.
The build output directory is `src/QsCompiler/CommandLineTool/bin/debug/netcoreapp3.1`,
and the Q# compiler is `qsc.exe`.
In this case, the command line is just:

```bash
./qsc.exe build --qir QIR s --input <input-file> <root-dir>/examples/QIR/QirCore.qs <root-dir>/examples/QIR/QirTarget.qs --proj <output-file>
```

Again, you will need the `--build-exe` switch if you have an entry point defined.

This will create two files: an LLVM file with a `.ll` extension,
and a text file containing any issues with a `.log` extension.
Both will have the base name set from the `--proj` command line argument.

Assuming everything went OK, the log file will contain the single line "No errors."
If there were errors during the Q# compilation, they will be listed in the log file, one per line.
If the Q# compilation succeeded but the QIR/LLVM generation threw an exception,
the exception and stack trace will appear in the LLVM file.
If the Q# compilation and LLVM generation succeeded, but the generated LLVM did not validate,
then the LLVM validation status will appear in the log file.

### Entry Points

If you have a Q# operation with the `@EntryPoint` attribute, the QIR generator
will create an additional C-callable wrapper function for the entry point.
The name of this wrapper is the same as the full, namespace-qualified name
of the entry point, with periods replaced by underscores.

The entry point wrapper performs some translation between QIR types and standard
C types.
In particular, a QIR array parameter will be replaced in the input signature by
two parameters, an i64 array count and a pointer to the element type.

The entry point wrapper function gets tagged with an LLVM "EntryPoint" attribute.
Note that this is a custom attribute, rather than metadata, so that passes should
not drop it.

The QIR generator does not currently create a `main(argc, argv)` that translates
string values to QIR types that would allow QIR compiled to executable through clang
to be executed from the command line.

## Q# Core

Usually the Q# compiler reads in various built-ins from a Core.qs file that is included by
default in project builds.
When used as above to build individual files, though, this file is not included.

A version of the Core.qs file suitable for use with the QIR tool is included as
`examples/QIR/QirCore.qs`.
As described above, this file should be included in all QIR generation builds.

## Target Definitions

The Q# compiler and LLVM generator do not have a built-in quantum instruction set.
Instead, they rely on two Q# segments that serve three purposes:

- One segment defines the target-level instruction set; that is, the set of LLVM functions that
  should be called to perform quantum operations.
- A second segment defines the user-level instruction set; that is, the set of Q# operations that
  user code should use in algorithms. Usually this would match the microsoft.quantum.intrinsic
  namespace, and indeed would be specified in that namespace.
- The second segment also specifies the mapping from user-level instructions to target-level
  instructions. This mapping is written in Q#, and can be arbitrarily complex; there's no
  assumption that there's a simple 1-1 map from user-level to target-level instructions.

These two segments can be in the same file as the application Q# code, or in a separate file,
or in two separate files, one per segment.

A sample Q# file suitable that contains both segments suitable for use with the QIR tool is included as
`examples/QIR/QirTarget.qs`.
As described above, this file should be included in all QIR generation builds.

The sample file does not define intrinsics for the full set of Q# primitives.
You may need to extend this file if it is missing intrinsics you require.

### Target-level instruction set

The target-level instructions can all be defined in a single namespace, or in many namespaces.
Both samples define them in the `Microsoft.Quantum.Instructions` namespace, but that can be
changed if desired.
The actual namespace name is not significant.

Target-level instruction definitions follow this pattern:

```qsharp
    @Intrinsic("x")
    operation PhysX (qb : Qubit) : Unit
    {
        body intrinsic;
    }
```

- The `Intrinsic` attribute is required. The string argument, with a `quantum.qis.` prefix,
  becomes the name of the global LLVM function that implements this instruction.
- The signature of the LLVM function is derived directly from the Q# operation's signature.
- Target-level operations should only have `body` specializations, not `adjoint` or `controlled`.
- The name of the target-level instructions can be arbitrary. By keeping it different from the
  name of the user-level instruction, we remove the need to explicitly qualify the name with the
  namespace when we refer to it in the user-level instruction definition.
- At the moment, the intrinsic can only have a `body` specialization. This may change in the
  future, but also might not.

### User-level instruction set

Generally, user-level instructions should be defined in the `Microsoft.Quantum.Intrinsic`
namespace to match standard Q# usage.
Similarly, the instruction names should match the names in the standard Q# library in that
namespace, so that other existing Q# code will find the instructions correctly.

User-level instruction definitions follow this pattern:

```qsharp
    @Inline()
    operation X(qb : Qubit) : Unit
    is Adj {
        body
        {
            PhysX(qb);  
        }
        adjoint self;
    }
```

- The `Inline` attribute is optional. If it appears, the QIR generator will in-line the
  implementation of the user-level instruction, so that the generated QIR directly calls the
  target-level instruction. Otherwise the generated QIR will call into the LLVM function that
  implements the user-level instruction, which will contain a call into the target-level
  instruction.
- The implementation of the user-level instruction can contain arbitrary Q# code. For instance,
  both sample files define the adjoint of a rotation as the rotation by the negative of the angle.
  Once I add support for controlled, the controlled `X` implementation will have Q# code that will
  start by checking the number of control qubits and either calling a target-level `CNOT` if 
  there's one, executing one of the various Toffoli decompositions if there are two, and using
  multiple Toffolis with an ancilla qubit if there are more than two.
- User-level instructions will often have both `adjoint` and `controlled` specializations. Because
  target-level instructions don't have such specializations, the only directive that can be used
  to define these specializations is `self` to mark self-adjoint instructions.

## QIR Specification

The QIR specification is on the [Q# language repository](https://github.com/microsoft/qsharp-language/tree/main/Specifications/QIR).

## To-Dos

- [ ] Add debug information to QIR, issue #637.
- [ ] Generate a `main` function for an entrypoint, issue #638
- [ ] Handle structure/tuple mapping for entrypoints, issue #639
- [ ] Add metadata to QIR from Q# attributes, issue #640
- [ ] Code clean-up, issue #641
- [ ] Port to Linux and MacOS, issue #642
