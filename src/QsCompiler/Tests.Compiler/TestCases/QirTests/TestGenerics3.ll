define void @Microsoft__Quantum__Testing__QIR__DumpRegisterTest__body() #0 {
entry:
  %q2 = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
  call void @__quantum__rt__array_update_alias_count(%Array* %q2, i32 1)
  call void @__quantum__qis__dumpregister__body(i8* null, %Array* %q2)
  call void @__quantum__rt__array_update_alias_count(%Array* %q2, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %q2)
  ret void
}
