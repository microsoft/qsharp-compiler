define void @Lifted__PartialApplication__6__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, %Callable* }*
  %1 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %0, i32 0, i32 1
  %2 = load %Callable*, %Callable** %1, align 8
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %4 = bitcast %Tuple* %3 to { %Callable*, { %Array* }* }*
  %5 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %4, i32 0, i32 0
  %6 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %4, i32 0, i32 1
  store %Callable* %2, %Callable** %5, align 8
  %7 = bitcast %Tuple* %arg-tuple to { %Array* }*
  store { %Array* }* %7, { %Array* }** %6, align 8
  %8 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %0, i32 0, i32 0
  %9 = load %Callable*, %Callable** %8, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %9, %Tuple* %3, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  ret void
}

define void @Lifted__PartialApplication__6__adj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, %Callable* }*
  %1 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %0, i32 0, i32 1
  %2 = load %Callable*, %Callable** %1, align 8
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %4 = bitcast %Tuple* %3 to { %Callable*, { %Array* }* }*
  %5 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %4, i32 0, i32 0
  %6 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %4, i32 0, i32 1
  store %Callable* %2, %Callable** %5, align 8
  %7 = bitcast %Tuple* %arg-tuple to { %Array* }*
  store { %Array* }* %7, { %Array* }** %6, align 8
  %8 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %0, i32 0, i32 0
  %9 = load %Callable*, %Callable** %8, align 8
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %9, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %10)
  call void @__quantum__rt__callable_invoke(%Callable* %10, %Tuple* %3, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i32 -1)
  ret void
}

define void @Lifted__PartialApplication__6__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { %Array* }* }*
  %1 = getelementptr inbounds { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i32 0, i32 0
  %2 = getelementptr inbounds { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i32 0, i32 1
  %3 = load %Array*, %Array** %1, align 8
  %4 = load { %Array* }*, { %Array* }** %2, align 8
  %5 = bitcast %Tuple* %capture-tuple to { %Callable*, %Callable* }*
  %6 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %5, i32 0, i32 1
  %7 = load %Callable*, %Callable** %6, align 8
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { %Callable*, { %Array* }* }*
  %10 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %9, i32 0, i32 0
  %11 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %9, i32 0, i32 1
  store %Callable* %7, %Callable** %10, align 8
  store { %Array* }* %4, { %Array* }** %11, align 8
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { %Callable*, { %Array* }* }* }*
  %14 = getelementptr inbounds { %Array*, { %Callable*, { %Array* }* }* }, { %Array*, { %Callable*, { %Array* }* }* }* %13, i32 0, i32 0
  %15 = getelementptr inbounds { %Array*, { %Callable*, { %Array* }* }* }, { %Array*, { %Callable*, { %Array* }* }* }* %13, i32 0, i32 1
  store %Array* %3, %Array** %14, align 8
  store { %Callable*, { %Array* }* }* %9, { %Callable*, { %Array* }* }** %15, align 8
  %16 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %5, i32 0, i32 0
  %17 = load %Callable*, %Callable** %16, align 8
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %18, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %18, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %18, i32 -1)
  ret void
}

define void @Lifted__PartialApplication__6__ctladj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { %Array* }* }*
  %1 = getelementptr inbounds { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i32 0, i32 0
  %2 = getelementptr inbounds { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i32 0, i32 1
  %3 = load %Array*, %Array** %1, align 8
  %4 = load { %Array* }*, { %Array* }** %2, align 8
  %5 = bitcast %Tuple* %capture-tuple to { %Callable*, %Callable* }*
  %6 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %5, i32 0, i32 1
  %7 = load %Callable*, %Callable** %6, align 8
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { %Callable*, { %Array* }* }*
  %10 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %9, i32 0, i32 0
  %11 = getelementptr inbounds { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %9, i32 0, i32 1
  store %Callable* %7, %Callable** %10, align 8
  store { %Array* }* %4, { %Array* }** %11, align 8
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { %Callable*, { %Array* }* }* }*
  %14 = getelementptr inbounds { %Array*, { %Callable*, { %Array* }* }* }, { %Array*, { %Callable*, { %Array* }* }* }* %13, i32 0, i32 0
  %15 = getelementptr inbounds { %Array*, { %Callable*, { %Array* }* }* }, { %Array*, { %Callable*, { %Array* }* }* }* %13, i32 0, i32 1
  store %Array* %3, %Array** %14, align 8
  store { %Callable*, { %Array* }* }* %9, { %Callable*, { %Array* }* }** %15, align 8
  %16 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %5, i32 0, i32 0
  %17 = load %Callable*, %Callable** %16, align 8
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %18, i32 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %18)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %18, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %18, i32 -1)
  ret void
}
