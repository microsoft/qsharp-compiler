define void @Microsoft__Quantum__Intrinsic__Rz__body__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, double, %Qubit* }*
  %1 = getelementptr { %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* %0, i64 0, i32 1
  %2 = load double, double* %1
  %3 = getelementptr { %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* %0, i64 0, i32 2
  %4 = load %Qubit*, %Qubit** %3
  call void @Microsoft__Quantum__Intrinsic__Rz__body(double %2, %Qubit* %4)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__adj__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, double, %Qubit* }*
  %1 = getelementptr { %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* %0, i64 0, i32 1
  %2 = load double, double* %1
  %3 = getelementptr { %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* %0, i64 0, i32 2
  %4 = load %Qubit*, %Qubit** %3
  call void @Microsoft__Quantum__Intrinsic__Rz__adj(double %2, %Qubit* %4)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__ctl__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }*
  %1 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }* %0, i64 0, i32 1
  %2 = load %Array*, %Array** %1
  %3 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }* %0, i64 0, i32 2
  %4 = load { %TupleHeader, double, %Qubit* }*, { %TupleHeader, double, %Qubit* }** %3
  call void @Microsoft__Quantum__Intrinsic__Rz__ctl(%Array* %2, { %TupleHeader, double, %Qubit* }* %4)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__ctladj__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }*
  %1 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }* %0, i64 0, i32 1
  %2 = load %Array*, %Array** %1
  %3 = getelementptr { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }, { %TupleHeader, %Array*, { %TupleHeader, double, %Qubit* }* }* %0, i64 0, i32 2
  %4 = load { %TupleHeader, double, %Qubit* }*, { %TupleHeader, double, %Qubit* }** %3
  call void @Microsoft__Quantum__Intrinsic__Rz__ctladj(%Array* %2, { %TupleHeader, double, %Qubit* }* %4)
  ret void
}
