define i64 @Microsoft__Quantum__Testing__QIR__TestRepeat__body(%Qubit* %q) {
entry:
  %n = alloca i64
  store i64 0, i64* %n
  br label %repeat__1

repeat__1:                                        ; preds = %continue__1, %entry
  call void @__quantum__qis__t__body(%Qubit* %q)
  call void @__quantum__qis__x__body(%Qubit* %q)
  call void @__quantum__qis__t__adj(%Qubit* %q)
  call void @__quantum__qis__h__body(%Qubit* %q)
  br label %until__1

until__1:                                         ; preds = %repeat__1
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = load %Result*, %Result** @ResultZero
  %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
  br i1 %2, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %3 = load i64, i64* %n
  %4 = add i64 %3, 1
  store i64 %4, i64* %n
  %5 = load i64, i64* %n
  %6 = icmp sgt i64 %5, 100
  br i1 %6, label %then0__1, label %continue__1

then0__1:                                         ; preds = %fixup__1
  %7 = call %String* @__quantum__rt__string_create(i32 19, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__result_unreference(%Result* %0)
  call void @__quantum__rt__fail(%String* %7)
  unreachable

continue__1:                                      ; preds = %fixup__1
  call void @__quantum__rt__result_unreference(%Result* %0)
  br label %repeat__1

rend__1:                                          ; preds = %until__1
  call void @__quantum__rt__result_unreference(%Result* %0)
  %8 = load i64, i64* %n
  ret i64 %8
}
