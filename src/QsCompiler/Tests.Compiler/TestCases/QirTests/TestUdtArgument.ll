define internal { i64, { i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType1__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %udt1 = call { i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = bitcast { i64 }* %udt1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 1)
  %2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType2__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %2, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %2, i32 1)
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2 }* getelementptr ({ %Callable*, i2 }, { %Callable*, i2 }* null, i32 1) to i64))
  %4 = bitcast %Tuple* %3 to { %Callable*, i2 }*
  %5 = getelementptr inbounds { %Callable*, i2 }, { %Callable*, i2 }* %4, i32 0, i32 0
  %6 = getelementptr inbounds { %Callable*, i2 }, { %Callable*, i2 }* %4, i32 0, i32 1
  store %Callable* %2, %Callable** %5, align 8
  store i2 1, i2* %6, align 1
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %3)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %7)
  %8 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i32 1)
  %9 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %9, i32 1)
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %11 = bitcast %Tuple* %10 to { %Callable*, i2, double }*
  %12 = getelementptr inbounds { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i32 0, i32 0
  %13 = getelementptr inbounds { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i32 0, i32 1
  %14 = getelementptr inbounds { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i32 0, i32 2
  store %Callable* %9, %Callable** %12, align 8
  store i2 1, i2* %13, align 1
  store double 2.000000e+00, double* %14, align 8
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__2__FunctionTable, %Tuple* %10)
  %udt3 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %15)
  %16 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i32 0, i32 0
  %17 = load { i2, i64 }*, { i2, i64 }** %16, align 8
  %18 = bitcast { i2, i64 }* %17 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i32 1)
  %19 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i32 1)
  %20 = getelementptr inbounds { i64 }, { i64 }* %udt1, i32 0, i32 0
  %21 = load i64, i64* %20, align 4
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 1)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i2, i64 }* }* getelementptr ({ i64, { i2, i64 }* }, { i64, { i2, i64 }* }* null, i32 1) to i64))
  %23 = bitcast %Tuple* %22 to { i64, { i2, i64 }* }*
  %24 = getelementptr inbounds { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %23, i32 0, i32 0
  %25 = getelementptr inbounds { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %23, i32 0, i32 1
  store i64 %21, i64* %24, align 4
  store { i2, i64 }* %udt2, { i2, i64 }** %25, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %2, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %7, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %7, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %15, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %15, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i32 -1)
  ret { i64, { i2, i64 }* }* %23
}
