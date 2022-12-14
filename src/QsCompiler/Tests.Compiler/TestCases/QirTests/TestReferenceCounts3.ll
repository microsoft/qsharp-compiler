define internal void @Microsoft__Quantum__Testing__QIR__Main__body() {
entry:
  %0 = call { %String*, %Array* }* @Microsoft__Quantum__Testing__QIR__TestPendingRefCountIncreases__body(i1 true)
  call void @Microsoft__Quantum__Testing__QIR__TestRefCountsForItemUpdate__body(i1 true)
  %id = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR_____GUID___Identity__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %id, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %id, i32 1)
  %1 = call { i64, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Identity__body(i64 0, i64 0)
  %2 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %0, i32 0, i32 0
  %3 = load %String*, %String** %2, align 8
  %4 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %0, i32 0, i32 1
  %5 = load %Array*, %Array** %4, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %3, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %5, i32 -1)
  %6 = bitcast { %String*, %Array* }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %id, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %id, i32 -1)
  %7 = bitcast { i64, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  ret void
}
