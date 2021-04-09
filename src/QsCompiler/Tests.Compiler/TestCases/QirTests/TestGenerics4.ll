define void @Microsoft__Quantum__Testing__QIR__DumpRegisterToFileTest__body(%String* %filePath) {
entry:
  %q2 = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
  call void @__quantum__rt__array_update_alias_count(%Array* %q2, i32 1)
  %0 = bitcast %String* %filePath to i8*
  call void @__quantum__qis__dumpregister__body(i8* %0, %Array* %q2)
  call void @__quantum__rt__array_update_alias_count(%Array* %q2, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %q2)
  ret void
}
