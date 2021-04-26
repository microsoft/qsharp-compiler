define i64 @Microsoft__Quantum__Testing__QIR__ReturnFromNested__body(i1 %branch1, i1 %branch2) {
entry:
  br i1 %branch1, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  br i1 %branch2, label %then0__2, label %else__2

then0__2:                                         ; preds = %then0__1
  ret i64 1

else__2:                                          ; preds = %then0__1
  ret i64 2

continue__2:                                      ; No predecessors!
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @3, i32 0, i32 0))
  call void @__quantum__rt__fail(%String* %0)
  unreachable

else__1:                                          ; preds = %entry
  br i1 %branch2, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__1
  ret i64 3

else__3:                                          ; preds = %else__1
  ret i64 4

continue__3:                                      ; No predecessors!
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @4, i32 0, i32 0))
  call void @__quantum__rt__fail(%String* %1)
  unreachable

continue__1:                                      ; No predecessors!
  %2 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @5, i32 0, i32 0))
  call void @__quantum__rt__fail(%String* %2)
  unreachable
}
