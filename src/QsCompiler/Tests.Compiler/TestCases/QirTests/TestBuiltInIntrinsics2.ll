define internal void @Microsoft__Quantum__Intrinsic__Message__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %String* }*
  %1 = getelementptr inbounds { %String* }, { %String* }* %0, i32 0, i32 0
  %2 = load %String*, %String** %1, align 8
  call void @__quantum__rt__message(%String* %2)
  ret void
}

define internal void @Microsoft__Quantum__Diagnostics__DumpMachine__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { i8* }*
  %1 = getelementptr inbounds { i8* }, { i8* }* %0, i32 0, i32 0
  %2 = load i8*, i8** %1, align 8
  call void @__quantum__qis__dumpmachine__body(i8* %2)
  ret void
}
