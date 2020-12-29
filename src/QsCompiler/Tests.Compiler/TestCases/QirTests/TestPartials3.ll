define void @Lifted__PartialApplication__2__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = bitcast %Tuple* %arg-tuple to { %String*, %Qubit* }*
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %3 = bitcast %Tuple* %2 to { { i64, double }*, { %String*, %Qubit* }* }*
  %4 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 0
  %5 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %6 = load { i64, double }*, { i64, double }** %5
  store { i64, double }* %6, { i64, double }** %4
  %7 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 1
  store { %String*, %Qubit* }* %1, { %String*, %Qubit* }** %7
  %8 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %9 = load %Callable*, %Callable** %8
  %10 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %3 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %9, %Tuple* %10, %Tuple* %result-tuple)
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 0
  %12 = load { i64, double }*, { i64, double }** %11
  %13 = bitcast { i64, double }* %12 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %13)
  %14 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 1
  %15 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %14
  %16 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %15, i64 0, i32 0
  %17 = load %String*, %String** %16
  call void @__quantum__rt__string_unreference(%String* %17)
  %18 = bitcast { %String*, %Qubit* }* %15 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  %19 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %3 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
  ret void
}
