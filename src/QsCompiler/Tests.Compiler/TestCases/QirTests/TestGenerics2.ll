define void @Microsoft__Quantum__Testing__QIR__DumpMachineToFileTest__body(%String* %filePath) {
entry:
  %0 = bitcast %String* %filePath to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %0)
  ret void
}
