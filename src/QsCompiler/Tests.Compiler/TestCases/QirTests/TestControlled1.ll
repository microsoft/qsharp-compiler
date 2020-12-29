define void @Lifted__PartialApplication__1__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, %Qubit* }*
  %2 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %1, i64 0, i32 1
  %4 = load %Array*, %Array** %2
  %5 = load %Qubit*, %Qubit** %3
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { %Qubit*, i64 }*
  %8 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %7, i64 0, i32 0
  store %Qubit* %5, %Qubit** %8
  %9 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %7, i64 0, i32 1
  %10 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %0, i64 0, i32 1
  %11 = load i64, i64* %10
  store i64 %11, i64* %9
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { %Qubit*, i64 }* }*
  %14 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 1
  store %Array* %4, %Array** %14
  call void @__quantum__rt__array_reference(%Array* %4)
  store { %Qubit*, i64 }* %7, { %Qubit*, i64 }** %15
  %16 = bitcast { %Qubit*, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %16)
  %17 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %0, i64 0, i32 0
  %18 = load %Callable*, %Callable** %17
  %19 = call %Callable* @__quantum__rt__callable_copy(%Callable* %18)
  call void @__quantum__rt__callable_make_controlled(%Callable* %19)
  %20 = bitcast { %Array*, { %Qubit*, i64 }* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %19, %Tuple* %20, %Tuple* %result-tuple)
  %21 = bitcast { %Qubit*, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  %22 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 0
  %23 = load %Array*, %Array** %22
  call void @__quantum__rt__array_unreference(%Array* %23)
  %24 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 1
  %25 = load { %Qubit*, i64 }*, { %Qubit*, i64 }** %24
  %26 = bitcast { %Qubit*, i64 }* %25 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %26)
  %27 = bitcast { %Array*, { %Qubit*, i64 }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %27)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  ret void
}
