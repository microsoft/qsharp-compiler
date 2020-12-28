define void @Lifted__PartialApplication__2__adj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
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
  %8 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %3 to %Tuple*
  %9 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %10 = load %Callable*, %Callable** %9
  %11 = call %Callable* @__quantum__rt__callable_copy(%Callable* %10)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %11)
  call void @__quantum__rt__callable_invoke(%Callable* %11, %Tuple* %8, %Tuple* %result-tuple)
  %12 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %3 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %12)
  %13 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 0
  %14 = load { i64, double }*, { i64, double }** %13
  %15 = bitcast { i64, double }* %14 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %15)
  %16 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 1
  %17 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %16
  %18 = bitcast { %String*, %Qubit* }* %17 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  %19 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %17, i64 0, i32 0
  %20 = load %String*, %String** %19
  call void @__quantum__rt__string_unreference(%String* %20)
  call void @__quantum__rt__callable_unreference(%Callable* %11)
  ret void
}

define void @Lifted__PartialApplication__2__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, { %String*, %Qubit* }* }*
  %2 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %1, i64 0, i32 1
  %4 = load %Array*, %Array** %2
  %5 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %3
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { { i64, double }*, { %String*, %Qubit* }* }*
  %8 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %9 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %10 = load { i64, double }*, { i64, double }** %9
  store { i64, double }* %10, { i64, double }** %8
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  store { %String*, %Qubit* }* %5, { %String*, %Qubit* }** %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %14 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  store %Array* %4, %Array** %14
  store { { i64, double }*, { %String*, %Qubit* }* }* %7, { { i64, double }*, { %String*, %Qubit* }* }** %15
  %16 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  %19 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
  %20 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %21 = load { i64, double }*, { i64, double }** %20
  %22 = bitcast { i64, double }* %21 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  %23 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %24 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %23
  %25 = bitcast { %String*, %Qubit* }* %24 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %25)
  %26 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %24, i64 0, i32 0
  %27 = load %String*, %String** %26
  call void @__quantum__rt__string_unreference(%String* %27)
  %28 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %28)
  %29 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %30 = load %Array*, %Array** %29
  call void @__quantum__rt__array_unreference(%Array* %30)
  %31 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %32 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %31
  %33 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %33)
  %34 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %32, i64 0, i32 0
  %35 = load { i64, double }*, { i64, double }** %34
  %36 = bitcast { i64, double }* %35 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  %37 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %32, i64 0, i32 1
  %38 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %37
  %39 = bitcast { %String*, %Qubit* }* %38 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %39)
  %40 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %38, i64 0, i32 0
  %41 = load %String*, %String** %40
  call void @__quantum__rt__string_unreference(%String* %41)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  ret void
}

define void @Lifted__PartialApplication__2__ctladj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, { %String*, %Qubit* }* }*
  %2 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, { %String*, %Qubit* }* }, { %Array*, { %String*, %Qubit* }* }* %1, i64 0, i32 1
  %4 = load %Array*, %Array** %2
  %5 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %3
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { { i64, double }*, { %String*, %Qubit* }* }*
  %8 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %9 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %10 = load { i64, double }*, { i64, double }** %9
  store { i64, double }* %10, { i64, double }** %8
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  store { %String*, %Qubit* }* %5, { %String*, %Qubit* }** %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %14 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  store %Array* %4, %Array** %14
  store { { i64, double }*, { %String*, %Qubit* }* }* %7, { { i64, double }*, { %String*, %Qubit* }* }** %15
  %16 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %18)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  %19 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
  %20 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %21 = load { i64, double }*, { i64, double }** %20
  %22 = bitcast { i64, double }* %21 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  %23 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %24 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %23
  %25 = bitcast { %String*, %Qubit* }* %24 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %25)
  %26 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %24, i64 0, i32 0
  %27 = load %String*, %String** %26
  call void @__quantum__rt__string_unreference(%String* %27)
  %28 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %28)
  %29 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %30 = load %Array*, %Array** %29
  call void @__quantum__rt__array_unreference(%Array* %30)
  %31 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %32 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %31
  %33 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %33)
  %34 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %32, i64 0, i32 0
  %35 = load { i64, double }*, { i64, double }** %34
  %36 = bitcast { i64, double }* %35 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  %37 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %32, i64 0, i32 1
  %38 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %37
  %39 = bitcast { %String*, %Qubit* }* %38 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %39)
  %40 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %38, i64 0, i32 0
  %41 = load %String*, %String** %40
  call void @__quantum__rt__string_unreference(%String* %41)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  ret void
}
