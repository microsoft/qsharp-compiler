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

continue__1:                                      ; No predecessors!
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__fail(%String* %1)
  unreachable
}
