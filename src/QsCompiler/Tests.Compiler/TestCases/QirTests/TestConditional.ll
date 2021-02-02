define i64 @Microsoft__Quantum__Testing__QIR__ReturnInt__body({ %Array* }* %arg) {
entry:
  %0 = getelementptr { %Array* }, { %Array* }* %arg, i64 0, i32 0
  %arg__1 = load %Array*, %Array** %0
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i64 1)
  %1 = bitcast { %Array* }* %arg to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i64 1)
  %2 = call i64 @__quantum__qis__drawrandom__body(%Array* %arg__1)
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i64 -1)
  %3 = icmp slt i64 %2, 0
  br i1 %3, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i64 -1)
  ret i64 1

else__1:                                          ; preds = %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i64 -1)
  ret i64 0
}
