define { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestUdtConstructor__body() {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i2, i64 }* getelementptr ({ i2, i64 }, { i2, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { i2, i64 }*
  %2 = load i2, i2* @PauliX
  %3 = getelementptr { i2, i64 }, { i2, i64 }* %1, i64 0, i32 0
  store i2 %2, i2* %3
  %4 = getelementptr { i2, i64 }, { i2, i64 }* %1, i64 0, i32 1
  store i64 1, i64* %4
  %5 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %1, double 2.000000e+00)
  %6 = bitcast { i2, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %6)
  ret { { i2, i64 }*, double }* %5
}
