define void @Microsoft__Quantum__Intrinsic__K__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { %Qubit*, i64 }* }*
  %1 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %0, i64 0, i32 0
  %2 = load %Array*, %Array** %1
  %3 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %0, i64 0, i32 1
  %4 = load { %Qubit*, i64 }*, { %Qubit*, i64 }** %3
  call void @Microsoft__Quantum__Intrinsic__K__ctl(%Array* %2, { %Qubit*, i64 }* %4)
  ret void
}
