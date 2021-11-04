# Proposal: QIR Adaptor Tool Specification

This document discusses a tool that transforms QIR into a restricted version of the QIR (known as a profile).
We aim to make a specification for a generic tool that allows the user to:

1. Create or use an existing profile without the need of writing code.
2. Validate that a QIR is compliant with the specific profile.
3. Generate a profile compliant QIR from a generic unconstrained QIR (if possible).

This document sets out to motivate and demonstrate feasibility of building such a tool.

## Motivation

It is anticipated that most usages of the QIR specification will need to only use a subset of it. These subsets
may further be subject to constraints such as how one allocates or acquire a qubit handle. We refer to such a subset with
constraints as a profile. For instance, it is likely that early versions of quantum hardware will have a limited
set of classical instructions available. With this in mind, the vendor or user of said hardware would define a profile
that only contains a specified subset. One example of such a profile is the base profile,
which only allows function calls and branching, but no arithmetic, classical memory, or classical registers.

The generation of QIR according to the spec with no constraints would typically be performed by the frontend. A couple
of examples are Q# or OpenQASM 2.0/3.0. However, for the generated QIR to be practical it is necessary to reduce it using a profile
which is compatible with the target platform:

```text
┌──────────────────────┐
│                      │
│       Frontend       │
│                      │
└──────────────────────┘
           │
           │
           ▼
┌──────────────────────┐
│                      │
│         QIR          │
│                      │
└──────────────────────┘
           │
   QIR Adaptor Tool
           │
           ▼
┌──────────────────────┐
│                      │
│    Restricted QIR    │
│                      │
└──────────────────────┘
```

We propose that such a reduction could be done using the LLVM passes infrastructure to compose a profile
which would map the QIR to a subset of the available instructions with any required constraints.

## Feasibility Study

In order to demonstrate feasibility of this proposal, we have built a proof-of-concept prototype based on LLVM passes which allows transformation from a generic QIR into one which does not have support for dynamic qubit allocation.

This transformation is considered to be the smallest, non-trivial case of QIR transformation we can perform which demonstrates the feasibility of this proposal.

To demonstrate the feasibility of this proposal, we use Q# as a frontend and will attempt to map the following code

```
namespace Feasibility {
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation Run() : Unit {
        use qs = Qubit[3];
        for q in 8..10 {
            X(qs[q - 8]);
        }
    }
}
```

to the base profile. We will do so using a combination of existing LLVM passes and custom written passes which are specific to the QIR. The above code is interesting as it is not base profile compliant with regards to two aspects: 1) Qubit allocation is not allowed and 2) arithmetic operations are not supported. Using the Q# QIR generator, the `Run` functions body becomes:

```
define internal void @Feasibility__Run__body() {
entry:
  %qs = call %Array* @__quantum__rt__qubit_allocate_array(i64 3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qs, i32 1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %q = phi i64 [ 8, %entry ], [ %4, %exiting__1 ]
  %0 = icmp sle i64 %q, 10
  br i1 %0, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %1 = sub i64 %q, 8
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %qs, i64 %1)
  %3 = bitcast i8* %2 to %Qubit**
  %qubit = load %Qubit*, %Qubit** %3, align 8
  call void @__quantum__qis__x__body(%Qubit* %qubit)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %4 = add i64 %q, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %qs, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qs)
  ret void
}
```

After applying the our demo profile transformation, the QIR is reduced to:

```
define void @Feasibility__Run__Interop() local_unnamed_addr #0 {
entry:
  call void @__quantum__qis__x__body(%Qubit* null)
  call void @__quantum__qis__x__body(%Qubit* nonnull inttoptr (i64 1 to %Qubit*))
  call void @__quantum__qis__x__body(%Qubit* nonnull inttoptr (i64 2 to %Qubit*))
  ret void
}
```

We note that we successfully have eliminated loops, arithmetic operations, dynamic qubit allocation and alias counting - all operations which are not supported by the base profile.

## Goal

We envision the tool to work as a stand-alone command line tool which can either validate or generate a QIR in accordance with a given profile. To validate, one would run:

```language
qat -p profile.yaml --validate unvalidated-qir.ll
```

In a similar fashion, generation is performed by adding `--generate` to the command line:

```language
qat -p profile.yaml --generate qir.ll > qir-profile.ll
```

Default behaviour of the tool is that it always validates the generated profile. This behaviour can be disabled by

```language
qat -p profile.yaml --generate --no-validate qir.ll > qir-profile.ll
```

# Profile Specification

Every profile is specified through a YAML file which defines an object at the top-level. This object must contain the fields `name` and `displayName`:

```yaml
name: profile-name
displayName: Profile Name
# ...
```

Additionally, top level also contains the fields `version` and `mode`. The version refers to the QIR version which forms the basis for the specification and `mode` explains how the profile is defined.
The final two top level fields are lists named `specification` and `generation`. These contains the specification and generation producedure, respectively.

## Specification

The default `mode` of specification is by `feature` which means that specification describes the feature set available. Alternatively, one can specify a profile by `limitation`. As an example profile for

```yaml
name: profile-name
displayName: Profile Name
version: 1.0
mode: feature
specification:
  functions:
    - __quantum__qis__toffoli__body
    - __quantum__qis__cnot__body
    - __quantum__qis__cz__body
    - __quantum__qis__h__body
    - __quantum__qis__mz__body
    - __quantum__qis__reset__body
    - __quantum__qis__rx__body
    - __quantum__qis__ry__body
    - __quantum__qis__rz__body
    - __quantum__qis__s__body
    - __quantum__qis__s__adj
    - __quantum__qis__t__body
    - __quantum__qis__t__adj
    - __quantum__qis__x__body
    - __quantum__qis__y__body
    - __quantum__qis__z__body
  instructions:
    - call
    - br
    - ret
    - inttoptr
# ...
```

This specification describes that 16 quantum instructions are available and 4 classical operations of the full QIR spec. Contrary, a specification by `limitation` could be as follows:

```yaml
name: profile-name
displayName: Profile Name
version: 1.0
mode: limitation
specification:
  functions:
    - __quantum__rt__array_update_alias_count
    - __quantum__rt__array_update_reference_count
  instructions:
    - br
# ...
```

This profile specifies a system that does not allow reference and alias counting and neither have support for branching, but otherwise has the full QIR vesion 1.0 available.

## Generation specification

To achieve the QIR generation in the feasibility section we made use of a number of different passes in order fold constants, unroll loops and map qubit allocations to static allocations. Based on this, we propose that generators are specified by creating a pipeline of LLVM passes to analyse and transform the QIR:

```yaml
name: profile-name
displayName: Profile Name
# ...
generation:
  - passName: loopUnroll
  - passName: functionInline
  - passName: useStaticQubitAllocation
  - passName: eliminateClassicalMemoryUsage
  - passName: ignoreCall
    config:
      names:
        - __quantum__rt__array_update_alias_count
        - __quantum__rt__array_update_reference_count
```

For those passes which are defined specifically for QIR we we allow configuration to be passed to them. This will allow the end-user to fine-tune the behaviour of profile generator.

# Library outline

This is a placeholder for describing the outline of the QAT library. The aim is to create a dynamic library where we can add new components that allow to extend the QIR profile generation components with more passes and/or spefication options.
