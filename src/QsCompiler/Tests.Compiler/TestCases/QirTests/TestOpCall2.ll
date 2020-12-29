define void @Microsoft__Quantum__Testing__QIR__CNOT__ctl(%Array* %__controlQubits__, { %Qubit*, %Qubit* }* %arg__1) {
entry:
  %0 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %arg__1, i64 0, i32 0
  %control = load %Qubit*, %Qubit** %0
  %1 = getelementptr { %Qubit*, %Qubit* }, { %Qubit*, %Qubit* }* %arg__1, i64 0, i32 1
  %target = load %Qubit*, %Qubit** %1
  %2 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %2, i64 0)
  %4 = bitcast i8* %3 to %Qubit**
  store %Qubit* %control, %Qubit** %4
  %5 = call %Array* @__quantum__rt__array_concatenate(%Array* %__controlQubits__, %Array* %2)
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { %Array*, %Qubit* }*
  %8 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 0
  %9 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 1
  store %Array* %5, %Array** %8
  call void @__quantum__rt__array_reference(%Array* %5)
  store %Qubit* %target, %Qubit** %9
  %10 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 0
  %.__controlQubits__ = load %Array*, %Array** %10
  %11 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 1
  %.q = load %Qubit*, %Qubit** %11
  call void @__quantum__qis__x__ctl(%Array* %.__controlQubits__, %Qubit* %.q)
  call void @__quantum__rt__array_unreference(%Array* %2)
  call void @__quantum__rt__array_unreference(%Array* %5)
  %12 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %7, i64 0, i32 0
  %13 = load %Array*, %Array** %12
  call void @__quantum__rt__array_unreference(%Array* %13)
  %14 = bitcast { %Array*, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %14)
  ret void
}
