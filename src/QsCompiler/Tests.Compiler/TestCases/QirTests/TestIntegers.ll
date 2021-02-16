define i64 @Microsoft__Quantum__Testing__QIR__TestInts__body(i64 %a, i64 %b) {
entry:
  %0 = icmp sgt i64 %a, %b
  %c = select i1 %0, i64 %a, i64 %b
  %1 = mul i64 %c, %a
  %2 = udiv i64 %b, 7
  %d = sub i64 %1, %2
  %e = ashr i64 %d, 3
  %3 = sitofp i64 %d to double
  %4 = trunc i64 %b to i32
  %5 = call double @llvm.powi.f64(double %3, i32 %4)
  %f = fptosi double %5 to i64
  %6 = and i64 %e, %f
  %g = or i64 %6, 65535
  %7 = xor i64 %g, -1
  ret i64 %7
}
