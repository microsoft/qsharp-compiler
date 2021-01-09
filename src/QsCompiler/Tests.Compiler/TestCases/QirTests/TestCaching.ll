define i64 @Microsoft__Quantum__Testing__QIR__TestCaching__body(%Array* %arr) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %arr)
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %q)
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = load %Result*, %Result** @ResultZero
  %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
  br i1 %2, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %3 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__result_unreference(%Result* %0)
  call void @__quantum__rt__array_remove_access(%Array* %arr)
  ret i64 %3

continue__1:                                      ; preds = %entry
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__result_unreference(%Result* %0)
  %4 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  call void @__quantum__rt__array_remove_access(%Array* %arr)
  ret i64 %4
}
