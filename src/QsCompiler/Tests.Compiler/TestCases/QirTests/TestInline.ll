define void @Microsoft__Quantum__Testing__Tracer__TestInline__body() #0 {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__k__body(double 3.000000e-01, %Qubit* %q)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret void
}
