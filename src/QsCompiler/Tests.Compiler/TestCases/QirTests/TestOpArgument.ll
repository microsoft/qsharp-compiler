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
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Qubit* }, { %Callable*, %Qubit* }* null, i32 1) to i64))
  %5 = bitcast %Tuple* %4 to { %Callable*, %Qubit* }*
  %6 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %5, i32 0, i32 0
  %7 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %5, i32 0, i32 1
  store %Callable* %3, %Callable** %6, align 8
  store %Qubit* %q1, %Qubit** %7, align 8
  %op = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %4)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op, i32 1)
  call void @__quantum__qis__cnot__body(%Qubit* %q1, %Qubit* %q2)
  call void @__quantum__qis__swap(%Qubit* %q1, %Qubit* %q2)
  %8 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Qubit* }, { %Callable*, %Qubit* }* null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Callable*, %Qubit* }*
  %11 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %10, i32 0, i32 0
  %12 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %10, i32 0, i32 1
  store %Callable* %8, %Callable** %11, align 8
  store %Qubit* %q1, %Qubit** %12, align 8
  %13 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__2__FunctionTable, %Tuple* %9)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %13)
  %14 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %15 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Qubit* }, { %Callable*, %Qubit* }* null, i32 1) to i64))
  %16 = bitcast %Tuple* %15 to { %Callable*, %Qubit* }*
  %17 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %16, i32 0, i32 0
  %18 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %16, i32 0, i32 1
  store %Callable* %14, %Callable** %17, align 8
  store %Qubit* %q1, %Qubit** %18, align 8
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__3__FunctionTable, %Tuple* %15)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %19)
  %20 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit*, %Qubit* }* getelementptr ({ %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %22 = bitcast %Tuple* %21 to { %Callable*, %Qubit*, %Qubit* }*
  %23 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %22, i32 0, i32 0
  %24 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %22, i32 0, i32 1
  %25 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %22, i32 0, i32 2
  store %Callable* %20, %Callable** %23, align 8
  store %Qubit* %q1, %Qubit** %24, align 8
  store %Qubit* %q2, %Qubit** %25, align 8
  %26 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__4__FunctionTable, %Tuple* %21)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %26)
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Qubit*, %Qubit* }* getelementptr ({ %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %29 = bitcast %Tuple* %28 to { %Callable*, %Qubit*, %Qubit* }*
  %30 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i32 0, i32 0
  %31 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i32 0, i32 1
  %32 = getelementptr inbounds { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i32 0, i32 2
  store %Callable* %27, %Callable** %30, align 8
  store %Qubit* %q1, %Qubit** %31, align 8
  store %Qubit* %q2, %Qubit** %32, align 8
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__4__FunctionTable, %Tuple* %28)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %33)
  %34 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 1)
  %35 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, { %Qubit*, %Qubit* }* }* getelementptr ({ %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* null, i32 1) to i64))
  %36 = bitcast %Tuple* %35 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %37 = getelementptr inbounds { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %36, i32 0, i32 0
  %38 = getelementptr inbounds { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %36, i32 0, i32 1
  store %Callable* %34, %Callable** %37, align 8
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %38, align 8
  %39 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__5__FunctionTable, %Tuple* %35)
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
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %40)
  call void @__quantum__qis__diagnose__body()
  %41 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__capture_update_reference_count(%Callable* %40, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %40, i32 -1)
  ret %String* %41
}
