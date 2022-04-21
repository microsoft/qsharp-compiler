define internal { i64, { i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType1__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %udt1 = call { i64 }* @Microsoft__Quantum__Testing__QIR______GUID____Build__body(%Callable* %0)
  %1 = bitcast { i64 }* %udt1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 1)
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2 }* getelementptr ({ %Callable*, i2 }, { %Callable*, i2 }* null, i32 1) to i64))
  %3 = bitcast %Tuple* %2 to { %Callable*, i2 }*
  %4 = getelementptr inbounds { %Callable*, i2 }, { %Callable*, i2 }* %3, i32 0, i32 0
  %5 = getelementptr inbounds { %Callable*, i2 }, { %Callable*, i2 }* %3, i32 0, i32 1
  %6 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType2__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %7 = load i2, i2* @PauliX, align 1
  store %Callable* %6, %Callable** %4, align 8
  store i2 %7, i2* %5, align 1
  %8 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %2)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR______GUID____Build__body(%Callable* %8)
  %9 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i32 1)
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %11 = bitcast %Tuple* %10 to { %Callable*, i2, double }*
  %12 = getelementptr inbounds { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i32 0, i32 0
  %13 = getelementptr inbounds { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i32 0, i32 1
  %14 = getelementptr inbounds { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i32 0, i32 2
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %16 = load i2, i2* @PauliX, align 1
  store %Callable* %15, %Callable** %12, align 8
  store i2 %16, i2* %13, align 1
  store double 2.000000e+00, double* %14, align 8
  %17 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__2__FunctionTable, %Tuple* %10)
  %udt3 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR______GUID____Build__body(%Callable* %17)
  %18 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i32 0, i32 0
  %19 = load { i2, i64 }*, { i2, i64 }** %18, align 8
  %20 = bitcast { i2, i64 }* %19 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %20, i32 1)
  %21 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %21, i32 1)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i2, i64 }* }* getelementptr ({ i64, { i2, i64 }* }, { i64, { i2, i64 }* }* null, i32 1) to i64))
  %23 = bitcast %Tuple* %22 to { i64, { i2, i64 }* }*
  %24 = getelementptr inbounds { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %23, i32 0, i32 0
  %25 = getelementptr inbounds { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %23, i32 0, i32 1
  %26 = getelementptr inbounds { i64 }, { i64 }* %udt1, i32 0, i32 0
  %27 = load i64, i64* %26, align 4
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i32 1)
  store i64 %27, i64* %24, align 4
  store { i2, i64 }* %udt2, { i2, i64 }** %25, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %20, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %21, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %8, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %20, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %21, i32 -1)
  ret { i64, { i2, i64 }* }* %23
}
