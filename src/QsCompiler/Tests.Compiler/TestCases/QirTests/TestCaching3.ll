define { double, double }* @Microsoft__Quantum__Testing__QIR__Conditional__body(%Result* %res, { double, double }* %0) {
entry:
  %1 = getelementptr inbounds { double, double }, { double, double }* %0, i32 0, i32 0
  %x = load double, double* %1, align 8
  %2 = getelementptr inbounds { double, double }, { double, double }* %0, i32 0, i32 1
  %y = load double, double* %2, align 8
  %3 = call %Result* @__quantum__rt__result_get_zero()
  %4 = call i1 @__quantum__rt__result_equal(%Result* %res, %Result* %3)
  br i1 %4, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %6 = bitcast %Tuple* %5 to { double, double }*
  %7 = getelementptr inbounds { double, double }, { double, double }* %6, i32 0, i32 0
  %8 = getelementptr inbounds { double, double }, { double, double }* %6, i32 0, i32 1
  %9 = fsub double %x, 5.000000e-01
  store double %9, double* %7, align 8
  store double %y, double* %8, align 8
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %11 = bitcast %Tuple* %10 to { double, double }*
  %12 = getelementptr inbounds { double, double }, { double, double }* %11, i32 0, i32 0
  %13 = getelementptr inbounds { double, double }, { double, double }* %11, i32 0, i32 1
  %14 = fadd double %y, 5.000000e-01
  store double %x, double* %12, align 8
  store double %14, double* %13, align 8
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %15 = phi { double, double }* [ %6, %condTrue__1 ], [ %11, %condFalse__1 ]
  ret { double, double }* %15
}
