define i64 @Microsoft__Quantum__Testing__QIR__TestInts__body(i64 %a, i64 %b) {
entry:
  %0 = icmp sgt i64 %a, %b
  %c = select i1 %0, i64 %a, i64 %b
  %1 = mul i64 %c, %a
  %2 = udiv i64 %b, 7
  %d = sub i64 %1, %2
  %e = ashr i64 %d, 3
  %3 = trunc i64 %b to i32
  %f = call i64 @__quantum__rt__int_power(i64 %d, i32 %3)
  %4 = and i64 %e, %f
  %g = or i64 %4, 65535
  %5 = xor i64 %g, -1
  ret i64 %5
}
