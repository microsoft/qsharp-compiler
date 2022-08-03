define internal void @Microsoft__Quantum__Testing__QIR__NoArgs__body() {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR_____GUID___NoArgs__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %1, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %1, i32 1)
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Tuple* }* getelementptr ({ %Callable*, %Tuple* }, { %Callable*, %Tuple* }* null, i32 1) to i64))
  %3 = bitcast %Tuple* %2 to { %Callable*, %Tuple* }*
  %4 = getelementptr inbounds { %Callable*, %Tuple* }, { %Callable*, %Tuple* }* %3, i32 0, i32 0
  %5 = getelementptr inbounds { %Callable*, %Tuple* }, { %Callable*, %Tuple* }* %3, i32 0, i32 1
  store %Callable* %1, %Callable** %4, align 8
  store %Tuple* null, %Tuple** %5, align 8
  call void @Microsoft__Quantum__ClassicalControl_____GUID___ApplyIfOne__body(%Result* %0, { %Callable*, %Tuple* }* %3)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %1, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %1, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %1, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret void
}
