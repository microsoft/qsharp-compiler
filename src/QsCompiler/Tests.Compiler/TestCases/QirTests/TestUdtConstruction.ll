define internal { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestUdtConstructor__body() {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %args = bitcast %Tuple* %0 to { double, double }*
  %1 = getelementptr inbounds { double, double }, { double, double }* %args, i32 0, i32 0
  %2 = getelementptr inbounds { double, double }, { double, double }* %args, i32 0, i32 1
  store double 1.000000e+00, double* %1, align 8
  store double 2.000000e+00, double* %2, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %0, i32 1)
  %complex = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 1.000000e+00, double 2.000000e+00)
  %3 = bitcast { double, double }* %complex to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 1)
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i2, i64 }* getelementptr ({ i2, i64 }, { i2, i64 }* null, i32 1) to i64))
  %5 = bitcast %Tuple* %4 to { i2, i64 }*
  %6 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %5, i32 0, i32 0
  %7 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %5, i32 0, i32 1
  %8 = load i2, i2* @PauliX, align 1
  store i2 %8, i2* %6, align 1
  store i64 1, i64* %7, align 4
  %9 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %5, double 2.000000e+00)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %4, i32 -1)
  ret { { i2, i64 }*, double }* %9
}
