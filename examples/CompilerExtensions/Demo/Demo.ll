
%Range = type { i64, i64, i64 }
%Qubit = type opaque
%Result = type opaque
%Array = type opaque

@PauliI = constant i2 0
@PauliX = constant i2 1
@PauliY = constant i2 -1
@PauliZ = constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }

@Demo__PhaseEstimation = alias double (i64), double (i64)* @Demo__PhaseEstimation__body

define double @Demo__PhaseEstimation__body(i64 %nrIter) #0 {
entry:
  %mu = alloca double, align 8
  store double 7.951000e-01, double* %mu, align 8
  %sigma = alloca double, align 8
  store double 6.065000e-01, double* %sigma, align 8
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @Demo__Preparation__body(%Qubit* %target)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %0 = phi i64 [ 1, %entry ], [ %17, %exiting__1 ]
  %1 = icmp sle i64 %0, %nrIter
  br i1 %1, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %2 = load double, double* %mu, align 8
  %3 = call double @Microsoft__Quantum__Math__PI__body()
  %4 = load double, double* %sigma, align 8
  %5 = fmul double %3, %4
  %6 = fdiv double %5, 2.000000e+00
  %time = fsub double %2, %6
  %theta = fdiv double 1.000000e+00, %4
  %aux = call %Qubit* @__quantum__rt__qubit_allocate()
  %7 = fneg double %theta
  %p1 = fmul double %7, %time
  %8 = call double @Microsoft__Quantum__Math__PI__body()
  %p2 = fmul double %8, %time
  %datum = call %Result* @Demo__Iteration__body(double %p1, double %p2, %Qubit* %target, %Qubit* %aux)
  %9 = call %Result* @__quantum__rt__result_get_zero()
  %10 = call i1 @__quantum__rt__result_equal(%Result* %datum, %Result* %9)
  br i1 %10, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__1
  %11 = fmul double %4, 6.065000e-01
  %12 = fsub double %2, %11
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__1
  %13 = fmul double %4, 6.065000e-01
  %14 = fadd double %2, %13
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %15 = phi double [ %12, %condTrue__1 ], [ %14, %condFalse__1 ]
  store double %15, double* %mu, align 8
  %16 = fmul double %4, 7.951000e-01
  store double %16, double* %sigma, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %datum, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %aux)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__1
  %17 = add i64 %0, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %18 = load double, double* %mu, align 8
  call void @__quantum__rt__qubit_release(%Qubit* %target)
  ret double %18
}

declare %Qubit* @__quantum__rt__qubit_allocate()

declare %Array* @__quantum__rt__qubit_allocate_array(i64)

define void @Demo__Preparation__body(%Qubit* %q1) {
entry:
  call void @__quantum__qis__h__body(%Qubit* %q1)
  ret void
}

define double @Microsoft__Quantum__Math__PI__body() {
entry:
  ret double 0x400921FB54442D18
}

define %Result* @Demo__Iteration__body(double %p1, double %p2, %Qubit* %q1, %Qubit* %q2) {
entry:
  call void @__quantum__qis__rz__body(double %p1, %Qubit* %q2)
  call void @__quantum__qis__crz__body(double %p2, %Qubit* %q2, %Qubit* %q2)
  call void @__quantum__qis__h__body(%Qubit* %q2)
  %0 = call %Result* @__quantum__qis__m__body(%Qubit* %q2)
  ret %Result* %0
}

declare %Result* @__quantum__rt__result_get_zero()

declare i1 @__quantum__rt__result_equal(%Result*, %Result*)

declare void @__quantum__rt__result_update_reference_count(%Result*, i32)

declare void @__quantum__rt__qubit_release(%Qubit*)

define void @Demo__CRz__body(double %p1, %Qubit* %q1, %Qubit* %q2) {
entry:
  call void @__quantum__qis__crz__body(double %p1, %Qubit* %q1, %Qubit* %q2)
  ret void
}

declare void @__quantum__qis__crz__body(double, %Qubit*, %Qubit*)

declare void @__quantum__qis__rz__body(double, %Qubit*)

declare void @__quantum__qis__h__body(%Qubit*)

declare %Result* @__quantum__qis__m__body(%Qubit*)

define void @Demo__Rz__body(double %p1, %Qubit* %q1) {
entry:
  call void @__quantum__qis__rz__body(double %p1, %Qubit* %q1)
  ret void
}

define %Result* @Demo__M__body(%Qubit* %q1) {
entry:
  %0 = call %Result* @__quantum__qis__m__body(%Qubit* %q1)
  ret %Result* %0
}

define void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %qubit) {
entry:
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__H__adj(%Qubit* %qubit) {
entry:
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__H__ctl(%Array* %__controlQubits__, %Qubit* %qubit) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 1)
  call void @__quantum__qis__h__ctl(%Array* %__controlQubits__, %Qubit* %qubit)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 -1)
  ret void
}

declare void @__quantum__rt__array_update_alias_count(%Array*, i32)

declare void @__quantum__qis__h__ctl(%Array*, %Qubit*)

define void @Microsoft__Quantum__Intrinsic__H__ctladj(%Array* %__controlQubits__, %Qubit* %qubit) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 1)
  call void @__quantum__qis__h__ctl(%Array* %__controlQubits__, %Qubit* %qubit)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 -1)
  ret void
}

attributes #0 = { "EntryPoint" }
