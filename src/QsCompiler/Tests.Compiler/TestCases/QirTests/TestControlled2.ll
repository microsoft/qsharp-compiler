define void @Lifted__PartialApplication__2__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable* }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, { %Qubit*, i64 }* }*
  %2 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %1, i64 0, i32 1
  %4 = load %Array*, %Array** %2
  %5 = load { %Qubit*, i64 }*, { %Qubit*, i64 }** %3
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { %Qubit*, i64 }*
  %8 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %7, i64 0, i32 0
  %9 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %5, i64 0, i32 0
  %10 = load %Qubit*, %Qubit** %9
  store %Qubit* %10, %Qubit** %8
  %11 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %7, i64 0, i32 1
  %12 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %5, i64 0, i32 1
  %13 = load i64, i64* %12
  store i64 %13, i64* %11
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %15 = bitcast %Tuple* %14 to { %Array*, { %Qubit*, i64 }* }*
  %16 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %15, i64 0, i32 0
  %17 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %15, i64 0, i32 1
  store %Array* %4, %Array** %16
  call void @__quantum__rt__array_reference(%Array* %4)
  store { %Qubit*, i64 }* %7, { %Qubit*, i64 }** %17
  %18 = bitcast { %Qubit*, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %19 = getelementptr { %Callable* }, { %Callable* }* %0, i64 0, i32 0
  %20 = load %Callable*, %Callable** %19
  %21 = call %Callable* @__quantum__rt__callable_copy(%Callable* %20)
  call void @__quantum__rt__callable_make_controlled(%Callable* %21)
  %22 = bitcast { %Array*, { %Qubit*, i64 }* }* %15 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %21, %Tuple* %22, %Tuple* %result-tuple)
  %23 = bitcast { %Qubit*, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  %24 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %15, i64 0, i32 0
  %25 = load %Array*, %Array** %24
  call void @__quantum__rt__array_unreference(%Array* %25)
  %26 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %15, i64 0, i32 1
  %27 = load { %Qubit*, i64 }*, { %Qubit*, i64 }** %26
  %28 = bitcast { %Qubit*, i64 }* %27 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %28)
  %29 = bitcast { %Array*, { %Qubit*, i64 }* }* %15 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %29)
  call void @__quantum__rt__callable_unreference(%Callable* %21)
  ret void
}
