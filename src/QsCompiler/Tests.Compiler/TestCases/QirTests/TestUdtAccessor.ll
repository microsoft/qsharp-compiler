define i64 @Microsoft__Quantum__Testing__QIR__TestAccessors__body() {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i2, i64 }* getelementptr ({ i2, i64 }, { i2, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { i2, i64 }*
  %2 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %1, i32 0, i32 1
  %4 = load i2, i2* @PauliX
  store i2 %4, i2* %2
  store i64 1, i64* %3
  %x = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %1, double 2.000000e+00)
  %5 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %x, i32 0, i32 0
  %6 = load { i2, i64 }*, { i2, i64 }** %5
  %7 = bitcast { i2, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i64 1)
  %8 = bitcast { { i2, i64 }*, double }* %x to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i64 1)
  %9 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %6, i32 0, i32 1
  %y = load i64, i64* %9
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  ret i64 %y
}

define { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %0, double %D) {
entry:
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ { i2, i64 }*, double }* getelementptr ({ { i2, i64 }*, double }, { { i2, i64 }*, double }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { { i2, i64 }*, double }*
  %3 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %2, i32 0, i32 1
  store { i2, i64 }* %0, { i2, i64 }** %3
  store double %D, double* %4
  %5 = bitcast { i2, i64 }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  ret { { i2, i64 }*, double }* %2
}
