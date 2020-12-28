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
  %8 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %9 = load %Callable*, %Callable** %8
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %9)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %10)
  %11 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %3 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %10, %Tuple* %11, %Tuple* %result-tuple)
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
  call void @__quantum__rt__callable_unreference(%Callable* %10)
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
  %19 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %19, %Tuple* %result-tuple)
  %20 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  %21 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %22 = load { i64, double }*, { i64, double }** %21
  %23 = bitcast { i64, double }* %22 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  %24 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %25 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %24
  %26 = bitcast { %String*, %Qubit* }* %25 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %26)
  %27 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %25, i64 0, i32 0
  %28 = load %String*, %String** %27
  call void @__quantum__rt__string_unreference(%String* %28)
  %29 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %29)
  %30 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %31 = load %Array*, %Array** %30
  call void @__quantum__rt__array_unreference(%Array* %31)
  %32 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %33 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %32
  %34 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  %35 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %33, i64 0, i32 0
  %36 = load { i64, double }*, { i64, double }** %35
  %37 = bitcast { i64, double }* %36 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %37)
  %38 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %33, i64 0, i32 1
  %39 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %38
  %40 = bitcast { %String*, %Qubit* }* %39 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %40)
  %41 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %39, i64 0, i32 0
  %42 = load %String*, %String** %41
  call void @__quantum__rt__string_unreference(%String* %42)
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
  %19 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %19, %Tuple* %result-tuple)
  %20 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  %21 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %22 = load { i64, double }*, { i64, double }** %21
  %23 = bitcast { i64, double }* %22 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  %24 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %25 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %24
  %26 = bitcast { %String*, %Qubit* }* %25 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %26)
  %27 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %25, i64 0, i32 0
  %28 = load %String*, %String** %27
  call void @__quantum__rt__string_unreference(%String* %28)
  %29 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %29)
  %30 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %31 = load %Array*, %Array** %30
  call void @__quantum__rt__array_unreference(%Array* %31)
  %32 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %33 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %32
  %34 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  %35 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %33, i64 0, i32 0
  %36 = load { i64, double }*, { i64, double }** %35
  %37 = bitcast { i64, double }* %36 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %37)
  %38 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %33, i64 0, i32 1
  %39 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %38
  %40 = bitcast { %String*, %Qubit* }* %39 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %40)
  %41 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %39, i64 0, i32 0
  %42 = load %String*, %String** %41
  call void @__quantum__rt__string_unreference(%String* %42)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  ret void
}
