define void @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() #0 {
entry:
  %0 = call %Qubit* @__quantum__rt__qubit_allocate()
  %1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %qs = bitcast %Tuple* %2 to { %Qubit*, %Qubit* }*
  %3 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %4 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  store %Qubit* %0, %Qubit** %3
  store %Qubit* %1, %Qubit** %4
  %5 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %q1 = load %Qubit*, %Qubit** %5
  %6 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %q2 = load %Qubit*, %Qubit** %6
  %7 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %8 = bitcast %Tuple* %7 to { %Callable*, %Qubit* }*
  %9 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %8, i64 0, i32 0
  %10 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %10, %Callable** %9
  call void @__quantum__rt__callable_reference(%Callable* %10)
  %11 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %8, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %11
  %op = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %7)
  %12 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %13 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %14 = load %Qubit*, %Qubit** %12
  %15 = load %Qubit*, %Qubit** %13
  call void @__quantum__qis__swap(%Qubit* %14, %Qubit* %15)
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %17 = bitcast %Tuple* %16 to { %Callable*, %Qubit* }*
  %18 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %17, i64 0, i32 0
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  store %Callable* %19, %Callable** %18
  call void @__quantum__rt__callable_reference(%Callable* %19)
  %20 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %17, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %20
  %21 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %16)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %21)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %23 = bitcast %Tuple* %22 to { %Callable*, %Qubit* }*
  %24 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %23, i64 0, i32 0
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  store %Callable* %25, %Callable** %24
  call void @__quantum__rt__callable_reference(%Callable* %25)
  %26 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %23, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %26
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %22)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %27)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %29 = bitcast %Tuple* %28 to { %Callable*, %Qubit*, %Qubit* }*
  %30 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i64 0, i32 0
  %31 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %31, %Callable** %30
  call void @__quantum__rt__callable_reference(%Callable* %31)
  %32 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %32
  %33 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %29, i64 0, i32 2
  store %Qubit* %q2, %Qubit** %33
  %34 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %28)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %34)
  %35 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %36 = bitcast %Tuple* %35 to { %Callable*, %Qubit*, %Qubit* }*
  %37 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %36, i64 0, i32 0
  %38 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %38, %Callable** %37
  call void @__quantum__rt__callable_reference(%Callable* %38)
  %39 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %36, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %39
  %40 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %36, i64 0, i32 2
  store %Qubit* %q2, %Qubit** %40
  %41 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %35)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %41)
  %42 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %43 = bitcast %Tuple* %42 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %44 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %43, i64 0, i32 0
  %45 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %45, %Callable** %44
  call void @__quantum__rt__callable_reference(%Callable* %45)
  %46 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %43, i64 0, i32 1
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %46
  %47 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %47)
  %48 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %42)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %48)
  %49 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %49, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %0)
  call void @__quantum__rt__qubit_release(%Qubit* %1)
  %50 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  call void @__quantum__rt__callable_unreference(%Callable* %10)
  call void @__quantum__rt__callable_unreference(%Callable* %op)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %21)
  call void @__quantum__rt__callable_unreference(%Callable* %25)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  call void @__quantum__rt__callable_unreference(%Callable* %31)
  call void @__quantum__rt__callable_unreference(%Callable* %34)
  call void @__quantum__rt__callable_unreference(%Callable* %38)
  call void @__quantum__rt__callable_unreference(%Callable* %41)
  call void @__quantum__rt__callable_unreference(%Callable* %45)
  call void @__quantum__rt__callable_unreference(%Callable* %48)
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %51)
  call void @__quantum__rt__callable_unreference(%Callable* %51)
  ret void
}
