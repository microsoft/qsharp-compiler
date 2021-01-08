define i64 @Microsoft__Quantum__Testing__QIR__TestAccessors__body() {
entry:
  %0 = load i2, i2* @PauliX
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i2, i64 }* getelementptr ({ i2, i64 }, { i2, i64 }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { i2, i64 }*
  %3 = getelementptr { i2, i64 }, { i2, i64 }* %2, i64 0, i32 0
  %4 = getelementptr { i2, i64 }, { i2, i64 }* %2, i64 0, i32 1
  store i2 %0, i2* %3
  store i64 1, i64* %4
  %x = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %2, double 2.000000e+00)
  %5 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %x, i64 0, i32 0
  %6 = load { i2, i64 }*, { i2, i64 }** %5
  %7 = bitcast { i2, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %7)
  %8 = bitcast { { i2, i64 }*, double }* %x to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %8)
  %9 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %x, i64 0, i32 0
  %10 = load { i2, i64 }*, { i2, i64 }** %9
  %11 = getelementptr { i2, i64 }, { i2, i64 }* %10, i64 0, i32 1
  %y = load i64, i64* %11
  %12 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %x, i64 0, i32 0
  %13 = load { i2, i64 }*, { i2, i64 }** %12
  %14 = bitcast { i2, i64 }* %13 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %14)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  %15 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %x, i64 0, i32 0
  %16 = load { i2, i64 }*, { i2, i64 }** %15
  %17 = bitcast { i2, i64 }* %16 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %17)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  ret i64 %y
}

define { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %arg__1, double %D) {
entry:
  %0 = bitcast { i2, i64 }* %arg__1 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %0)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ { i2, i64 }*, double }* getelementptr ({ { i2, i64 }*, double }, { { i2, i64 }*, double }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { { i2, i64 }*, double }*
  %3 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %2, i64 0, i32 0
  %4 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %2, i64 0, i32 1
  store { i2, i64 }* %arg__1, { i2, i64 }** %3
  %5 = bitcast { i2, i64 }* %arg__1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %5)
  store double %D, double* %4
  call void @__quantum__rt__tuple_remove_access(%Tuple* %0)
  ret { { i2, i64 }*, double }* %2
}