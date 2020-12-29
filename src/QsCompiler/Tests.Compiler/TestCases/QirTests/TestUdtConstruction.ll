define { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestUdtConstructor__body() {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %args = bitcast %Tuple* %0 to { double, double }*
  %1 = getelementptr { double, double }, { double, double }* %args, i64 0, i32 0
  %2 = getelementptr { double, double }, { double, double }* %args, i64 0, i32 1
  store double 1.000000e+00, double* %1
  store double 2.000000e+00, double* %2
  %3 = getelementptr { double, double }, { double, double }* %args, i64 0, i32 0
  %4 = getelementptr { double, double }, { double, double }* %args, i64 0, i32 1
  %5 = load double, double* %3
  %6 = load double, double* %4
  %complex = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double %5, double %6)
  %7 = load i2, i2* @PauliX
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i2, i64 }* getelementptr ({ i2, i64 }, { i2, i64 }* null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { i2, i64 }*
  %10 = getelementptr { i2, i64 }, { i2, i64 }* %9, i64 0, i32 0
  %11 = getelementptr { i2, i64 }, { i2, i64 }* %9, i64 0, i32 1
  store i2 %7, i2* %10
  store i64 1, i64* %11
  %12 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %9, double 2.000000e+00)
  %13 = bitcast { double, double }* %args to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %13)
  %14 = bitcast { double, double }* %complex to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %14)
  %15 = bitcast { i2, i64 }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %15)
  ret { { i2, i64 }*, double }* %12
}
