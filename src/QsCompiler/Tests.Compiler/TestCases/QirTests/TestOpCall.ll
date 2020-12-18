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
  %.__controlQubits__ = call %Array* @__quantum__rt__array_concatenate(%Array* %__controlQubits__, %Array* %2)
  call void @__quantum__qis__x__ctl(%Array* %.__controlQubits__, %Qubit* %target)
  call void @__quantum__rt__array_unreference(%Array* %2)
  call void @__quantum__rt__array_unreference(%Array* %.__controlQubits__)
  ret void
}
