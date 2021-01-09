define void @Microsoft__Quantum__Testing__QIR__CNOT__ctl(%Array* %__controlQubits__, { %Qubit*, %Qubit* }* %0) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits__)
  %1 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %0, i64 0, i32 0
  %control = load %Qubit*, %Qubit** %1
  %2 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %0, i64 0, i32 1
  %target = load %Qubit*, %Qubit** %2
  %3 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %3, i64 0)
  %5 = bitcast i8* %4 to %Qubit**
  store %Qubit* %control, %Qubit** %5
  %__controlQubits____inline__1 = call %Array* @__quantum__rt__array_concatenate(%Array* %__controlQubits__, %Array* %3)
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { %Array*, %Qubit* }*
  %8 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 0
  %9 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 1
  store %Array* %__controlQubits____inline__1, %Array** %8
  call void @__quantum__rt__array_reference(%Array* %__controlQubits____inline__1)
  store %Qubit* %target, %Qubit** %9
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits____inline__1)
  call void @__quantum__qis__x__ctl(%Array* %__controlQubits____inline__1, %Qubit* %target)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits____inline__1)
  call void @__quantum__rt__array_unreference(%Array* %3)
  call void @__quantum__rt__array_unreference(%Array* %__controlQubits____inline__1)
  call void @__quantum__rt__array_unreference(%Array* %__controlQubits____inline__1)
  call void @__quantum__rt__tuple_unreference(%Tuple* %6)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits__)
  ret void
}
