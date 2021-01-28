define void @Microsoft__Quantum__Intrinsic__Rz__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { double, %Qubit* }*
  %1 = getelementptr { double, %Qubit* }, { double, %Qubit* }* %0, i64 0, i32 0
  %2 = getelementptr { double, %Qubit* }, { double, %Qubit* }* %0, i64 0, i32 1
  %3 = load double, double* %1
  %4 = load %Qubit*, %Qubit** %2
  call void @Microsoft__Quantum__Intrinsic__Rz__body(double %3, %Qubit* %4)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__adj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { double, %Qubit* }*
  %1 = getelementptr { double, %Qubit* }, { double, %Qubit* }* %0, i64 0, i32 0
  %2 = getelementptr { double, %Qubit* }, { double, %Qubit* }* %0, i64 0, i32 1
  %3 = load double, double* %1
  %4 = load %Qubit*, %Qubit** %2
  call void @Microsoft__Quantum__Intrinsic__Rz__adj(double %3, %Qubit* %4)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { double, %Qubit* }* }*
  %1 = getelementptr { %Array*, { double, %Qubit* }* }, { %Array*, { double, %Qubit* }* }* %0, i64 0, i32 0
  %2 = getelementptr { %Array*, { double, %Qubit* }* }, { %Array*, { double, %Qubit* }* }* %0, i64 0, i32 1
  %3 = load %Array*, %Array** %1
  %4 = load { double, %Qubit* }*, { double, %Qubit* }** %2
  call void @Microsoft__Quantum__Intrinsic__Rz__ctl(%Array* %3, { double, %Qubit* }* %4)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__ctladj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { double, %Qubit* }* }*
  %1 = getelementptr { %Array*, { double, %Qubit* }* }, { %Array*, { double, %Qubit* }* }* %0, i64 0, i32 0
  %2 = getelementptr { %Array*, { double, %Qubit* }* }, { %Array*, { double, %Qubit* }* }* %0, i64 0, i32 1
  %3 = load %Array*, %Array** %1
  %4 = load { double, %Qubit* }*, { double, %Qubit* }** %2
  call void @Microsoft__Quantum__Intrinsic__Rz__ctladj(%Array* %3, { double, %Qubit* }* %4)
  ret void
}
