define void @Lifted__PartialApplication__1__body__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %capture-tuple to { %TupleHeader, %Callable*, double }*
  %1 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Qubit* }*
  %2 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, double, %Qubit* }* getelementptr ({ %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* null, i32 1) to i64))
  %3 = bitcast %TupleHeader* %2 to { %TupleHeader, double, %Qubit* }*
  %4 = getelementptr { %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* %3, i64 0, i32 1
  %5 = getelementptr { %TupleHeader, %Callable*, double }, { %TupleHeader, %Callable*, double }* %0, i64 0, i32 2
  %6 = load double, double* %5
  store double %6, double* %4
  %7 = getelementptr { %TupleHeader, double, %Qubit* }, { %TupleHeader, double, %Qubit* }* %3, i64 0, i32 2
  %8 = getelementptr { %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* %1, i64 0, i32 1
  %9 = load %Qubit*, %Qubit** %8
  store %Qubit* %9, %Qubit** %7
  %10 = getelementptr { %TupleHeader, %Callable*, double }, { %TupleHeader, %Callable*, double }* %0, i64 0, i32 1
  %11 = load %Callable*, %Callable** %10
  call void @__quantum__rt__callable_invoke(%Callable* %11, %TupleHeader* %2, %TupleHeader* %result-tuple)
  %12 = bitcast { %TupleHeader, double, %Qubit* }* %3 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %12)
  ret void
}
