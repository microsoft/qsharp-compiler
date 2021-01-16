define { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %0, double %D) {
entry:
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ { i2, i64 }*, double }* getelementptr ({ { i2, i64 }*, double }, { { i2, i64 }*, double }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { { i2, i64 }*, double }*
  %3 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %2, i64 0, i32 0
  %4 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %2, i64 0, i32 1
  store { i2, i64 }* %0, { i2, i64 }** %3
  %5 = bitcast { i2, i64 }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  store double %D, double* %4
  ret { { i2, i64 }*, double }* %2
}
