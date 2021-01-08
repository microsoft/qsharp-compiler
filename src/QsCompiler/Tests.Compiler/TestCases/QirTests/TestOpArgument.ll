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
  call void @__quantum__rt__tuple_add_access(%Tuple* %2)
  %5 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %q1 = load %Qubit*, %Qubit** %5
  %6 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
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
  %12 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 0
  %13 = load %Callable*, %Callable** %12
  call void @__quantum__rt__callable_reference(%Callable* %13)
  call void @__quantum__rt__tuple_reference(%Tuple* %8)
  %14 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %control__inline__1 = load %Qubit*, %Qubit** %14
  %15 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %target__inline__1 = load %Qubit*, %Qubit** %15
  call void @__quantum__qis__cnot__body(%Qubit* %control__inline__1, %Qubit* %target__inline__1)
  %16 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 0
  %17 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %qs, i64 0, i32 1
  %18 = load %Qubit*, %Qubit** %16
  %19 = load %Qubit*, %Qubit** %17
  call void @__quantum__qis__swap(%Qubit* %18, %Qubit* %19)
  %20 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %Tuple* null)
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %22 = bitcast %Tuple* %21 to { %Callable*, %Qubit* }*
  %23 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %22, i64 0, i32 0
  %24 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %22, i64 0, i32 1
  store %Callable* %20, %Callable** %23
  call void @__quantum__rt__callable_reference(%Callable* %20)
  store %Qubit* %q1, %Qubit** %24
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %21)
  %26 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %22, i64 0, i32 0
  %27 = load %Callable*, %Callable** %26
  call void @__quantum__rt__callable_reference(%Callable* %27)
  call void @__quantum__rt__tuple_reference(%Tuple* %21)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %25)
  %28 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %Tuple* null)
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %30 = bitcast %Tuple* %29 to { %Callable*, %Qubit* }*
  %31 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %30, i64 0, i32 0
  %32 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %30, i64 0, i32 1
  store %Callable* %28, %Callable** %31
  call void @__quantum__rt__callable_reference(%Callable* %28)
  store %Qubit* %q1, %Qubit** %32
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %29)
  %34 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %30, i64 0, i32 0
  %35 = load %Callable*, %Callable** %34
  call void @__quantum__rt__callable_reference(%Callable* %35)
  call void @__quantum__rt__tuple_reference(%Tuple* %29)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %33)
  %36 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %37 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %38 = bitcast %Tuple* %37 to { %Callable*, %Qubit*, %Qubit* }*
  %39 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 0
  %40 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 1
  %41 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 2
  store %Callable* %36, %Callable** %39
  call void @__quantum__rt__callable_reference(%Callable* %36)
  store %Qubit* %q1, %Qubit** %40
  store %Qubit* %q2, %Qubit** %41
  %42 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %37)
  %43 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 0
  %44 = load %Callable*, %Callable** %43
  call void @__quantum__rt__callable_reference(%Callable* %44)
  call void @__quantum__rt__tuple_reference(%Tuple* %37)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %42)
  %45 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %46 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %47 = bitcast %Tuple* %46 to { %Callable*, %Qubit*, %Qubit* }*
  %48 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %47, i64 0, i32 0
  %49 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %47, i64 0, i32 1
  %50 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %47, i64 0, i32 2
  store %Callable* %45, %Callable** %48
  call void @__quantum__rt__callable_reference(%Callable* %45)
  store %Qubit* %q1, %Qubit** %49
  store %Qubit* %q2, %Qubit** %50
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %46)
  %52 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %47, i64 0, i32 0
  %53 = load %Callable*, %Callable** %52
  call void @__quantum__rt__callable_reference(%Callable* %53)
  call void @__quantum__rt__tuple_reference(%Tuple* %46)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %51)
  %54 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %Tuple* null)
  %55 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %56 = bitcast %Tuple* %55 to { %Callable*, { %Qubit*, %Qubit* }* }*
  %57 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %56, i64 0, i32 0
  %58 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %56, i64 0, i32 1
  store %Callable* %54, %Callable** %57
  call void @__quantum__rt__callable_reference(%Callable* %54)
  store { %Qubit*, %Qubit* }* %qs, { %Qubit*, %Qubit* }** %58
  call void @__quantum__rt__tuple_reference(%Tuple* %2)
  %59 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %55)
  %60 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %56, i64 0, i32 0
  %61 = load %Callable*, %Callable** %60
  call void @__quantum__rt__callable_reference(%Callable* %61)
  %62 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %56, i64 0, i32 1
  %63 = load { %Qubit*, %Qubit* }*, { %Qubit*, %Qubit* }** %62
  %64 = bitcast { %Qubit*, %Qubit* }* %63 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %64)
  call void @__quantum__rt__tuple_reference(%Tuple* %55)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %59)
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %2, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %0)
  call void @__quantum__rt__qubit_release(%Qubit* %1)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %2)
  call void @__quantum__rt__tuple_unreference(%Tuple* %2)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  %65 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %9, i64 0, i32 0
  %66 = load %Callable*, %Callable** %65
  call void @__quantum__rt__callable_unreference(%Callable* %66)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  call void @__quantum__rt__callable_unreference(%Callable* %op)
  call void @__quantum__rt__callable_unreference(%Callable* %20)
  %67 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %22, i64 0, i32 0
  %68 = load %Callable*, %Callable** %67
  call void @__quantum__rt__callable_unreference(%Callable* %68)
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  call void @__quantum__rt__callable_unreference(%Callable* %25)
  call void @__quantum__rt__callable_unreference(%Callable* %28)
  %69 = getelementptr { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %30, i64 0, i32 0
  %70 = load %Callable*, %Callable** %69
  call void @__quantum__rt__callable_unreference(%Callable* %70)
  call void @__quantum__rt__tuple_unreference(%Tuple* %29)
  call void @__quantum__rt__callable_unreference(%Callable* %33)
  call void @__quantum__rt__callable_unreference(%Callable* %36)
  %71 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %38, i64 0, i32 0
  %72 = load %Callable*, %Callable** %71
  call void @__quantum__rt__callable_unreference(%Callable* %72)
  call void @__quantum__rt__tuple_unreference(%Tuple* %37)
  call void @__quantum__rt__callable_unreference(%Callable* %42)
  call void @__quantum__rt__callable_unreference(%Callable* %45)
  %73 = getelementptr { %Callable*, %Qubit*, %Qubit* }, { %Callable*, %Qubit*, %Qubit* }* %47, i64 0, i32 0
  %74 = load %Callable*, %Callable** %73
  call void @__quantum__rt__callable_unreference(%Callable* %74)
  call void @__quantum__rt__tuple_unreference(%Tuple* %46)
  call void @__quantum__rt__callable_unreference(%Callable* %51)
  call void @__quantum__rt__callable_unreference(%Callable* %54)
  %75 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %56, i64 0, i32 0
  %76 = load %Callable*, %Callable** %75
  call void @__quantum__rt__callable_unreference(%Callable* %76)
  %77 = getelementptr { %Callable*, { %Qubit*, %Qubit* }* }, { %Callable*, { %Qubit*, %Qubit* }* }* %56, i64 0, i32 1
  %78 = load { %Qubit*, %Qubit* }*, { %Qubit*, %Qubit* }** %77
  %79 = bitcast { %Qubit*, %Qubit* }* %78 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %79)
  call void @__quantum__rt__tuple_unreference(%Tuple* %55)
  call void @__quantum__rt__callable_unreference(%Callable* %59)
  %80 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %80)
  call void @__quantum__qis__diagnose__body()
  %81 = call %String* @__quantum__qis__message()
  call void @__quantum__rt__callable_unreference(%Callable* %80)
  ret %String* %81
}
