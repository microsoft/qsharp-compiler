define i64 @Microsoft__Quantum__Testing__QIR__TestCaching__body(%Array* %arr) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i64 1)
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %q)
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = load %Result*, %Result** @ResultZero
  %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
  br i1 %2, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %3 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i64 -1)
  ret i64 %3

continue__1:                                      ; preds = %entry
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i64 -1)
  %4 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %5 = icmp slt i64 %4, 10
  br i1 %5, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %continue__1
  %6 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__1

condFalse__1:                                     ; preds = %continue__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i64 1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %exit__1
  %pad = phi %Array* [ %6, %condTrue__1 ], [ %arr, %condFalse__1 ]
  call void @__quantum__rt__array_update_alias_count(%Array* %pad, i64 1)
  %7 = call i64 @__quantum__rt__array_get_size_1d(%Array* %pad)
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %pad, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %pad, i64 -1)
  ret i64 %7

header__1:                                        ; preds = %exiting__1, %condTrue__1
  %8 = phi i64 [ 0, %condTrue__1 ], [ %12, %exiting__1 ]
  %9 = icmp sle i64 %8, 9
  br i1 %9, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 %8)
  %11 = bitcast i8* %10 to i64*
  store i64 0, i64* %11
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %12 = add i64 %8, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  br label %condContinue__1
}
