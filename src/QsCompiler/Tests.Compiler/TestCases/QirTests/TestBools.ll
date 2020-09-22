define i1 @Microsoft__Quantum__Testing__QIR__TestBools__body(i1 %a, i1 %b) {
entry:
  %0 = icmp eq i1 %a, %b
  %c = select i1 %0, i1 %a, i1 %b
  %d = and i1 %a, %b
  %e = or i1 %a, %b
  %f = xor i1 %a, true
  br i1 %f, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  ret i1 %d

else__1:                                          ; preds = %entry
  ret i1 %e
}
