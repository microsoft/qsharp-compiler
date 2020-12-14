define { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestUdtConstructor__body() {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i2, i64 }* getelementptr ({ %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, i2, i64 }*
  %2 = load i2, i2* @PauliX
  %3 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %1, i64 0, i32 1
  store i2 %2, i2* %3
  %4 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %1, i64 0, i32 2
  store i64 1, i64* %4
  %5 = call { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ %TupleHeader, i2, i64 }* %1, double 2.000000e+00)
  %6 = bitcast { %TupleHeader, i2, i64 }* %1 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %6)
  ret { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %5
}
