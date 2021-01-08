define void @Microsoft__Quantum__Testing__QIR__CNOT__ctl(%Array* %__controlQubits__, { %Qubit*, %Qubit* }* %arg__1) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits__)
  %0 = bitcast { %Qubit*, %Qubit* }* %arg__1 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %0)
  %1 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %arg__1, i64 0, i32 0
  %control = load %Qubit*, %Qubit** %1
  %2 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %arg__1, i64 0, i32 1
  %target = load %Qubit*, %Qubit** %2
  %3 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %3, i64 0)
  %5 = bitcast i8* %4 to %Qubit**
  store %Qubit* %control, %Qubit** %5
  %6 = call %Array* @__quantum__rt__array_concatenate(%Array* %__controlQubits__, %Array* %3)
  %7 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %8 = bitcast %Tuple* %7 to { %Array*, %Qubit* }*
  %9 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %8, i64 0, i32 0
  %10 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %8, i64 0, i32 1
  store %Array* %6, %Array** %9
  call void @__quantum__rt__array_reference(%Array* %6)
  store %Qubit* %target, %Qubit** %10
  %11 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %8, i64 0, i32 0
  %__controlQubits____inline__1 = load %Array*, %Array** %11
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits____inline__1)
  %12 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %8, i64 0, i32 1
  %q__inline__1 = load %Qubit*, %Qubit** %12
  call void @__quantum__qis__x__ctl(%Array* %__controlQubits____inline__1, %Qubit* %q__inline__1)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits____inline__1)
  call void @__quantum__rt__array_unreference(%Array* %3)
  call void @__quantum__rt__array_unreference(%Array* %6)
  %13 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %8, i64 0, i32 0
  %14 = load %Array*, %Array** %13
  call void @__quantum__rt__array_unreference(%Array* %14)
  call void @__quantum__rt__tuple_unreference(%Tuple* %7)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits__)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %0)
  ret void
}