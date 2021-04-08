define i64 @Microsoft__Quantum__Testing__QIR__TestUnreachable2__body(i64 %a, i64 %b) {
entry:
  %c = add i64 %a, %b
  ret i64 %c

entry__unreachable__1:                            ; No predecessors!
  %0 = icmp eq i64 %c, 5
  br i1 %0, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry__unreachable__1
  ret i64 %a

else__1:                                          ; preds = %entry__unreachable__1
  ret i64 %b
}
