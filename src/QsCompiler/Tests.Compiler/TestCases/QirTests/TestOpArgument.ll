define internal %String* @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() {
entry:
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, %Qubit* }* getelementptr ({ %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* null, i32 1) to i64))
  %qs = bitcast %Tuple* %0 to { %Qubit*, %Qubit* }*
  %1 = getelementptr inbounds { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i32 0, i32 0
  %2 = getelementptr inbounds { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i32 0, i32 1
  store %Qubit* %q1, %Qubit** %1, align 8
  store %Qubit* %q2, %Qubit** %2, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %0, i32 1)
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Qubit* }, { %Callable*, %Qubit* }* null, i32 1) to i64))
  %4 = bitcast %Tuple* %3 to { %Callable*, %Qubit* }*
  %5 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %4, i32 0, i32 0
  %6 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %4, i32 0, i32 1
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %7, %Callable** %5, align 8
  store %Qubit* %q1, %Qubit** %6, align 8
  %op = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %3)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op, i32 1)
  call void @__quantum__qis__cnot__body(%Qubit* %q1, %Qubit* %q2)
  call void @__quantum__qis__swap(%Qubit* %q1, %Qubit* %q2)
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Qubit* }, { %Callable*, %Qubit* }* null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { %Callable*, %Qubit* }*
  %10 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i32 0, i32 0
  %11 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i32 0, i32 1
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %12, %Callable** %10, align 8
  store %Qubit* %q1, %Qubit** %11, align 8
  %13 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__2__FunctionTable, %Tuple* %8)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %13)
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Qubit* }, { %Callable*, %Qubit* }* null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { %Callable*, %Qubit* }*
  %16 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %15, i32 0, i32 0
  %17 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %15, i32 0, i32 1
  %18 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %18, %Callable** %16, align 8
  store %Qubit* %q1, %Qubit** %17, align 8
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__3__FunctionTable, %Tuple* %14)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %19)
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit*, %Qubit* }* getelementptr ({ %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %21 = bitcast %Tuple* %20 to { %Callable*, %Qubit*, %Qubit* }*
  %22 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %21, i32 0, i32 0
  %23 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %21, i32 0, i32 1
  %24 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %21, i32 0, i32 2
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %25, %Callable** %22, align 8
  store %Qubit* %q1, %Qubit** %23, align 8
  store %Qubit* %q2, %Qubit** %24, align 8
  %26 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__4__FunctionTable, %Tuple* %20)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %26)
  %27 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit*, %Qubit* }* getelementptr ({ %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %28 = bitcast %Tuple* %27 to { %Callable*, %Qubit*, %Qubit* }*
  %29 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %28, i32 0, i32 0
  %30 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %28, i32 0, i32 1
  %31 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %28, i32 0, i32 2
  %32 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %32, %Callable** %29, align 8
  store %Qubit* %q1, %Qubit** %30, align 8
  store %Qubit* %q2, %Qubit** %31, align 8
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__4__FunctionTable, %Tuple* %27)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %33)
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, { %Qubit*, %Qubit* }* }* getelementptr ({ %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* null, i32 1) to i64))
  %35 = bitcast %Tuple* %34 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %36 = getelementptr inbounds { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %35, i32 0, i32 0
  %37 = getelementptr inbounds { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %35, i32 0, i32 1
  %38 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 1)
  store %Callable* %38, %Callable** %36, align 8
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %37, align 8
  %39 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__5__FunctionTable, %Tuple* %34)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %39)
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %0, %Tuple* null)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %13, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %13, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %19, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %19, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %26, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %26, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %33, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %33, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %39, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %39, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %40 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR______GUID____InvokeAndIgnore__body(%Callable* %40)
  call void @__quantum__qis__diagnose__body()
  %41 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__capture_update_reference_count(%Callable* %40, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %40, i32 -1)
  ret %String* %41
}
