define %String* @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() #0 {
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
  %.control = load %Qubit*, %Qubit** %12
  %13 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %.target = load %Qubit*, %Qubit** %13
  call void @__quantum__qis__cnot__body(%Qubit* %.control, %Qubit* %.target)
  %14 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %15 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %16 = load %Qubit*, %Qubit** %14
  %17 = load %Qubit*, %Qubit** %15
  call void @__quantum__qis__swap(%Qubit* %16, %Qubit* %17)
  %18 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %19 = bitcast %Tuple* %18 to { %Callable*, %Qubit* }*
  %20 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %19, i64 0, i32 0
  %21 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  store %Callable* %21, %Callable** %20
  call void @__quantum__rt__callable_reference(%Callable* %21)
  %22 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %19, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %22
  %23 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %18)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %23)
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %25 = bitcast %Tuple* %24 to { %Callable*, %Qubit* }*
  %26 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %25, i64 0, i32 0
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  store %Callable* %27, %Callable** %26
  call void @__quantum__rt__callable_reference(%Callable* %27)
  %28 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %25, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %28
  %29 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %24)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %29)
  %30 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %31 = bitcast %Tuple* %30 to { %Callable*, %Qubit*, %Qubit* }*
  %32 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %31, i64 0, i32 0
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %33, %Callable** %32
  call void @__quantum__rt__callable_reference(%Callable* %33)
  %34 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %31, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %34
  %35 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %31, i64 0, i32 2
  store %Qubit* %q2, %Qubit** %35
  %36 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %30)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %36)
  %37 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %38 = bitcast %Tuple* %37 to { %Callable*, %Qubit*, %Qubit* }*
  %39 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 0
  %40 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %40, %Callable** %39
  call void @__quantum__rt__callable_reference(%Callable* %40)
  %41 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 1
  store %Qubit* %q1, %Qubit** %41
  %42 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 2
  store %Qubit* %q2, %Qubit** %42
  %43 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %37)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %43)
  %44 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %45 = bitcast %Tuple* %44 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %46 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %45, i64 0, i32 0
  %47 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  store %Callable* %47, %Callable** %46
  call void @__quantum__rt__callable_reference(%Callable* %47)
  %48 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %45, i64 0, i32 1
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %48
  %49 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %49)
  %50 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %44)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %50)
  %51 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %51, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %0)
  call void @__quantum__rt__qubit_release(%Qubit* %1)
  %52 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %52)
  call void @__quantum__rt__callable_unreference(%Callable* %10)
  call void @__quantum__rt__callable_unreference(%Callable* %op)
  call void @__quantum__rt__callable_unreference(%Callable* %21)
  call void @__quantum__rt__callable_unreference(%Callable* %23)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  call void @__quantum__rt__callable_unreference(%Callable* %29)
  call void @__quantum__rt__callable_unreference(%Callable* %33)
  call void @__quantum__rt__callable_unreference(%Callable* %36)
  call void @__quantum__rt__callable_unreference(%Callable* %40)
  call void @__quantum__rt__callable_unreference(%Callable* %43)
  call void @__quantum__rt__callable_unreference(%Callable* %47)
  call void @__quantum__rt__callable_unreference(%Callable* %50)
  %53 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %53)
  call void @__quantum__qis__diagnose__body()
  %54 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__callable_unreference(%Callable* %53)
  ret %String* %54
}
