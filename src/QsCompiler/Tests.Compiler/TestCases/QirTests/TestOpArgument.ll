define %String* @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() #0 {
entry:
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %qs = bitcast %Tuple* %0 to { %Qubit*, %Qubit* }*
  %1 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %2 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %1
  store %Qubit* %q2, %Qubit** %2
  call void @__quantum__rt__tuple_add_access(%Tuple* %0)
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %4 = bitcast %Tuple* %3 to { %Callable*, %Qubit* }*
  %5 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %4, i64 0, i32 0
  %6 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %4, i64 0, i32 1
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %7, %Callable** %5
  store %Qubit* %q1, %Qubit** %6
  %op = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %3)
  call void @__quantum__qis__cnot__body(%Qubit* %q1, %Qubit* %q2)
  call void @__quantum__qis__swap(%Qubit* %q1, %Qubit* %q2)
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { %Callable*, %Qubit* }*
  %10 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 0
  %11 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 1
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  store %Callable* %12, %Callable** %10
  store %Qubit* %q1, %Qubit** %11
  %13 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %8)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %13)
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %15 = bitcast %Tuple* %14 to { %Callable*, %Qubit* }*
  %16 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %15, i64 0, i32 0
  %17 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %15, i64 0, i32 1
  %18 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  store %Callable* %18, %Callable** %16
  store %Qubit* %q1, %Qubit** %17
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %14)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %19)
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %21 = bitcast %Tuple* %20 to { %Callable*, %Qubit*, %Qubit* }*
  %22 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %21, i64 0, i32 0
  %23 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %21, i64 0, i32 1
  %24 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %21, i64 0, i32 2
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %25, %Callable** %22
  store %Qubit* %q1, %Qubit** %23
  store %Qubit* %q2, %Qubit** %24
  %26 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %20)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %26)
  %27 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %28 = bitcast %Tuple* %27 to { %Callable*, %Qubit*, %Qubit* }*
  %29 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %28, i64 0, i32 0
  %30 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %28, i64 0, i32 1
  %31 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %28, i64 0, i32 2
  %32 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %32, %Callable** %29
  store %Qubit* %q1, %Qubit** %30
  store %Qubit* %q2, %Qubit** %31
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %27)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %33)
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %35 = bitcast %Tuple* %34 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %36 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %35, i64 0, i32 0
  %37 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %35, i64 0, i32 1
  %38 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  call void @__quantum__rt__tuple_reference(%Tuple* %0)
  store %Callable* %38, %Callable** %36
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %37
  %39 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %34)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %39)
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %0, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %op)
  call void @__quantum__rt__callable_unreference(%Callable* %13)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %26)
  call void @__quantum__rt__callable_unreference(%Callable* %33)
  call void @__quantum__rt__callable_unreference(%Callable* %39)
  %40 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %40)
  call void @__quantum__qis__diagnose__body()
  %41 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__callable_unreference(%Callable* %40)
  ret %String* %41
}
