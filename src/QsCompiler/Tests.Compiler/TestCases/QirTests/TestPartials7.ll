define internal void @Lifted__PartialApplication__7__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, %Callable*, %Qubit* }*
  %1 = getelementptr inbounds { %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* %0, i32 0, i32 1
  %2 = load %Callable*, %Callable** %1, align 8
  %3 = getelementptr inbounds { %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* %0, i32 0, i32 2
  %4 = load %Qubit*, %Qubit** %3, align 8
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %6 = bitcast %Tuple* %5 to { %Callable*, %Qubit*, %Tuple* }*
  %7 = getelementptr inbounds { %Callable*, %Qubit*, %Tuple* }, { %Callable*, %Qubit*, %Tuple* }* %6, i32 0, i32 0
  %8 = getelementptr inbounds { %Callable*, %Qubit*, %Tuple* }, { %Callable*, %Qubit*, %Tuple* }* %6, i32 0, i32 1
  %9 = getelementptr inbounds { %Callable*, %Qubit*, %Tuple* }, { %Callable*, %Qubit*, %Tuple* }* %6, i32 0, i32 2
  store %Callable* %2, %Callable** %7, align 8
  store %Qubit* %4, %Qubit** %8, align 8
  store %Tuple* null, %Tuple** %9, align 8
  %10 = getelementptr inbounds { %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* %0, i32 0, i32 0
  %11 = load %Callable*, %Callable** %10, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %11, %Tuple* %5, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  ret void
}
