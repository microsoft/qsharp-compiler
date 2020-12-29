define void @Lifted__PartialApplication__2__adj__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
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
  %15 = call %Callable* @__quantum__rt__callable_copy(%Callable* %14)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %15)
  call void @__quantum__rt__callable_invoke(%Callable* %15, %Tuple* %12, %Tuple* %result-tuple)
  %16 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %5, i64 0, i32 0
  %17 = load { i64, double }*, { i64, double }** %16
  %18 = bitcast { i64, double }* %17 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  %19 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %5, i64 0, i32 1
  %20 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %19
  %21 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %20, i64 0, i32 0
  %22 = load %String*, %String** %21
  call void @__quantum__rt__string_unreference(%String* %22)
  %23 = bitcast { %String*, %Qubit* }* %20 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  %24 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %5 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %24)
  call void @__quantum__rt__callable_unreference(%Callable* %15)
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
  %6 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %7 = load { i64, double }*, { i64, double }** %6
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { { i64, double }*, { %String*, %Qubit* }* }*
  %10 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  store { i64, double }* %7, { i64, double }** %10
  %12 = bitcast { i64, double }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %12)
  store { %String*, %Qubit* }* %5, { %String*, %Qubit* }** %11
  %13 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %5, i64 0, i32 0
  %14 = load %String*, %String** %13
  call void @__quantum__rt__string_reference(%String* %14)
  %15 = bitcast { %String*, %Qubit* }* %5 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %15)
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %17 = bitcast %Tuple* %16 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %18 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %19 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  store %Array* %4, %Array** %18
  call void @__quantum__rt__array_reference(%Array* %4)
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
  %28 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %28)
  %29 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %30 = load %Callable*, %Callable** %29
  %31 = call %Callable* @__quantum__rt__callable_copy(%Callable* %30)
  call void @__quantum__rt__callable_make_controlled(%Callable* %31)
  call void @__quantum__rt__callable_invoke(%Callable* %31, %Tuple* %16, %Tuple* %result-tuple)
  %32 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %33 = load { i64, double }*, { i64, double }** %32
  %34 = bitcast { i64, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  %35 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  %36 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %35
  %37 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %36, i64 0, i32 0
  %38 = load %String*, %String** %37
  call void @__quantum__rt__string_unreference(%String* %38)
  %39 = bitcast { %String*, %Qubit* }* %36 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %39)
  %40 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %40)
  %41 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %42 = load %Array*, %Array** %41
  call void @__quantum__rt__array_unreference(%Array* %42)
  %43 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  %44 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %43
  %45 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %44, i64 0, i32 0
  %46 = load { i64, double }*, { i64, double }** %45
  %47 = bitcast { i64, double }* %46 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %47)
  %48 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %44, i64 0, i32 1
  %49 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %48
  %50 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %49, i64 0, i32 0
  %51 = load %String*, %String** %50
  call void @__quantum__rt__string_unreference(%String* %51)
  %52 = bitcast { %String*, %Qubit* }* %49 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %52)
  %53 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %44 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %53)
  %54 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %54)
  call void @__quantum__rt__callable_unreference(%Callable* %31)
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
  %6 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %7 = load { i64, double }*, { i64, double }** %6
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %9 = bitcast %Tuple* %8 to { { i64, double }*, { %String*, %Qubit* }* }*
  %10 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %11 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  store { i64, double }* %7, { i64, double }** %10
  %12 = bitcast { i64, double }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %12)
  store { %String*, %Qubit* }* %5, { %String*, %Qubit* }** %11
  %13 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %5, i64 0, i32 0
  %14 = load %String*, %String** %13
  call void @__quantum__rt__string_reference(%String* %14)
  %15 = bitcast { %String*, %Qubit* }* %5 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %15)
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %17 = bitcast %Tuple* %16 to { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }*
  %18 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %19 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  store %Array* %4, %Array** %18
  call void @__quantum__rt__array_reference(%Array* %4)
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
  %28 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %28)
  %29 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %30 = load %Callable*, %Callable** %29
  %31 = call %Callable* @__quantum__rt__callable_copy(%Callable* %30)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %31)
  call void @__quantum__rt__callable_make_controlled(%Callable* %31)
  call void @__quantum__rt__callable_invoke(%Callable* %31, %Tuple* %16, %Tuple* %result-tuple)
  %32 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 0
  %33 = load { i64, double }*, { i64, double }** %32
  %34 = bitcast { i64, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  %35 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %9, i64 0, i32 1
  %36 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %35
  %37 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %36, i64 0, i32 0
  %38 = load %String*, %String** %37
  call void @__quantum__rt__string_unreference(%String* %38)
  %39 = bitcast { %String*, %Qubit* }* %36 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %39)
  %40 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %40)
  %41 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 0
  %42 = load %Array*, %Array** %41
  call void @__quantum__rt__array_unreference(%Array* %42)
  %43 = getelementptr { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }, { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17, i64 0, i32 1
  %44 = load { { i64, double }*, { %String*, %Qubit* }* }*, { { i64, double }*, { %String*, %Qubit* }* }** %43
  %45 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %44, i64 0, i32 0
  %46 = load { i64, double }*, { i64, double }** %45
  %47 = bitcast { i64, double }* %46 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %47)
  %48 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %44, i64 0, i32 1
  %49 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %48
  %50 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %49, i64 0, i32 0
  %51 = load %String*, %String** %50
  call void @__quantum__rt__string_unreference(%String* %51)
  %52 = bitcast { %String*, %Qubit* }* %49 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %52)
  %53 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %44 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %53)
  %54 = bitcast { %Array*, { { i64, double }*, { %String*, %Qubit* }* }* }* %17 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %54)
  call void @__quantum__rt__callable_unreference(%Callable* %31)
  ret void
}
