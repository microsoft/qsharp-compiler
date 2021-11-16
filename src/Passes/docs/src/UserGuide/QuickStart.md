# Quick start

Before we start, you will need to build the QAT tool. To this end, enter `src/Passes` from the root of the repository and create a new folder `Debug`. Then run `cmake ..` and use `make` to build `qat`:

```sh
mkdir Debug
cd Debug
cmake ..
make qat
```

A more detailed documentation of this step is available in the [build steps section](./BuildingLibrary.md). Once the build has been completed successfully, we will create a QIR in order to have an example code we can apply a profile to. The next step does not need to be completed if you already have a QIR. We will be using the Q# front end to generate the QIR and use the example `SimpleLoop`. However, you are free to choose another example in the `QirExamples` folder and/or another frontend. Go to the folder `./QirExamples/SimpleLoop/QSharpVersion` and type

```sh
make qir/Example.ll
```

This will generate a QIR that will have the path `./QirExamples/SimpleLoop/QSharpVersion/qir/Example.ll` relative to the project root.
By using an application called QAT you can transform the QIR that was generated into a QIR that is tailored to a specific profile. Performing a transformation of the QIR from the `./Debug` folder is done by typing the following commands:

```sh
./qir/qat/Apps/qat --apply --profile base -S ../QirExamples/SimpleLoop/QSharpVersion/qir/Example.ll
```

Validation of QIR profiles is not supported by the tool at the moment. We're working on this feature, and will add the quickstart documentation here when it is available.

## Example: SimpleLoop

For the sake of demonstration, we will look at the result of applying the profile to the previous Q# code and its corresponding QIR. However, instead of giving all the 3316 lines of the original QIR, we instead present the frontend code for the QIR. It is always possible to recreate the QIR from the step that was previously explained if you are curious:

```
namespace SimpleLoop {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;

    function Value(r : Result) : Int {
         return r == Zero ? 122 | 1337;
    }

    @EntryPoint()
    operation RunMain() : Int {
        let nrIter = 5;
        mutable ret = 1;
        for _ in 1 .. nrIter {
            use q = Qubit();
            H(q);
            let r = MResetZ(q);
            set ret = Value(r);
        }

        return ret;
    }
}
```

The tool was ran with three options: `--apply`, `--profile base` and `-S`. First, it tells the tool to apply the profile to the QIR so that it can become a profile-specific QIR according to the profile selected. By specifying the value `baseProfile` to the argument `--profile`, we select which profile to use in the tool and the third argument ensures that LLVM IR will be printed to the terminal in a human readable format. The resulting code emitted (omitting declarations) is:

```ll
; ModuleID = 'QSharpVersion/qir/Example.ll'
source_filename = "QSharpVersion/qir/Example.ll"

; ...

define void @SimpleLoop__Main() local_unnamed_addr #0 {
entry:
  tail call void @__quantum__qis__h__body(%Qubit* null)
  tail call void @__quantum__qis__mz__body(%Qubit* null, %Result* null)
  tail call void @__quantum__qis__reset__body(%Qubit* null)
  %0 = tail call %Result* @__quantum__rt__result_get_zero()
  %1 = tail call i1 @__quantum__rt__result_equal(%Result* null, %Result* %0)
  tail call void @__quantum__qis__h__body(%Qubit* null)
  tail call void @__quantum__qis__mz__body(%Qubit* null, %Result* nonnull inttoptr (i64 1 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* null)
  %2 = tail call %Result* @__quantum__rt__result_get_zero()
  %3 = tail call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 1 to %Result*), %Result* %2)
  tail call void @__quantum__qis__h__body(%Qubit* null)
  tail call void @__quantum__qis__mz__body(%Qubit* null, %Result* nonnull inttoptr (i64 2 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* null)
  %4 = tail call %Result* @__quantum__rt__result_get_zero()
  %5 = tail call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 2 to %Result*), %Result* %4)
  tail call void @__quantum__qis__h__body(%Qubit* null)
  tail call void @__quantum__qis__mz__body(%Qubit* null, %Result* nonnull inttoptr (i64 3 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* null)
  %6 = tail call %Result* @__quantum__rt__result_get_zero()
  %7 = tail call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*), %Result* %6)
  tail call void @__quantum__qis__h__body(%Qubit* null)
  tail call void @__quantum__qis__mz__body(%Qubit* null, %Result* nonnull inttoptr (i64 4 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* null)
  %8 = tail call %Result* @__quantum__rt__result_get_zero()
  %9 = tail call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 4 to %Result*), %Result* %8)
  %10 = select i1 %9, i64 122, i64 1337
  %11 = tail call %String* @__quantum__rt__int_to_string(i64 %10)
  ret void
}

; …

attributes #0 = { "EntryPoint" "requiredQubits"="1" }
```

A notable feature of the resulting code is that there are no loops, and qubit registers are assigned at compile time, meaning that you can identify each qubit instance by its unique constant integer ID. These are features of the selected profile which does not support neither loops nor dynamic qubit allocation.
