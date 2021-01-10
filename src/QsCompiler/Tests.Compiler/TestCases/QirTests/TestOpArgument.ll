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
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %5 = bitcast %Tuple* %4 to { %Callable*, %Qubit* }*
  %6 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %5, i64 0, i32 0
  %7 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %5, i64 0, i32 1
  store %Callable* %3, %Callable** %6
  call void @__quantum__rt__callable_reference(%Callable* %3)
  store %Qubit* %q1, %Qubit** %7
  %op = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %4)
  call void @__quantum__qis__cnot__body(%Qubit* %q1, %Qubit* %q2)
  call void @__quantum__qis__swap(%Qubit* %q1, %Qubit* %q2)
  %8 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %10 = bitcast %Tuple* %9 to { %Callable*, %Qubit* }*
  %11 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %10, i64 0, i32 0
  %12 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %10, i64 0, i32 1
  store %Callable* %8, %Callable** %11
  call void @__quantum__rt__callable_reference(%Callable* %8)
  store %Qubit* %q1, %Qubit** %12
  %13 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %9)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %13)
  %14 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  %15 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %16 = bitcast %Tuple* %15 to { %Callable*, %Qubit* }*
  %17 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %16, i64 0, i32 0
  %18 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %16, i64 0, i32 1
  store %Callable* %14, %Callable** %17
  call void @__quantum__rt__callable_reference(%Callable* %14)
  store %Qubit* %q1, %Qubit** %18
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %15)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %19)
  %20 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %22 = bitcast %Tuple* %21 to { %Callable*, %Qubit*, %Qubit* }*
  %23 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %22, i64 0, i32 0
  %24 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %22, i64 0, i32 1
  %25 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %22, i64 0, i32 2
  store %Callable* %20, %Callable** %23
  call void @__quantum__rt__callable_reference(%Callable* %20)
  store %Qubit* %q1, %Qubit** %24
  store %Qubit* %q2, %Qubit** %25
  %26 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %21)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %26)
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %29 = bitcast %Tuple* %28 to { %Callable*, %Qubit*, %Qubit* }*
  %30 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i64 0, i32 0
  %31 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i64 0, i32 1
  %32 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i64 0, i32 2
  store %Callable* %27, %Callable** %30
  call void @__quantum__rt__callable_reference(%Callable* %27)
  store %Qubit* %q1, %Qubit** %31
  store %Qubit* %q2, %Qubit** %32
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %28)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %33)
  %34 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %35 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %36 = bitcast %Tuple* %35 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %37 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %36, i64 0, i32 0
  %38 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %36, i64 0, i32 1
  store %Callable* %34, %Callable** %37
  call void @__quantum__rt__callable_reference(%Callable* %34)
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %38
  call void @__quantum__rt__tuple_reference(%Tuple* %0)
  %39 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %35)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %39)
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %0, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %op)
  call void @__quantum__rt__callable_unreference(%Callable* %8)
  call void @__quantum__rt__callable_unreference(%Callable* %13)
  call void @__quantum__rt__callable_unreference(%Callable* %14)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %20)
  call void @__quantum__rt__callable_unreference(%Callable* %26)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  call void @__quantum__rt__callable_unreference(%Callable* %33)
  call void @__quantum__rt__callable_unreference(%Callable* %34)
  call void @__quantum__rt__callable_unreference(%Callable* %39)
  %40 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %40)
  call void @__quantum__qis__diagnose__body()
  %41 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__callable_unreference(%Callable* %40)
  ret %String* %41
}
