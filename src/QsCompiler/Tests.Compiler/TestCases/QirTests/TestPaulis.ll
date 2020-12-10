define i2 @Microsoft__Quantum__Testing__QIR__TestPaulis__body(i2 %a, i2 %b) {
entry:
  %0 = load i2, i2* @PauliX
  %1 = icmp eq i2 %a, %0
  br i1 %1, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %2 = load i2, i2* @PauliY
  ret i2 %2

continue__1:                                      ; preds = %entry
  ret i2 %b
}
