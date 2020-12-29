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
  %12 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 0
  %13 = load { i64, double }*, { i64, double }** %12
  %14 = bitcast { i64, double }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %14)
  %15 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %3, i64 0, i32 1
  %16 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %15
  %17 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %16, i64 0, i32 0
  %18 = load %String*, %String** %17
  call void @__quantum__rt__string_unreference(%String* %18)
  %19 = bitcast { %String*, %Qubit* }* %16 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
  %20 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %3 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
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
  call void @__quantum__rt__array_reference(%Array* %4)
  store { { i64, double }*, { %String*, %Qubit* }* }* %7, { { i64, double }*, { %String*, %Qubit* }* }** %15
  %16 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %17 = load { i64, double }*, { i64, double }** %16
  %18 = bitcast { i64, double }* %17 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %19 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %20 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %19
  %21 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %20, i64 0, i32 0
  %22 = load %String*, %String** %21
  call void @__quantum__rt__string_reference(%String* %22)
  %23 = bitcast { %String*, %Qubit* }* %20 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %23)
  %24 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %24)
  %25 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %26 = load %Callable*, %Callable** %25
  %27 = call %Callable* @__quantum__rt__callable_copy(%Callable* %26)
  call void @__quantum__rt__callable_make_controlled(%Callable* %27)
  call void @__quantum__rt__callable_invoke(%Callable* %27, %Tuple* %12, %Tuple* %result-tuple)
  %28 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %29 = load { i64, double }*, { i64, double }** %28
  %30 = bitcast { i64, double }* %29 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %30)
  %31 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %32 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %31
  %33 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %32, i64 0, i32 0
  %34 = load %String*, %String** %33
  call void @__quantum__rt__string_unreference(%String* %34)
  %35 = bitcast { %String*, %Qubit* }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %35)
  %36 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  %37 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %38 = load %Array*, %Array** %37
  call void @__quantum__rt__array_unreference(%Array* %38)
  %39 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %40 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %39
  %41 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %40, i64 0, i32 0
  %42 = load { i64, double }*, { i64, double }** %41
  %43 = bitcast { i64, double }* %42 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %43)
  %44 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %40, i64 0, i32 1
  %45 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %44
  %46 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %45, i64 0, i32 0
  %47 = load %String*, %String** %46
  call void @__quantum__rt__string_unreference(%String* %47)
  %48 = bitcast { %String*, %Qubit* }* %45 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %48)
  %49 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %40 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %49)
  %50 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
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
  call void @__quantum__rt__array_reference(%Array* %4)
  store { { i64, double }*, { %String*, %Qubit* }* }* %7, { { i64, double }*, { %String*, %Qubit* }* }** %15
  %16 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %17 = load { i64, double }*, { i64, double }** %16
  %18 = bitcast { i64, double }* %17 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %19 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %20 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %19
  %21 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %20, i64 0, i32 0
  %22 = load %String*, %String** %21
  call void @__quantum__rt__string_reference(%String* %22)
  %23 = bitcast { %String*, %Qubit* }* %20 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %23)
  %24 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %24)
  %25 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %26 = load %Callable*, %Callable** %25
  %27 = call %Callable* @__quantum__rt__callable_copy(%Callable* %26)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %27)
  call void @__quantum__rt__callable_make_controlled(%Callable* %27)
  call void @__quantum__rt__callable_invoke(%Callable* %27, %Tuple* %12, %Tuple* %result-tuple)
  %28 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %29 = load { i64, double }*, { i64, double }** %28
  %30 = bitcast { i64, double }* %29 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %30)
  %31 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %32 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %31
  %33 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %32, i64 0, i32 0
  %34 = load %String*, %String** %33
  call void @__quantum__rt__string_unreference(%String* %34)
  %35 = bitcast { %String*, %Qubit* }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %35)
  %36 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  %37 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %38 = load %Array*, %Array** %37
  call void @__quantum__rt__array_unreference(%Array* %38)
  %39 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %40 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %39
  %41 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %40, i64 0, i32 0
  %42 = load { i64, double }*, { i64, double }** %41
  %43 = bitcast { i64, double }* %42 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %43)
  %44 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %40, i64 0, i32 1
  %45 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %44
  %46 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %45, i64 0, i32 0
  %47 = load %String*, %String** %46
  call void @__quantum__rt__string_unreference(%String* %47)
  %48 = bitcast { %String*, %Qubit* }* %45 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %48)
  %49 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %40 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %49)
  %50 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  ret void
}
