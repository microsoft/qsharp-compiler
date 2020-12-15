define i64 @Microsoft__Quantum__Testing__QIR__TestAccessors__body() {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i2, i64 }* getelementptr ({ %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, i2, i64 }*
  %2 = load i2, i2* @PauliX
  %3 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %1, i64 0, i32 1
  store i2 %2, i2* %3
  %4 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %1, i64 0, i32 2
  store i64 1, i64* %4
  %x = call { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ %TupleHeader, i2, i64 }* %1, double 2.000000e+00)
  %5 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %x, i64 0, i32 1
  %6 = load { %TupleHeader, i2, i64 }*, { %TupleHeader, i2, i64 }** %5
  %7 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %6, i64 0, i32 2
  %y = load i64, i64* %7
  %8 = bitcast { %TupleHeader, i2, i64 }* %1 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %8)
  %9 = bitcast { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %x to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %9)
  %10 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %x, i64 0, i32 1
  %11 = load { %TupleHeader, i2, i64 }*, { %TupleHeader, i2, i64 }** %10
  %12 = bitcast { %TupleHeader, i2, i64 }* %11 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %12)
  ret i64 %y
}

define { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ %TupleHeader, i2, i64 }* %arg__1, double %D) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, { %TupleHeader, i2, i64 }*, double }* getelementptr ({ %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, { %TupleHeader, i2, i64 }*, double }*
  %2 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1, i64 0, i32 1
  store { %TupleHeader, i2, i64 }* %arg__1, { %TupleHeader, i2, i64 }** %2
  %3 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1, i64 0, i32 2
  store double %D, double* %3
  ret { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1
}
