define void @Microsoft__Quantum__Testing__QIR__NoArgs__body() {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %2 = bitcast %Tuple* %1 to { %Callable*, %Qubit* }*
  %3 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { %Callable*, %Qubit* }, { %Callable*, %Qubit* }* %2, i32 0, i32 1
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR_____GUID___NoArgs, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %5, %Callable** %3, align 8
  store %Qubit* %q, %Qubit** %4, align 8
  call void @Microsoft__Quantum__Simulation__QuantumProcessor__Extensions_____GUID___ApplyIfOne__body(%Result* %0, { %Callable*, %Qubit* }* %2)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %5, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %5, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i64 -1)
  ret void
}
