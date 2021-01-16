define void @Lifted__PartialApplication__2__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %2 = load { i64, double }*, { i64, double }** %1
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %4 = bitcast %Tuple* %3 to { { i64, double }*, { %String*, %Qubit* }* }*
  %5 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 0
  %6 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 1
  store { i64, double }* %2, { i64, double }** %5
  %7 = bitcast { i64, double }* %2 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 1)
  %8 = bitcast %Tuple* %arg-tuple to { %String*, %Qubit* }*
  store { %String*, %Qubit* }* %8, { %String*, %Qubit* }** %6
  %9 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %8, i64 0, i32 0
  %10 = load %String*, %String** %9
  call void @__quantum__rt__string_update_reference_count(%String* %10, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %arg-tuple, i64 1)
  %11 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %12 = load %Callable*, %Callable** %11
  call void @__quantum__rt__callable_invoke(%Callable* %12, %Tuple* %3, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %10, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %arg-tuple, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  ret void
}
