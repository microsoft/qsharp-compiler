define i64 @Microsoft__Quantum__Testing__QIR__ReturnFromNested__body(i1 %branch1, i1 %branch2) {
entry:
  br i1 %branch1, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  br i1 %branch2, label %then0__2, label %else__2

then0__2:                                         ; preds = %then0__1
  ret i64 1

else__2:                                          ; preds = %then0__1
  ret i64 2

else__1:                                          ; preds = %entry
  br i1 %branch2, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__1
  ret i64 3

else__3:                                          ; preds = %else__1
  ret i64 4
}
