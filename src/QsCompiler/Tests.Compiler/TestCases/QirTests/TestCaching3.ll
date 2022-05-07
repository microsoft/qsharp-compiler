define internal { double, double }* @Microsoft__Quantum__Testing__QIR__Conditional__body(%Result* %res, { double, double }* %0) {
entry:
  %1 = getelementptr inbounds { double, double }, { double, double }* %0, i32 0, i32 0
  %x = load double, double* %1, align 8
  %2 = getelementptr inbounds { double, double }, { double, double }* %0, i32 0, i32 1
  %y = load double, double* %2, align 8
  %3 = call %Result* @__quantum__rt__result_get_zero()
  %4 = call i1 @__quantum__rt__result_equal(%Result* %res, %Result* %3)
  br i1 %4, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %5 = fsub double %x, 5.000000e-01
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, double }* getelementptr ({ double, double }, { double, double }* null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { double, double }*
  %8 = getelementptr inbounds { double, double }, { double, double }* %7, i32 0, i32 0
  %9 = getelementptr inbounds { double, double }, { double, double }* %7, i32 0, i32 1
  store double %5, double* %8, align 8
  store double %y, double* %9, align 8
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %10 = fadd double %y, 5.000000e-01
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, double }* getelementptr ({ double, double }, { double, double }* null, i32 1) to i64))
  %12 = bitcast %Tuple* %11 to { double, double }*
  %13 = getelementptr inbounds { double, double }, { double, double }* %12, i32 0, i32 0
  %14 = getelementptr inbounds { double, double }, { double, double }* %12, i32 0, i32 1
  store double %x, double* %13, align 8
  store double %10, double* %14, align 8
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %15 = phi { double, double }* [ %7, %condTrue__1 ], [ %12, %condFalse__1 ]
  ret { double, double }* %15
}
