define void @Microsoft__Quantum__Intrinsic_____GUID___Message__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %String* }*
  %1 = getelementptr inbounds { %String* }, { %String* }* %0, i32 0, i32 0
  %2 = load %String*, %String** %1, align 8
  call void @__quantum__rt__message(%String* %2)
  ret void
}
