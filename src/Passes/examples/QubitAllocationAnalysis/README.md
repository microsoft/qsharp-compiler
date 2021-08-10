# QubitAllocationAnalysis

## Quick start

The following depnds on:

-   A working LLVM installation, including paths correctly setup
-   CMake
-   C#, Q# and the .NET framework

Running following command

```sh
make run
```

will first build the pass, then build the QIR using Q# following by removing the noise using `opt` with optimisation level 1. Finally, it will execute the analysis pass and should provide you with information about qubit allocation in the Q# program defined in `ConstSizeArray/ConstSizeArray.qs`.

## Detailed run

From the Passes root (two levels up from this directory), make a new build

```sh
mkdir Debug
cd Debug
cmake ..
```

and then compile the `QubitAllocationAnalysis`:

```sh
make QubitAllocationAnalysis
```

Next return `examples/QubitAllocationAnalysis` and enter the directory `ConstSizeArray` to build the QIR:

```sh
make analysis-example.ll
```

or execute the commands manually,

```sh
dotnet build ConstSizeArray.csproj
opt -S qir/ConstSizeArray.ll -O1  > ../analysis-example.ll
make clean
```

Returning to `examples/QubitAllocationAnalysis`, the pass can now be ran by executing:

```sh
opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll
```

## Example cases

Below we will consider a few different examples. You can run them by updating the code in `ConstSizeArray/ConstSizeArray.qs` and executing `make run` from the `examples/QubitAllocationAnalysis` folder subsequently. You will need to delete `analysis-example.ll` between runs.

### Trivially constant

This is the simplest example we can think of:

```qsharp
namespace Example {
    @EntryPoint()
    operation QuantumProgram() : Unit {
        use qubits = Qubit[3];
    }
}
```

The corresponding QIR is:

```
; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Array = type opaque

define internal fastcc void @Example__QuantumProgram__body() unnamed_addr {
entry:
  %qubits = call %Array* @__quantum__rt__qubit_allocate_array(i64 3)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits)
  ret void
}

; (...)
```

Running the pass procudes following output:

```
opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll

Example__QuantumProgram__body
====================

qubits is trivially static with 3 qubits.
```

### Dependency case

In some cases, a qubit array will be compile time constant in size if the function arguments
provided are compile-time constants. One example of this is:

```
namespace Example {
    @EntryPoint()
    operation Main() : Int
    {
        QuantumProgram(3);
        QuantumProgram(4);
        return 0;
    }

    operation QuantumProgram(x: Int) : Unit {
        use qubits = Qubit[x];
    }
}
```

The corresponding QIR is

```
; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Array = type opaque
%String = type opaque

define internal fastcc void @Example__Main__body() unnamed_addr {
entry:
  call fastcc void @Example__QuantumProgram__body(i64 3)
  call fastcc void @Example__QuantumProgram__body(i64 4)
  ret void
}

define internal fastcc void @Example__QuantumProgram__body(i64 %x) unnamed_addr {
entry:
  %qubits = call %Array* @__quantum__rt__qubit_allocate_array(i64 %x)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits)
  ret void
}
; ( ... )

```

The analyser returns following output:

```
opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll

Example__QuantumProgram__body
====================

qubits depends on x being constant to be static.

```

### Summary case

Finally, we do a summary case that demonstrates some of the more elaborate cases:

```
namespace Example {
    @EntryPoint()
    operation Main() : Int
    {
        QuantumProgram(3,2,1);
        QuantumProgram(4,9,4);
        return 0;
    }

    function X(value: Int): Int
    {
        return 3 * value;
    }

    operation QuantumProgram(x: Int, h: Int, g: Int) : Unit {
        let z = x * (x + 1) - 47;
        let y = 3 * x;

        use qubits0 = Qubit[9];
        use qubits1 = Qubit[(y - 2)/2-z];
        use qubits2 = Qubit[y - g];
        use qubits3 = Qubit[h];
        use qubits4 = Qubit[X(x)];
    }
}
```

We will omit the QIR in the documenation as it is a long. The output of the anaysis is:

```
opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll

Example__QuantumProgram__body
====================

qubits0 is trivially static with 9 qubits.
qubits1 depends on x being constant to be static.
qubits2 depends on x, g being constant to be static.
qubits3 depends on h being constant to be static.
qubits4 is dynamic.
```
