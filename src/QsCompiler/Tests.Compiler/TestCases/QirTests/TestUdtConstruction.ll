define { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ %TupleHeader, i2, i64 }* %arg0, double %arg1) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, { %TupleHeader, i2, i64 }*, double }* getelementptr ({ %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, { %TupleHeader, i2, i64 }*, double }*
  %2 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1, i64 0, i32 1
  store { %TupleHeader, i2, i64 }* %arg0, { %TupleHeader, i2, i64 }** %2
  %3 = bitcast { %TupleHeader, i2, i64 }* %arg0 to %TupleHeader*
  call void @__quantum__rt__tuple_reference(%TupleHeader* %3)
  %4 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1, i64 0, i32 2
  store double %arg1, double* %4
  ret { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1
}
