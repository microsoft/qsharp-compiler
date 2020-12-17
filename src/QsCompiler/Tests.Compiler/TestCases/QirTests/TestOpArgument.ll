define void @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() #0 {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %1 = bitcast %Tuple* %0 to { %Callable*, %Qubit* }*
  %2 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %1, i64 0, i32 0
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  store %Callable* %3, %Callable** %2
  call void @__quantum__rt__callable_reference(%Callable* %3)
  %4 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %1, i64 0, i32 1
  store %Qubit* %q, %Qubit** %4
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %5)
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { %Callable*, %Qubit* }*
  %8 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %7, i64 0, i32 0
  %9 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  store %Callable* %9, %Callable** %8
  call void @__quantum__rt__callable_reference(%Callable* %9)
  %10 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %7, i64 0, i32 1
  store %Qubit* %q, %Qubit** %10
  %11 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %6)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %11)
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %13 = bitcast %Tuple* %12 to { %Callable*, %Qubit*, %Qubit* }*
  %14 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %13, i64 0, i32 0
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %15, %Callable** %14
  call void @__quantum__rt__callable_reference(%Callable* %15)
  %16 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %13, i64 0, i32 1
  store %Qubit* %q, %Qubit** %16
  %17 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %13, i64 0, i32 2
  store %Qubit* %q, %Qubit** %17
  %18 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %12)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %18)
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %20 = bitcast %Tuple* %19 to { %Callable*, %Qubit*, %Qubit* }*
  %21 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %20, i64 0, i32 0
  %22 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %22, %Callable** %21
  call void @__quantum__rt__callable_reference(%Callable* %22)
  %23 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %20, i64 0, i32 1
  store %Qubit* %q, %Qubit** %23
  %24 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %20, i64 0, i32 2
  store %Qubit* %q, %Qubit** %24
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %19)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %25)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %5)
  call void @__quantum__rt__callable_unreference(%Callable* %9)
  call void @__quantum__rt__callable_unreference(%Callable* %11)
  call void @__quantum__rt__callable_unreference(%Callable* %15)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  call void @__quantum__rt__callable_unreference(%Callable* %22)
  call void @__quantum__rt__callable_unreference(%Callable* %25)
  %26 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %26)
  call void @__quantum__rt__callable_unreference(%Callable* %26)
  ret void
}
