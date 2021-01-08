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
  %7 = bitcast { i64, double }* %2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %7)
  %8 = bitcast %Tuple* %arg-tuple to { %String*, %Qubit* }*
  store { %String*, %Qubit* }* %8, { %String*, %Qubit* }** %6
  %9 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %8, i64 0, i32 0
  %10 = load %String*, %String** %9
  call void @__quantum__rt__string_reference(%String* %10)
  call void @__quantum__rt__tuple_reference(%Tuple* %arg-tuple)
  %11 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %12 = load %Callable*, %Callable** %11
  %13 = call %Callable* @__quantum__rt__callable_copy(%Callable* %12, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %13)
  call void @__quantum__rt__callable_invoke(%Callable* %13, %Tuple* %3, %Tuple* %result-tuple)
  %14 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 0
  %15 = load { i64, double }*, { i64, double }** %14
  %16 = bitcast { i64, double }* %15 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %16)
  %17 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 1
  %18 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %17
  %19 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %18, i64 0, i32 0
  %20 = load %String*, %String** %19
  call void @__quantum__rt__string_unreference(%String* %20)
  %21 = bitcast { %String*, %Qubit* }* %18 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  call void @__quantum__rt__tuple_unreference(%Tuple* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %13)
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
  %12 = bitcast { i64, double }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %12)
  store { %String*, %Qubit* }* %4, { %String*, %Qubit* }** %11
  %13 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %4, i64 0, i32 0
  %14 = load %String*, %String** %13
  call void @__quantum__rt__string_reference(%String* %14)
  %15 = bitcast { %String*, %Qubit* }* %4 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %15)
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %17 = bitcast %Tuple* %16 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %18 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %19 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  store %Array* %3, %Array** %18
  call void @__quantum__rt__array_reference(%Array* %3)
  store { { i64, double }*, { %String*, %Qubit* }* }* %9, { { i64, double }*, { %String*, %Qubit* }* }** %19
  %20 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %21 = load { i64, double }*, { i64, double }** %20
  %22 = bitcast { i64, double }* %21 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %22)
  %23 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  %24 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %23
  %25 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %24, i64 0, i32 0
  %26 = load %String*, %String** %25
  call void @__quantum__rt__string_reference(%String* %26)
  %27 = bitcast { %String*, %Qubit* }* %24 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %27)
  call void @__quantum__rt__tuple_reference(%Tuple* %8)
  %28 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %5, i64 0, i32 0
  %29 = load %Callable*, %Callable** %28
  %30 = call %Callable* @__quantum__rt__callable_copy(%Callable* %29, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %30)
  call void @__quantum__rt__callable_invoke(%Callable* %30, %Tuple* %16, %Tuple* %result-tuple)
  %31 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %32 = load { i64, double }*, { i64, double }** %31
  %33 = bitcast { i64, double }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %33)
  %34 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  %35 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %34
  %36 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %35, i64 0, i32 0
  %37 = load %String*, %String** %36
  call void @__quantum__rt__string_unreference(%String* %37)
  %38 = bitcast { %String*, %Qubit* }* %35 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %38)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  %39 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %40 = load %Array*, %Array** %39
  call void @__quantum__rt__array_unreference(%Array* %40)
  %41 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  %42 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %41
  %43 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %42, i64 0, i32 0
  %44 = load { i64, double }*, { i64, double }** %43
  %45 = bitcast { i64, double }* %44 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %45)
  %46 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %42, i64 0, i32 1
  %47 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %46
  %48 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %47, i64 0, i32 0
  %49 = load %String*, %String** %48
  call void @__quantum__rt__string_unreference(%String* %49)
  %50 = bitcast { %String*, %Qubit* }* %47 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  %51 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %42 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %51)
  call void @__quantum__rt__tuple_unreference(%Tuple* %16)
  call void @__quantum__rt__callable_unreference(%Callable* %30)
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
  %12 = bitcast { i64, double }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %12)
  store { %String*, %Qubit* }* %4, { %String*, %Qubit* }** %11
  %13 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %4, i64 0, i32 0
  %14 = load %String*, %String** %13
  call void @__quantum__rt__string_reference(%String* %14)
  %15 = bitcast { %String*, %Qubit* }* %4 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %15)
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %17 = bitcast %Tuple* %16 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %18 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %19 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  store %Array* %3, %Array** %18
  call void @__quantum__rt__array_reference(%Array* %3)
  store { { i64, double }*, { %String*, %Qubit* }* }* %9, { { i64, double }*, { %String*, %Qubit* }* }** %19
  %20 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %21 = load { i64, double }*, { i64, double }** %20
  %22 = bitcast { i64, double }* %21 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %22)
  %23 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  %24 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %23
  %25 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %24, i64 0, i32 0
  %26 = load %String*, %String** %25
  call void @__quantum__rt__string_reference(%String* %26)
  %27 = bitcast { %String*, %Qubit* }* %24 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %27)
  call void @__quantum__rt__tuple_reference(%Tuple* %8)
  %28 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %5, i64 0, i32 0
  %29 = load %Callable*, %Callable** %28
  %30 = call %Callable* @__quantum__rt__callable_copy(%Callable* %29, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %30)
  call void @__quantum__rt__callable_make_controlled(%Callable* %30)
  call void @__quantum__rt__callable_invoke(%Callable* %30, %Tuple* %16, %Tuple* %result-tuple)
  %31 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %32 = load { i64, double }*, { i64, double }** %31
  %33 = bitcast { i64, double }* %32 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %33)
  %34 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  %35 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %34
  %36 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %35, i64 0, i32 0
  %37 = load %String*, %String** %36
  call void @__quantum__rt__string_unreference(%String* %37)
  %38 = bitcast { %String*, %Qubit* }* %35 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %38)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  %39 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %40 = load %Array*, %Array** %39
  call void @__quantum__rt__array_unreference(%Array* %40)
  %41 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  %42 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %41
  %43 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %42, i64 0, i32 0
  %44 = load { i64, double }*, { i64, double }** %43
  %45 = bitcast { i64, double }* %44 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %45)
  %46 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %42, i64 0, i32 1
  %47 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %46
  %48 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %47, i64 0, i32 0
  %49 = load %String*, %String** %48
  call void @__quantum__rt__string_unreference(%String* %49)
  %50 = bitcast { %String*, %Qubit* }* %47 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %50)
  %51 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %42 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %51)
  call void @__quantum__rt__tuple_unreference(%Tuple* %16)
  call void @__quantum__rt__callable_unreference(%Callable* %30)
  ret void
}
