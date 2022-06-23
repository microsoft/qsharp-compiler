define internal i64 @Microsoft__Quantum__Testing__QIR__TestConditions__body(%String* %input, %Array* %arr) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @4, i32 0, i32 0))
  %1 = call i1 @__quantum__rt__string_equal(%String* %input, %String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  br i1 %1, label %then0__1, label %test1__1

then0__1:                                         ; preds = %entry
  br label %continue__1

test1__1:                                         ; preds = %entry
  %2 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @5, i32 0, i32 0))
  %3 = call i1 @__quantum__rt__string_equal(%String* %input, %String* %2)
  call void @__quantum__rt__string_update_reference_count(%String* %2, i32 -1)
  br i1 %3, label %then1__1, label %test2__1

then1__1:                                         ; preds = %test1__1
  br label %continue__1

test2__1:                                         ; preds = %test1__1
  %4 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %5 = icmp sgt i64 %4, 0
  br i1 %5, label %then2__1, label %continue__1

then2__1:                                         ; preds = %test2__1
  br label %continue__1

continue__1:                                      ; preds = %then2__1, %test2__1, %then1__1, %then0__1
  %6 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  ret i64 %6
}
