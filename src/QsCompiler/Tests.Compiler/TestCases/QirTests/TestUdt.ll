define { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %arg__1, double %D) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ { i2, i64 }*, double }* getelementptr ({ { i2, i64 }*, double }, { { i2, i64 }*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { { i2, i64 }*, double }*
  %2 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %1, i64 0, i32 0
  store { i2, i64 }* %arg__1, { i2, i64 }** %2
  %3 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %1, i64 0, i32 1
  store double %D, double* %3
  ret { { i2, i64 }*, double }* %1
}
