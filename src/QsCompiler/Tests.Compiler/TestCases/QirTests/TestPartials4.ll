define void @Lifted__PartialApplication__2__adj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %2 = load { i64, double }*, { i64, double }** %1
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %4 = bitcast %Tuple* %3 to { { i64, double }*, { %String*, %Qubit* }* }*
  %5 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 0
  %6 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 1
  store { i64, double }* %2, { i64, double }** %5
  %7 = bitcast %Tuple* %arg-tuple to { %String*, %Qubit* }*
  store { %String*, %Qubit* }* %7, { %String*, %Qubit* }** %6
  %8 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %9 = load %Callable*, %Callable** %8
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %9, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %10, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %10)
  call void @__quantum__rt__callable_invoke(%Callable* %10, %Tuple* %3, %Tuple* %result-tuple)
  %11 = bitcast { i64, double }* %2 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 1)
  %12 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %7, i64 0, i32 0
  %13 = load %String*, %String** %12
  call void @__quantum__rt__string_update_reference_count(%String* %13, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %arg-tuple, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %arg-tuple, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %10, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i64 -1)
  ret void
}

define void @Lifted__PartialApplication__2__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { %String*, %Qubit* }* }*
  %1 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %0, i64 0, i32 0
  %2 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %0, i64 0, i32 1
  %3 = load %Array*, %Array** %1
  %4 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %2
  %5 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %6 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %5, i64 0, i32 1
  %7 = load { i64, double }*, { i64, double }** %6
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { { i64, double }*, { %String*, %Qubit* }* }*
  %10 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  store { i64, double }* %7, { i64, double }** %10
  store { %String*, %Qubit* }* %4, { %String*, %Qubit* }** %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %14 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  store %Array* %3, %Array** %14
  store { { i64, double }*, { %String*, %Qubit* }* }* %9, { { i64, double }*, { %String*, %Qubit* }* }** %15
  %16 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %5, i64 0, i32 0
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %18, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  %19 = bitcast { i64, double }* %7 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  %20 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %4, i64 0, i32 0
  %21 = load %String*, %String** %20
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 1)
  %22 = bitcast { %String*, %Qubit* }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %18, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %18, i64 -1)
  ret void
}

define void @Lifted__PartialApplication__2__ctladj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { %String*, %Qubit* }* }*
  %1 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %0, i64 0, i32 0
  %2 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %0, i64 0, i32 1
  %3 = load %Array*, %Array** %1
  %4 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %2
  %5 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %6 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %5, i64 0, i32 1
  %7 = load { i64, double }*, { i64, double }** %6
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { { i64, double }*, { %String*, %Qubit* }* }*
  %10 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  store { i64, double }* %7, { i64, double }** %10
  store { %String*, %Qubit* }* %4, { %String*, %Qubit* }** %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %14 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  store %Array* %3, %Array** %14
  store { { i64, double }*, { %String*, %Qubit* }* }* %9, { { i64, double }*, { %String*, %Qubit* }* }** %15
  %16 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %5, i64 0, i32 0
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %18, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %18)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  %19 = bitcast { i64, double }* %7 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  %20 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %4, i64 0, i32 0
  %21 = load %String*, %String** %20
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 1)
  %22 = bitcast { %String*, %Qubit* }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %18, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %18, i64 -1)
  ret void
}

define void @MemoryManagement__2__RefCount(%Tuple* %capture-tuple, i64 %count-change) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %2 = load %Callable*, %Callable** %1
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %2, i64 %count-change)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %2, i64 %count-change)
  %3 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %4 = load { i64, double }*, { i64, double }** %3
  %5 = bitcast { i64, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 %count-change)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %capture-tuple, i64 %count-change)
  ret void
}

define void @MemoryManagement__2__AccessCount(%Tuple* %capture-tuple, i64 %count-change) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %2 = load %Callable*, %Callable** %1
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %2, i64 %count-change)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %2, i64 %count-change)
  %3 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %4 = load { i64, double }*, { i64, double }** %3
  %5 = bitcast { i64, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 %count-change)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %capture-tuple, i64 %count-change)
  ret void
}
