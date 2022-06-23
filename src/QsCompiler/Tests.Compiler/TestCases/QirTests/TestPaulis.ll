define internal i2 @Microsoft__Quantum__Testing__QIR__TestPaulis__body(i2 %a, i2 %b) {
entry:
  %0 = icmp eq i2 %a, 1
  br i1 %0, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  ret i2 -1

continue__1:                                      ; preds = %entry
  ret i2 %b
}
