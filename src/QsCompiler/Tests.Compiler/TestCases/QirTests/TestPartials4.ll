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
  %28 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %27, %Tuple* %28, %Tuple* %result-tuple)
  %29 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %30 = load { i64, double }*, { i64, double }** %29
  %31 = bitcast { i64, double }* %30 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %31)
  %32 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %33 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %32
  %34 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %33, i64 0, i32 0
  %35 = load %String*, %String** %34
  call void @__quantum__rt__string_unreference(%String* %35)
  %36 = bitcast { %String*, %Qubit* }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  %37 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %37)
  %38 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %39 = load %Array*, %Array** %38
  call void @__quantum__rt__array_unreference(%Array* %39)
  %40 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %41 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %40
  %42 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %41, i64 0, i32 0
  %43 = load { i64, double }*, { i64, double }** %42
  %44 = bitcast { i64, double }* %43 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %44)
  %45 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %41, i64 0, i32 1
  %46 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %45
  %47 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %46, i64 0, i32 0
  %48 = load %String*, %String** %47
  call void @__quantum__rt__string_unreference(%String* %48)
  %49 = bitcast { %String*, %Qubit* }* %46 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %49)
  %50 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %41 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  %51 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %51)
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
  %28 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %27, %Tuple* %28, %Tuple* %result-tuple)
  %29 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %30 = load { i64, double }*, { i64, double }** %29
  %31 = bitcast { i64, double }* %30 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %31)
  %32 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %33 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %32
  %34 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %33, i64 0, i32 0
  %35 = load %String*, %String** %34
  call void @__quantum__rt__string_unreference(%String* %35)
  %36 = bitcast { %String*, %Qubit* }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  %37 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %37)
  %38 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 0
  %39 = load %Array*, %Array** %38
  call void @__quantum__rt__array_unreference(%Array* %39)
  %40 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13, i64 0, i32 1
  %41 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %40
  %42 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %41, i64 0, i32 0
  %43 = load { i64, double }*, { i64, double }** %42
  %44 = bitcast { i64, double }* %43 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %44)
  %45 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %41, i64 0, i32 1
  %46 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %45
  %47 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %46, i64 0, i32 0
  %48 = load %String*, %String** %47
  call void @__quantum__rt__string_unreference(%String* %48)
  %49 = bitcast { %String*, %Qubit* }* %46 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %49)
  %50 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %41 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  %51 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %51)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  ret void
}
