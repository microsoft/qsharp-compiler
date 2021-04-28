define i64 @Microsoft__Quantum__Testing__QIR__TestCaching__body(%Array* %arr) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %q)
  %res = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %1 = bitcast %Tuple* %0 to { double, double }*
  %2 = getelementptr inbounds { double, double }, { double, double }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { double, double }, { double, double }* %1, i32 0, i32 1
  store double 1.000000e+00, double* %2, align 8
  store double 2.000000e+00, double* %3, align 8
  %4 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Conditional__body(%Result* %res, { double, double }* %1)
  %5 = call %Result* @__quantum__rt__result_get_zero()
  %6 = call i1 @__quantum__rt__result_equal(%Result* %res, %Result* %5)
  br i1 %6, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %7 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %res, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  %8 = bitcast { double, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret i64 %7

continue__1:                                      ; preds = %entry
  call void @__quantum__rt__result_update_reference_count(%Result* %res, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  %9 = bitcast { double, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  %10 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %11 = icmp slt i64 %10, 10
  br i1 %11, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %continue__1
  %12 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__1

condFalse__1:                                     ; preds = %continue__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %exit__1
  %pad = phi %Array* [ %12, %exit__1 ], [ %arr, %condFalse__1 ]
  call void @__quantum__rt__array_update_alias_count(%Array* %pad, i32 1)
  %sliced = call %Array* @Microsoft__Quantum__Testing__QIR__LengthCaching__body(%Array* %pad)
  call void @__quantum__rt__array_update_alias_count(%Array* %sliced, i32 1)
  %13 = call i64 @__quantum__rt__array_get_size_1d(%Array* %pad)
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %pad, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %sliced, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %pad, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %sliced, i32 -1)
  ret i64 %13

header__1:                                        ; preds = %exiting__1, %condTrue__1
  %14 = phi i64 [ 0, %condTrue__1 ], [ %18, %exiting__1 ]
  %15 = icmp sle i64 %14, 9
  br i1 %15, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 %14)
  %17 = bitcast i8* %16 to i64*
  store i64 0, i64* %17, align 4
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %18 = add i64 %14, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  br label %condContinue__1
}
