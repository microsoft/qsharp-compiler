define void @Microsoft__Quantum__Testing__QIR__SelfAdjointOp__ctl(%Array* %__controlQubits__, %Tuple* %__unitArg__) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits__)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits__)
  ret void
}
