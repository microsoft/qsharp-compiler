define internal void @Microsoft__Quantum__Testing__QIR__SelfAdjointOp__ctladj(%Array* %__controlQubits__, %Tuple* %__unitArg__) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 1)
  call void @Microsoft__Quantum__Testing__QIR__SelfAdjointOp__ctl(%Array* %__controlQubits__, %Tuple* %__unitArg__)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 -1)
  ret void
}
