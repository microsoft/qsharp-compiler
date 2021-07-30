# Basic profile target

In this document, we outline a set of proposed analysis and transformatino passes based on the considerations of compiling an Adder using Q# to generate OpenQASM with a restricted set of instructions available.

## Purpose

The purpose of this work is to create the tools to help transform a high-level language into assembly instructions compatible with a specific target.
The core problem encountered is that the classical control logic of quantum computers are unlikely to have an advanced instruction set. The [base profile](https://github.com/microsoft/qsharp-language/blob/ageller/profile/Specifications/QIR/Base-Profile.md)
describes an example of a highly restricted (classical) instruction with only `inttoptr`, `call`, `br` and `ret` available and the types support are integers and floating point precision.

The general code generation pipeline follows these high-level three steps:

1. Source -> Initial QIR
2. Initial QIR -> Constrained QIR
3. Constrained QIR -> ASM

This work is concerned with the second step. For step 1 and 3, we will consider Q# as an example frontend and OpenQASM as an example backend. While OpenQASM allows allocation of qubit arrays, we will further restrict this work assuming that no such feature is available and that the target system has N qubits. As a result, the final program may or may not fit on the architecture.

## Frontend example: Q# code

The reference implementation of the Adder in Q# is provided here:

```
namespace Microsoft.Quantum.Qir.Emission {
    operation M(q : Qubit) : Result {
        body intrinsic;
    }
    operation ForEach<'T, 'U>(action : 'T => 'U, array : 'T[]) : 'U[] {
        mutable res = [];
        for item in array {
            set res += [action(item)];
        }
        return res;
    }
    operation X (q : Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }
    operation CNOT (c : Qubit, q : Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }
    operation Toffoli (a : Qubit, b : Qubit, c : Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }
    operation Majority(a : Qubit, b : Qubit, c : Qubit) : Unit is Adj + Ctl {
        CNOT(c, b);
        CNOT(c, a);
        Toffoli(a, b, c);
    }
    @EntryPoint()
    operation RunAdder() : Result[] {
        use a = Qubit[4];
        use b = Qubit[4];
        use (cin, cout) = (Qubit(), Qubit());
        X(a[0]);
        for q in b {
            X(q);
        }
        within {
            Majority(cin, b[0], a[0]);
            Majority(a[0], b[1], a[1]);
            Majority(a[1], b[2], a[2]);
            Majority(a[2], b[3], a[3]);
        } apply {
            CNOT(a[3], cout);
        }
        return ForEach(M, b);
    }
}
```

## Backend example: OpenQASM 2.0

A reference implementation of the Adder in OpenQASM, is provided here:

```
// quantum ripple-carry adder from Cuccaro et al, quant-ph/0410184
OPENQASM 2.0;
include "qelib1.inc";
gate majority a,b,c
{
  cx c,b;
  cx c,a;
  ccx a,b,c;
}
gate unmaj a,b,c
{
  ccx a,b,c;
  cx c,a;
  cx a,b;
}
qreg cin[1];
qreg a[4];
qreg b[4];
qreg cout[1];
creg ans[5];
// set input states
x a[0]; // a = 0001
x b;    // b = 1111
// add a to b, storing result in b
majority cin[0],b[0],a[0];
majority a[0],b[1],a[1];
majority a[1],b[2],a[2];
majority a[2],b[3],a[3];
cx a[3],cout[0];
unmaj a[2],b[3],a[3];
unmaj a[1],b[2],a[2];
unmaj a[0],b[1],a[1];
unmaj cin[0],b[0],a[0];
measure b[0] -> ans[0];
measure b[1] -> ans[1];
measure b[2] -> ans[2];
measure b[3] -> ans[3];
measure cout[0] -> ans[4];
```

The corresponding QIR was manually developed based on this implementation and the resulting QIR is

```language
; a total of 5 classical bits are used, so one byte is sufficient
@quantum_results = global [1 x i8]

; Since neither read_qresult nor write_qresult are used in this example,
; the QIR generator is free to not include their implementations.

; The "majority" custom gate
define void @majority_body(%Qubit addrspace(2)* %a, %Qubit addrspace(2)* %b, %Qubit addrspace(2)* %c) {
    call void quantum_qis_cnot(%Qubit addrspace(2)* %c, %Qubit addrspace(2)* %b);
    call void quantum_qis_cnot(%Qubit addrspace(2)* %c, %Qubit addrspace(2)* %a);
    call void quantum_qis_toffoli(%Qubit addrspace(2)* %a, %Qubit addrspace(2)* %b, %Qubit addrspace(2)* %c);
}

; The "unmaj" custom gate
define void @unmaj_body(%Qubit addrspace(2)* %a, %Qubit addrspace(2)* %b, %Qubit addrspace(2)* %c) {
    call void quantum_qis_toffoli(%Qubit addrspace(2)* %a, %Qubit addrspace(2)* %b, %Qubit addrspace(2)* %c);
    call void quantum_qis_cnot(%Qubit addrspace(2)* %c, %Qubit addrspace(2)* %a);
    ; This line matches the OpenQASM sample, but is incorrect. The first qubit should be %c, not %a.
    call void quantum_qis_cnot(%Qubit addrspace(2)* %a, %Qubit addrspace(2)* %b);
}

; The main OpenQASM program
define void @quantum_main() {
entry:
    ; The "cin" qubit register is mapped to device qubit 0.
    ; The "a" qubit register is mapped to device qubits 1-4.
    ; The "b" qubit register is mapped to device qubit2 5-8.
    ; The "cout" qubit register is mapped to device qubit 9.
    %a0 = inttoptr i32 1 to %Qubit addrspace(2)*
    call void @quantum_qis_x_body(%Qubit addrspace(2)* %a0)

    %b0 = inttoptr i32 5 to %Qubit addrspace(2)*
    call void @quantum_qis_x_body(%Qubit addrspace(2)* %b0)
    %b1 = inttoptr i32 6 to %Qubit addrspace(2)*
    call void @quantum_qis_x_body(%Qubit addrspace(2)* %b1)
    %b2 = inttoptr i32 7 to %Qubit addrspace(2)*
    call void @quantum_qis_x_body(%Qubit addrspace(2)* %b2)
    %b3 = inttoptr i32 8 to %Qubit addrspace(2)*
    call void @quantum_qis_x_body(%Qubit addrspace(2)* %b3)

    %cin = inttoptr i32 0 to %Qubit addrspace(2)*
    call void @majority_body(%Qubit addrspace(2)* %cin, %Qubit addrspace(2)* %b0, %Qubit addrspace(2)* %a0)
    %a1 = inttoptr i32 2 to %Qubit addrspace(2)*
    call void @majority_body(%Qubit addrspace(2)* %a0, %Qubit addrspace(2)* %b1, %Qubit addrspace(2)* %a1)
    %a2 = inttoptr i32 3 to %Qubit addrspace(2)*
    call void @majority_body(%Qubit addrspace(2)* %a1, %Qubit addrspace(2)* %b2, %Qubit addrspace(2)* %a2)
    %a3 = inttoptr i32 4 to %Qubit addrspace(2)*
    call void @majority_body(%Qubit addrspace(2)* %a2, %Qubit addrspace(2)* %b3, %Qubit addrspace(2)* %a3)
    %cout = inttoptr i32 9 to %Qubit addrspace(2)*
    call void @quantum_qis_cnot_body(%Qubit addrspace(2)* %a3, %Qubit addrspace(2)* %cout)
    call void @unmaj_body(%Qubit addrspace(2)* %a2, %Qubit addrspace(2)* %b3, %Qubit addrspace(2)* %a3)
    call void @unmaj_body(%Qubit addrspace(2)* %a1, %Qubit addrspace(2)* %b2, %Qubit addrspace(2)* %a2)
    call void @unmaj_body(%Qubit addrspace(2)* %a0, %Qubit addrspace(2)* %b1, %Qubit addrspace(2)* %a1)
    call void @unmaj_body(%Qubit addrspace(2)* %cin, %Qubit addrspace(2)* %b0, %Qubit addrspace(2)* %a0)

    call void quantum_qis_mz_body(%Qubit addrspace(2)* %b0, 0)
    call void quantum_qis_mz_body(%Qubit addrspace(2)* %b1, 1)
    call void quantum_qis_mz_body(%Qubit addrspace(2)* %b2, 2)
    call void quantum_qis_mz_body(%Qubit addrspace(2)* %b3, 3)
    call void quantum_qis_mz_body(%Qubit addrspace(2)* %cout, 4)
}
```

## Required transformations

- Drop reference function calls:

```
__quantum__rt__array_update_alias_count
__quantum__rt__capture_update_reference_count
```

- Replace allocations

```
%a = call %Array* @__quantum__rt__qubit_allocate_array(i64 3) ; <- allocation size 3, offset 0
%b = call %Array* @__quantum__rt__qubit_allocate_array(i64 4) ; <- allocation size 4, offset 3
```

with

```
%a = inttoptr i32 0 to %Qubit addrspace(7)*
%b = inttoptr i32 3 to %Qubit addrspace(7)*
```

Note that the two allocations `a` and `b` gives an address space of 7 with `a` being offset at `0` and `b` being offset `3`.

- Replace qubit references

```
%0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2) ; <- Index
%1 = bitcast i8* %0 to %Qubit**             ; <- Casting, which can disregarded
%q = load %Qubit*, %Qubit** %1, align 8     ; <- Type %Qubit
```

with

```
%q = inttoptr i32 2 to %Qubit addrspace(7)*
```
