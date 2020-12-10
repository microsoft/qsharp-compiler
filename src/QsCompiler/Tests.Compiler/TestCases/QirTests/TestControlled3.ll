define void @Microsoft__Quantum__Intrinsic__K__ctl__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Array*, { %TupleHeader, %Qubit*, i64 }* }*
  %1 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, %Qubit*, i64 }* }, { %TupleHeader, %Array*, { %TupleHeader, %Qubit*, i64 }* }* %0, i64 0, i32 1
  %2 = load %Array*, %Array** %1
  %3 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, %Qubit*, i64 }* }, { %TupleHeader, %Array*, { %TupleHeader, %Qubit*, i64 }* }* %0, i64 0, i32 2
  %4 = load { %TupleHeader, %Qubit*, i64 }*, { %TupleHeader, %Qubit*, i64 }** %3
  call void @Microsoft__Quantum__Intrinsic__K__ctl(%Array* %2, { %TupleHeader, %Qubit*, i64 }* %4)
  ret void
}
