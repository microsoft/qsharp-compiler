define { double, double }* @Microsoft__Quantum__Testing__QIR__TestInline__body() #0 {
entry:
  %x = alloca double
  store double 0.000000e+00, double* %x
  %y = alloca double
  store double 0.000000e+00, double* %y
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__k__body(double 3.000000e-01, %Qubit* %q)
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %2 = bitcast %Tuple* %1 to { double, double }*
  %3 = getelementptr { double, double }, { double, double }* %2, i64 0, i32 0
  %4 = getelementptr { double, double }, { double, double }* %2, i64 0, i32 1
  store double 0.000000e+00, double* %3
  store double 0.000000e+00, double* %4
  %5 = call { double, double }* @Microsoft__Quantum__Testing__QIR__UpdatedValues__body(%Result* %0, { double, double }* %2)
  %6 = getelementptr { double, double }, { double, double }* %5, i64 0, i32 0
  %7 = load double, double* %6
  store double %7, double* %x
  %8 = getelementptr { double, double }, { double, double }* %5, i64 0, i32 1
  %9 = load double, double* %8
  store double %9, double* %y
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__result_unreference(%Result* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  %10 = bitcast { double, double }* %5 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %10)
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %12 = bitcast %Tuple* %11 to { double, double }*
  %13 = getelementptr { double, double }, { double, double }* %12, i64 0, i32 0
  %14 = getelementptr { double, double }, { double, double }* %12, i64 0, i32 1
  store double %7, double* %13
  store double %9, double* %14
  ret { double, double }* %12
}
