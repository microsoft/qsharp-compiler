define void @Microsoft__Quantum__Testing__QIR__Baz__body(%Qubit* %q) {
entry:
  call void @__quantum__qis__h__body(%Qubit* %q)
  call void @__quantum__qis__t__body(%Qubit* %q)
  ret void
}
