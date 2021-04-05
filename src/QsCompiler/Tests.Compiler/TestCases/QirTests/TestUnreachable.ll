define i64 @Microsoft__Quantum__Testing__QIR__TestUnreachable__body(i64 %a, i64 %b) {
entry:
  %c = add i64 %a, %b
  %0 = icmp eq i64 %c, 5
  br i1 %0, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  ret i64 %c

then0__1__unreachable__1:                         ; No predecessors!
  %d = add i64 2, %a
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([14 x i8], [14 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__fail(%String* %1)
  unreachable

then0__1__unreachable__2:                         ; No predecessors!
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret i64 %b

then0__1__unreachable__3:                         ; No predecessors!
  %e = add i64 3, %b
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret i64 %e

then0__1__unreachable__4:                         ; No predecessors!
  %f = add i64 %c, %e
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  br label %continue__1

else__1:                                          ; preds = %entry
  ret i64 %b

else__1__unreachable__1:                          ; No predecessors!
  ret i64 %c

continue__1:                                      ; preds = %then0__1__unreachable__4
  %f__1 = add i64 %c, %b
  ret i64 %f__1

continue__1__unreachable__1:                      ; No predecessors!
  ret i64 %c
}
