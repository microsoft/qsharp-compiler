# Introduction to profiles

In this document we discuss QIR profiles. A QIR profile describes a subset of the generic QIR functionality and conventions. It
is anticipated that most usages of the QIR specification will need to only use a subset of it. These subsets
may further be subject to constraints such as how one allocate or acquire a qubit handle. We refer to such a subset with
constraints as a profile. For instance, it is likely that early versions of quantum hardware will have a limited
set of classical instructions available. With this in mind, the vendor or user of said hardware would define a profile
that only contains a specified subset. One example of such a profile is the base profile,
which only allows function calls and branching, but no arithmetic, classical memory, or classical registers.

The generation of QIR according to the spec with no constraints would typically be performed by the frontend. A couple
of examples are Q# or OpenQASM 2.0/3.0. However, for the generated QIR to be practical it is necessary to reduce it using a profile
which is compatible with the target platform:

```text
┌──────────────────────┐
│       Frontend       │
└──────────────────────┘
           │
           │ QIR
           ▼
┌──────────────────────┐
│  QIR Adaptor Tool    │ <─────── QIR Profile
└──────────────────────┘
           │
           │ Adapted QIR
           ▼
┌──────────────────────┐
│       Backend        │
└──────────────────────┘
```

As an example, a hardware based quantum platform may only have support for sequential gates with no branching or ability for subroutines. Likewise, some quantum platforms only allow for a single measurement at the end of executing the pipeline of quantum gates. Profiles suppose to express these nuances and restrictions which are absent in the generic QIR.

## Generic QIR specification

See [Quantum Intermediate Representation (QIR)](https://github.com/microsoft/qsharp-language/tree/main/Specifications/QIR)

## Pipeline profile

This profile assumes a quantum system where qubits and result registers fixed in availability. That is to say, that one target may have 25 qubits and 10 result registers.

The pipeline profile is the profile with the least classical logic available. It only supports [`call`](https://llvm.org/docs/LangRef.html#call-instruction), [`inttoptr`](https://llvm.org/docs/LangRef.html#inttoptr-to-instruction), 64-bit integers [`i64`](https://llvm.org/docs/LangRef.html#integer-type), qubit ids [`Qubit*`](https://github.com/microsoft/qsharp-language/blob/main/Specifications/QIR/Data-Types.md#opaque-types) and result ids `Result*`. It does not provide a runtime and only intrinsic quantum instructions are available to this profile. This profile is intended for client-host type infrastructure where a gate pipeline is uploaded to the client quantum system and executed one or more times. Measurements are always performed at the end of the execution and all available results. This profile only allows for defining a single function that takes no arguments and has no return type.

Available types
| Typename | Description |
|----------|---|
| Result | Used as pointer type within the ID for constant integers |
| Qubit | Used as pointer type within the ID for constant integers |

As an example of how these types can be used:

```
%0 = Qubit* inttoptr 1 to Qubit*
```

which expresses that we store the handle to qubit with ID 1 in the variable `%0`. Result registers are referred to in a similar manner.

## Base Profile

The base profile is a slight advancement to the pipeline profile

Available types
| Typename | | |
|----------|---|---|
| Array | | |
| Result | | |
| Qubit | | |

Available quantum intrinsic functions
| | | |
|---|---|---|
| | | |
| | | |
| | | |

Available runtime functions
| | | |
|---|---|---|
| | | |
| | | |
| | | |

Available IR functionality
| | | |
|---|---|---|
| call | | |
| ret | | |
| | | |
