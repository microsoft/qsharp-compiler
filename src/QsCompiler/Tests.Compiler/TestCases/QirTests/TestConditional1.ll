﻿define i64 @Microsoft__Quantum__Testing__QIR__ReturnInt__body({ %Array* }* %arg) {
entry:
  %0 = getelementptr inbounds { %Array* }, { %Array* }* %arg, i32 0, i32 0
  %arg__1 = load %Array*, %Array** %0, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i32 1)
  %1 = bitcast { %Array* }* %arg to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i32 1)
  %2 = call i64 @__quantum__qis__drawrandom__body(%Array* %arg__1)
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i32 -1)
  %3 = icmp slt i64 %2, 0
  br i1 %3, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 -1)
  ret i64 1

else__1:                                          ; preds = %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 -1)
  ret i64 0

continue__1:                                      ; No predecessors!
  %4 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @6, i32 0, i32 0))
  call void @__quantum__rt__array_update_alias_count(%Array* %arg__1, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__fail(%String* %4)
  unreachable
}
