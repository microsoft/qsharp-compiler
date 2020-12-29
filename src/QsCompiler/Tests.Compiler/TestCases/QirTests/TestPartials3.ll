define void @Lifted__PartialApplication__2__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = bitcast %Tuple* %arg-tuple to { %String*, %Qubit* }*
  %2 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %3 = load { i64, double }*, { i64, double }** %2
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %5 = bitcast %Tuple* %4 to { { i64, double }*, { %String*, %Qubit* }* }*
  %6 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %5, i64 0, i32 0
  %7 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %5, i64 0, i32 1
  store { i64, double }* %3, { i64, double }** %6
  %8 = bitcast { i64, double }* %3 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %8)
  store { %String*, %Qubit* }* %1, { %String*, %Qubit* }** %7
  %9 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %1, i64 0, i32 0
  %10 = load %String*, %String** %9
  call void @__quantum__rt__string_reference(%String* %10)
  %11 = bitcast { %String*, %Qubit* }* %1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %11)
  %12 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %5 to %Tuple*
  %13 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %14 = load %Callable*, %Callable** %13
  call void @__quantum__rt__callable_invoke(%Callable* %14, %Tuple* %12, %Tuple* %result-tuple)
  %15 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %5, i64 0, i32 0
  %16 = load { i64, double }*, { i64, double }** %15
  %17 = bitcast { i64, double }* %16 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %17)
  %18 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %5, i64 0, i32 1
  %19 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %18
  %20 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %19, i64 0, i32 0
  %21 = load %String*, %String** %20
  call void @__quantum__rt__string_unreference(%String* %21)
  %22 = bitcast { %String*, %Qubit* }* %19 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  %23 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %5 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  ret void
}
