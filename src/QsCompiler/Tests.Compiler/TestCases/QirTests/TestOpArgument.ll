define void @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() #0 {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, %Qubit* }* getelementptr ({ %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %Callable*, %Qubit* }*
  %2 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %1, i64 0, i32 1
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %TupleHeader* null)
  store %Callable* %3, %Callable** %2
  %4 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %1, i64 0, i32 2
  store %Qubit* %q, %Qubit** %4
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__1, %TupleHeader* %0)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %5)
  %6 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, %Qubit* }* getelementptr ({ %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* null, i32 1) to i64))
  %7 = bitcast %TupleHeader* %6 to { %TupleHeader, %Callable*, %Qubit* }*
  %8 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %7, i64 0, i32 1
  %9 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR___SWAP, %TupleHeader* null)
  store %Callable* %9, %Callable** %8
  %10 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %7, i64 0, i32 2
  store %Qubit* %q, %Qubit** %10
  %11 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__2, %TupleHeader* %6)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %11)
  %12 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, %Qubit*, %Qubit* }* getelementptr ({ %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %13 = bitcast %TupleHeader* %12 to { %TupleHeader, %Callable*, %Qubit*, %Qubit* }*
  %14 = getelementptr { %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* %13, i64 0, i32 1
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %TupleHeader* null)
  store %Callable* %15, %Callable** %14
  %16 = getelementptr { %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* %13, i64 0, i32 2
  store %Qubit* %q, %Qubit** %16
  %17 = getelementptr { %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* %13, i64 0, i32 3
  store %Qubit* %q, %Qubit** %17
  %18 = bitcast %TupleHeader* %8 to { %TupleHeader, %Qubit*, %Qubit* }*
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__3, %TupleHeader* %12)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %19)
  %20 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, %Qubit*, %Qubit* }* getelementptr ({ %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %21 = bitcast %TupleHeader* %20 to { %TupleHeader, %Callable*, %Qubit*, %Qubit* }*
  %22 = getelementptr { %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* %21, i64 0, i32 1
  %23 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR___Choose, %TupleHeader* null)
  store %Callable* %23, %Callable** %22
  %24 = getelementptr { %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* %21, i64 0, i32 2
  store %Qubit* %q, %Qubit** %24
  %25 = getelementptr { %TupleHeader, %Callable*, %Qubit*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit*, %Qubit* }* %21, i64 0, i32 3
  store %Qubit* %q, %Qubit** %25
  %26 = bitcast %TupleHeader* %8 to { %TupleHeader, %Qubit*, %Qubit* }*
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__4, %TupleHeader* %20)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %27)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__callable_unreference(%Callable* %5)
  call void @__quantum__rt__callable_unreference(%Callable* %11)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  %28 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__GetNestedTuple, %TupleHeader* null)
  call void @Microsoft__Quantum__Testing__QIR_____GUID___InvokeAndIgnore__body(%Callable* %28)
  call void @__quantum__rt__callable_unreference(%Callable* %28)
  ret void
}
