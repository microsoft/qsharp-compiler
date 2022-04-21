define i1 @Microsoft__Quantum__Testing__QIR__Foo__body(i64 %c1, i1 %c2) {
entry:
  %0 = icmp sgt i64 %c1, 0
  %1 = and i1 %0, %c2
  %2 = call i1 @Microsoft__Quantum__Testing__QIR______GUID____Bar__body(i1 %1)
  ret i1 %2
}

define internal i1 @Microsoft__Quantum__Testing__QIR______GUID____Bar__body(i1 %a1) {
entry:
  ret i1 %a1
}
