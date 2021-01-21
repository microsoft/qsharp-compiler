define void @Microsoft__Quantum__Testing__QIR__TestOperationCalls__body() #0 {
entry:
  %doNothing = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnDoNothing__body(i64 1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %doNothing, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %doNothing, i64 1)
  %aux = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @Microsoft__Quantum__Testing__QIR__CNOT__body(%Qubit* %aux, %Qubit* %aux)
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %2 = bitcast i8* %1 to %Qubit**
  store %Qubit* %aux, %Qubit** %2
  call void @Microsoft__Quantum__Testing__QIR__Empty__body(%Array* %0)
  %3 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %3, i64 0)
  %5 = bitcast i8* %4 to %Qubit**
  store %Qubit* %aux, %Qubit** %5
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { %Array* }*
  %8 = getelementptr { %Array* }, { %Array* }* %7, i64 0, i32 0
  store %Array* %3, %Array** %8
  call void @__quantum__rt__callable_invoke(%Callable* %doNothing, %Tuple* %6, %Tuple* null)
  %9 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 0)
  %11 = bitcast i8* %10 to %Qubit**
  store %Qubit* %aux, %Qubit** %11
  %12 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 0)
  %14 = bitcast i8* %13 to %Qubit**
  store %Qubit* %aux, %Qubit** %14
  call void @Microsoft__Quantum__Testing__QIR__DoNothing__ctl(%Array* %9, %Array* %12)
  call void @__quantum__rt__qubit_release(%Qubit* %aux)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i64 -1)
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnDoNothing, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR__TakesSingleTupleArg__body(i64 2, %Callable* %15)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %doNothing, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %doNothing, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %doNothing, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %doNothing, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %15, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %15, i64 -1)
  ret void
}
