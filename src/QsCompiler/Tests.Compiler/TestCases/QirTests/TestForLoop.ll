define double @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
entry:
  %energy = alloca double
  store double 0.000000e+00, double* %energy
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %3, %exiting__1 ]
  %0 = icmp sge i64 %i, 10
  %1 = icmp sle i64 %i, 10
  %2 = select i1 true, i1 %1, i1 %0
  br i1 %2, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %3 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %4 = load double, double* %energy
  ret double %4

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %10, %exiting__2 ]
  %5 = icmp sge i64 %j, 0
  %6 = icmp sle i64 %j, 0
  %7 = select i1 false, i1 %6, i1 %5
  br i1 %7, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %8 = load double, double* %energy
  %9 = fadd double %8, 5.000000e-01
  store double %9, double* %energy
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %10 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
