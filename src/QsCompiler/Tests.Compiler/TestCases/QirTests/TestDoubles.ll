define double @Microsoft__Quantum__Testing__QIR__TestDouble__body(double %x, double %y) {
entry:
  %0 = fadd double %x, %y
  %a = fsub double %0, 2.000000e+00
  %1 = fmul double %a, 1.235000e+00
  %2 = call double @llvm.pow.f64(double %x, double %y)
  %b = fadd double %1, %2
  %3 = fcmp oge double %a, %b
  br i1 %3, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %4 = fsub double %a, %b
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %5 = fadd double %a, %b
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %c = phi double [ %4, %condTrue__1 ], [ %5, %condFalse__1 ]
  %6 = fmul double %a, %b
  %7 = fmul double %6, %c
  ret double %7
}
