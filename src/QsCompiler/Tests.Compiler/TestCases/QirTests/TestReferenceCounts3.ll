define internal void @Microsoft__Quantum__Testing__QIR__Main__body() {
entry:
  %0 = call { %String*, %Array* }* @Microsoft__Quantum__Testing__QIR__TestPendingRefCountIncreases__body(i1 true)
  call void @Microsoft__Quantum__Testing__QIR__TestRefCountsForItemUpdate__body(i1 true)
  %id = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR______GUID____Identity__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %id, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %id, i32 1)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i64 }* getelementptr ({ i64, i64 }, { i64, i64 }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { i64, i64 }*
  %3 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %2, i32 0, i32 1
  store i64 0, i64* %3, align 4
  store i64 0, i64* %4, align 4
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i64 }* getelementptr ({ i64, i64 }, { i64, i64 }* null, i32 1) to i64))
  call void @__quantum__rt__callable_invoke(%Callable* %id, %Tuple* %1, %Tuple* %5)
  %6 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %0, i32 0, i32 0
  %7 = load %String*, %String** %6, align 8
  %8 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %0, i32 0, i32 1
  %9 = load %Array*, %Array** %8, align 8
  %10 = bitcast %Tuple* %5 to { i64, i64 }*
  call void @__quantum__rt__capture_update_alias_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %7, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  %11 = bitcast { %String*, %Array* }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  ret void
}
