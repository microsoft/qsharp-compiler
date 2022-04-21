define internal void @Microsoft__Quantum__Testing__QIR__NoArgs__body() {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Tuple* }* getelementptr ({ %Callable*, %Tuple* }, { %Callable*, %Tuple* }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { %Callable*, %Tuple* }*
  %3 = getelementptr inbounds { %Callable*, %Tuple* }, { %Callable*, %Tuple* }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { %Callable*, %Tuple* }, { %Callable*, %Tuple* }* %2, i32 0, i32 1
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR______GUID____NoArgs__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %5, %Callable** %3, align 8
  store %Tuple* null, %Tuple** %4, align 8
  call void @Microsoft__Quantum__ClassicalControl______GUID____ApplyIfOne__body(%Result* %0, { %Callable*, %Tuple* }* %2)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret void
}
