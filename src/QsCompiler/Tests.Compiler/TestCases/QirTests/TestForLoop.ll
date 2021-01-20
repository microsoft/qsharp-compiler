define double @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
entry:
  %energy = alloca double
  store double 0.000000e+00, double* %energy
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %1, %exiting__1 ]
  %0 = icmp sle i64 %i, 10
  br i1 %0, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %1 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %2 = load double, double* %energy
  ret double %2

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %8, %exiting__2 ]
  %3 = icmp sle i64 %j, 0
  %4 = icmp sge i64 %j, 0
  %5 = select i1 false, i1 %3, i1 %4
  br i1 %5, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %6 = load double, double* %energy
  %7 = fadd double %6, 5.000000e-01
  store double %7, double* %energy
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %8 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
