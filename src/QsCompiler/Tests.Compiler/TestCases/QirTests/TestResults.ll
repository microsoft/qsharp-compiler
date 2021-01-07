define %Result* @Microsoft__Quantum__Testing__QIR__TestResults__body(%Result* %a, %Result* %b) {
entry:
  %0 = call i1 @__quantum__rt__result_equal(%Result* %a, %Result* %b)
  br i1 %0, label %then0__1, label %test1__1

then0__1:                                         ; preds = %entry
  %1 = load %Result*, %Result** @ResultOne
  call void @__quantum__rt__result_reference(%Result* %1)
  ret %Result* %1

test1__1:                                         ; preds = %entry
  %2 = load %Result*, %Result** @ResultOne
  %3 = call i1 @__quantum__rt__result_equal(%Result* %a, %Result* %2)
  br i1 %3, label %then1__1, label %continue__1

then1__1:                                         ; preds = %test1__1
  call void @__quantum__rt__result_reference(%Result* %b)
  ret %Result* %b

continue__1:                                      ; preds = %test1__1
  %4 = load %Result*, %Result** @ResultZero
  call void @__quantum__rt__result_reference(%Result* %4)
  ret %Result* %4
}
