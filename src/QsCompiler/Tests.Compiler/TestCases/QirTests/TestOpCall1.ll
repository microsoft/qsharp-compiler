define void @Microsoft__Quantum__Testing__QIR__TestOperationCalls__body() #0 {
entry:
  %doNothing = call %Callable* @Microsoft__Quantum__Testing__QIR__ReturnDoNothing__body(i64 1)
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
  call void @__quantum__rt__array_reference(%Array* %3)
  %9 = bitcast { %Array* }* %7 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %doNothing, %Tuple* %9, %Tuple* null)
  %10 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %10, i64 0)
  %12 = bitcast i8* %11 to %Qubit**
  store %Qubit* %aux, %Qubit** %12
  %13 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %13, i64 0)
  %15 = bitcast i8* %14 to %Qubit**
  store %Qubit* %aux, %Qubit** %15
  call void @Microsoft__Quantum__Testing__QIR__DoNothing__ctl(%Array* %10, %Array* %13)
  call void @__quantum__rt__qubit_release(%Qubit* %aux)
  call void @__quantum__rt__array_unreference(%Array* %0)
  call void @__quantum__rt__array_unreference(%Array* %3)
  %16 = getelementptr { %Array* }, { %Array* }* %7, i64 0, i32 0
  %17 = load %Array*, %Array** %16
  call void @__quantum__rt__array_unreference(%Array* %17)
  %18 = bitcast { %Array* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  call void @__quantum__rt__array_unreference(%Array* %10)
  call void @__quantum__rt__array_unreference(%Array* %13)
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnDoNothing, %Tuple* null)
  call void @Microsoft__Quantum__Testing__QIR__TakesSingleTupleArg__body(i64 2, %Callable* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %doNothing)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  ret void
}
