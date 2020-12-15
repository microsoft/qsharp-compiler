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
