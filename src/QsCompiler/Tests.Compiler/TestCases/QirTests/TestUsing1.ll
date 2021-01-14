define void @Microsoft__Quantum__Testing__QIR__TestUsing__body() #0 {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @Microsoft__Quantum__Testing__QIR__ArbitraryAllocation__body(i64 3, %Qubit* %q)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret void
}
