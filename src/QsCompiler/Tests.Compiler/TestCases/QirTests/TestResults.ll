define %Result* @Microsoft__Quantum__Testing__QIR__TestResults__body(%Result* %a, %Result* %b) {
entry:
  %0 = call i1 @__quantum__rt__result_equal(%Result* %a, %Result* %b)
  br i1 %0, label %then0__1, label %test1__1

then0__1:                                         ; preds = %entry
  %1 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %1, i32 1)
  ret %Result* %1

test1__1:                                         ; preds = %entry
  %2 = call %Result* @__quantum__rt__result_get_one()
  %3 = call i1 @__quantum__rt__result_equal(%Result* %a, %Result* %2)
  br i1 %3, label %then1__1, label %continue__1

then1__1:                                         ; preds = %test1__1
  call void @__quantum__rt__result_update_reference_count(%Result* %b, i32 1)
  ret %Result* %b

continue__1:                                      ; preds = %test1__1
  %4 = call %Result* @__quantum__rt__result_get_zero()
  call void @__quantum__rt__result_update_reference_count(%Result* %4, i32 1)
  ret %Result* %4
}
