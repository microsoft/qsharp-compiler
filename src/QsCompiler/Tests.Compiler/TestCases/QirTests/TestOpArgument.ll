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
  %6 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %q1 = load %Qubit*, %Qubit** %5
  %q2 = load %Qubit*, %Qubit** %6
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { %Callable*, %Qubit* }*
  %10 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 0
  %11 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 1
  store %Callable* %7, %Callable** %10
  call void @__quantum__rt__callable_reference(%Callable* %7)
  store %Qubit* %q1, %Qubit** %11
  %op = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %8)
  %12 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %13 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %.control = load %Qubit*, %Qubit** %12
  %.target = load %Qubit*, %Qubit** %13
  call void @__quantum__qis__cnot__body(%Qubit* %.control, %Qubit* %.target)
  %14 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %15 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %16 = load %Qubit*, %Qubit** %14
  %17 = load %Qubit*, %Qubit** %15
  call void @__quantum__qis__swap(%Qubit* %16, %Qubit* %17)
  %18 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %20 = bitcast %Tuple* %19 to { %Callable*, %Qubit* }*
  %21 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %20, i64 0, i32 0
  %22 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %20, i64 0, i32 1
  store %Callable* %18, %Callable** %21
  call void @__quantum__rt__callable_reference(%Callable* %18)
  store %Qubit* %q1, %Qubit** %22
  %23 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %19)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %23)
  %24 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  %25 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %26 = bitcast %Tuple* %25 to { %Callable*, %Qubit* }*
  %27 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %26, i64 0, i32 0
  %28 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %26, i64 0, i32 1
  store %Callable* %24, %Callable** %27
  call void @__quantum__rt__callable_reference(%Callable* %24)
  store %Qubit* %q1, %Qubit** %28
  %29 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %25)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %29)
  %30 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %31 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %32 = bitcast %Tuple* %31 to { %Callable*, %Qubit*, %Qubit* }*
  %33 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %32, i64 0, i32 0
  %34 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %32, i64 0, i32 1
  %35 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %32, i64 0, i32 2
  store %Callable* %30, %Callable** %33
  call void @__quantum__rt__callable_reference(%Callable* %30)
  store %Qubit* %q1, %Qubit** %34
  store %Qubit* %q2, %Qubit** %35
  %36 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %31)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %36)
  %37 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %38 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %39 = bitcast %Tuple* %38 to { %Callable*, %Qubit*, %Qubit* }*
  %40 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %39, i64 0, i32 0
  %41 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %39, i64 0, i32 1
  %42 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %39, i64 0, i32 2
  store %Callable* %37, %Callable** %40
  call void @__quantum__rt__callable_reference(%Callable* %37)
  store %Qubit* %q1, %Qubit** %41
  store %Qubit* %q2, %Qubit** %42
  %43 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %38)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %43)
  %44 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %45 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %46 = bitcast %Tuple* %45 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %47 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %46, i64 0, i32 0
  %48 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %46, i64 0, i32 1
  store %Callable* %44, %Callable** %47
  call void @__quantum__rt__callable_reference(%Callable* %44)
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %48
  %49 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %49)
  %50 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %45)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %50)
  %51 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %51, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %0)
  call void @__quantum__rt__qubit_release(%Qubit* %1)
  %52 = bitcast { %Qubit*, %Qubit* }* %qs to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %52)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  %53 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 0
  %54 = load %Callable*, %Callable** %53
  call void @__quantum__rt__callable_unreference(%Callable* %54)
  %55 = bitcast { %Callable*, %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %55)
  call void @__quantum__rt__callable_unreference(%Callable* %op)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  %56 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %20, i64 0, i32 0
  %57 = load %Callable*, %Callable** %56
  call void @__quantum__rt__callable_unreference(%Callable* %57)
  %58 = bitcast { %Callable*, %Qubit* }* %20 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %58)
  call void @__quantum__rt__callable_unreference(%Callable* %23)
  call void @__quantum__rt__callable_unreference(%Callable* %24)
  %59 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %26, i64 0, i32 0
  %60 = load %Callable*, %Callable** %59
  call void @__quantum__rt__callable_unreference(%Callable* %60)
  %61 = bitcast { %Callable*, %Qubit* }* %26 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %61)
  call void @__quantum__rt__callable_unreference(%Callable* %29)
  call void @__quantum__rt__callable_unreference(%Callable* %30)
  %62 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %32, i64 0, i32 0
  %63 = load %Callable*, %Callable** %62
  call void @__quantum__rt__callable_unreference(%Callable* %63)
  %64 = bitcast { %Callable*, %Qubit*, %Qubit* }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %64)
  call void @__quantum__rt__callable_unreference(%Callable* %36)
  call void @__quantum__rt__callable_unreference(%Callable* %37)
  %65 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %39, i64 0, i32 0
  %66 = load %Callable*, %Callable** %65
  call void @__quantum__rt__callable_unreference(%Callable* %66)
  %67 = bitcast { %Callable*, %Qubit*, %Qubit* }* %39 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %67)
  call void @__quantum__rt__callable_unreference(%Callable* %43)
  call void @__quantum__rt__callable_unreference(%Callable* %44)
  %68 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %46, i64 0, i32 0
  %69 = load %Callable*, %Callable** %68
  call void @__quantum__rt__callable_unreference(%Callable* %69)
  %70 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %46, i64 0, i32 1
  %71 = load { %Qubit*, %Qubit* }*, { %Qubit*, %Qubit* }** %70
  %72 = bitcast { %Qubit*, %Qubit* }* %71 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %72)
  %73 = bitcast { %Callable*, { %Qubit*, %Qubit* }* }* %46 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %73)
  call void @__quantum__rt__callable_unreference(%Callable* %50)
  %74 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %74)
  call void @__quantum__qis__diagnose__body()
  %75 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__callable_unreference(%Callable* %74)
  ret %String* %75
}
