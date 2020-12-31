define void @Microsoft__Quantum__Testing__QIR__SelfAdjointIntrinsic__adj() {
entry:
  call void @__quantum__qis__selfadjointintrinsic__body()
  ret void
}

define void @Microsoft__Quantum__Testing__QIR__SelfAdjointIntrinsic__ctl(%Array* %__controlQubits__, %Tuple* %__unitArg__) {
entry:
  call void @__quantum__qis__selfadjointintrinsic__ctl(%Array* %__controlQubits__, %Tuple* %__unitArg__)
  ret void
}
