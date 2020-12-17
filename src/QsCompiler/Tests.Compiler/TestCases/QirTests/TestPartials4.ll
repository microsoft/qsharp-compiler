define void @Lifted__PartialApplication__1__adj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, double }*
  %1 = bitcast %Tuple* %arg-tuple to { %Qubit* }*
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %Qubit* }* getelementptr ({ double, %Qubit* }, { double, %Qubit* }* null, i32 1) to i64))
  %3 = bitcast %Tuple* %2 to { double, %Qubit* }*
  %4 = getelementptr { double, %Qubit* }, { double, %Qubit* }* %3, i64 0, i32 0
  %5 = getelementptr { %Callable*, double }, { %Callable*, double }* %0, i64 0, i32 1
  %6 = load double, double* %5
  store double %6, double* %4
  %7 = getelementptr { double, %Qubit* }, { double, %Qubit* }* %3, i64 0, i32 1
  %8 = getelementptr { %Qubit* }, { %Qubit* }* %1, i64 0, i32 0
  %9 = load %Qubit*, %Qubit** %8
  store %Qubit* %9, %Qubit** %7
  %10 = getelementptr { %Callable*, double }, { %Callable*, double }* %0, i64 0, i32 0
  %11 = load %Callable*, %Callable** %10
  %12 = call %Callable* @__quantum__rt__callable_copy(%Callable* %11)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %12)
  call void @__quantum__rt__callable_invoke(%Callable* %12, %Tuple* %2, %Tuple* %result-tuple)
  %13 = bitcast { double, %Qubit* }* %3 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %13)
  call void @__quantum__rt__callable_unreference(%Callable* %12)
  ret void
}
