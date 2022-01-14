define internal i64 @Microsoft__Quantum__Testing__QIR__TestDeepNesting__body(%Result* %r1, %Result* %r2, %Result* %r3) {
entry:
  %0 = call i1 @__quantum__rt__result_equal(%Result* %r1, %Result* %r2)
  br i1 %0, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %1 = call i1 @__quantum__rt__result_equal(%Result* %r2, %Result* %r3)
  br i1 %1, label %then0__2, label %continue__2

then0__2:                                         ; preds = %then0__1
  %2 = call i1 @__quantum__rt__result_equal(%Result* %r3, %Result* %r1)
  br i1 %2, label %then0__3, label %continue__3

then0__3:                                         ; preds = %then0__2
  %3 = call %Result* @__quantum__rt__result_get_one()
  %4 = call i1 @__quantum__rt__result_equal(%Result* %r1, %Result* %3)
  br i1 %4, label %then0__4, label %continue__4

then0__4:                                         ; preds = %then0__3
  %5 = call %Result* @__quantum__rt__result_get_one()
  %6 = call i1 @__quantum__rt__result_equal(%Result* %r2, %Result* %5)
  br i1 %6, label %then0__5, label %continue__5

then0__5:                                         ; preds = %then0__4
  %7 = call %Result* @__quantum__rt__result_get_one()
  %8 = call i1 @__quantum__rt__result_equal(%Result* %r3, %Result* %7)
  br i1 %8, label %then0__6, label %continue__6

then0__6:                                         ; preds = %then0__5
  %9 = call %Result* @__quantum__rt__result_get_zero()
  %10 = call i1 @__quantum__rt__result_equal(%Result* %r3, %Result* %9)
  %11 = xor i1 %10, true
  br i1 %11, label %then0__7, label %continue__7

then0__7:                                         ; preds = %then0__6
  %12 = call %Result* @__quantum__rt__result_get_zero()
  %13 = call i1 @__quantum__rt__result_equal(%Result* %r2, %Result* %12)
  %14 = xor i1 %13, true
  br i1 %14, label %then0__8, label %continue__8

then0__8:                                         ; preds = %then0__7
  ret i64 1

continue__8:                                      ; preds = %then0__7
  br label %continue__7

continue__7:                                      ; preds = %continue__8, %then0__6
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__5
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__4
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__3
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__2
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__1
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %entry
  ret i64 0
}
