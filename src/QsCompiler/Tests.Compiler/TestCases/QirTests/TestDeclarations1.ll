define void @Microsoft__Quantum__Testing__QIR__SelfAdjointIntrinsic__adj() {
entry:
  call void @__quantum__qis__selfadjointintrinsic__body()
  ret void
}

define void @Microsoft__Quantum__Testing__QIR__SelfAdjointIntrinsic__ctl(%Array* %__controlQubits__, %Tuple* %__unitArg__) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i64 1)
  call void @__quantum__qis__selfadjointintrinsic__ctl(%Array* %__controlQubits__, %Tuple* %__unitArg__)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i64 -1)
  ret void
}
