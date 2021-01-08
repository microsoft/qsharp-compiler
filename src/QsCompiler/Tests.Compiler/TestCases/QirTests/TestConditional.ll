define i64 @Microsoft__Quantum__Testing__QIR__ReturnInt__body({ %Array* }* %arg) {
entry:
  %0 = getelementptr { %Array* }, { %Array* }* %arg, i64 0, i32 0
  %1 = load %Array*, %Array** %0
  call void @__quantum__rt__array_add_access(%Array* %1)
  %2 = bitcast { %Array* }* %arg to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %2)
  %3 = getelementptr { %Array* }, { %Array* }* %arg, i64 0, i32 0
  %arg__inline__1 = load %Array*, %Array** %3
  call void @__quantum__rt__array_add_access(%Array* %arg__inline__1)
  %4 = call i64 @__quantum__qis__drawrandom__body(%Array* %arg__inline__1)
  call void @__quantum__rt__array_remove_access(%Array* %arg__inline__1)
  %5 = icmp slt i64 %4, 0
  br i1 %5, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  %6 = getelementptr { %Array* }, { %Array* }* %arg, i64 0, i32 0
  %7 = load %Array*, %Array** %6
  call void @__quantum__rt__array_remove_access(%Array* %7)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %2)
  ret i64 1

else__1:                                          ; preds = %entry
  %8 = getelementptr { %Array* }, { %Array* }* %arg, i64 0, i32 0
  %9 = load %Array*, %Array** %8
  call void @__quantum__rt__array_remove_access(%Array* %9)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %2)
  ret i64 0
}