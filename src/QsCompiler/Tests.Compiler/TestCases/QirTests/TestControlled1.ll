define void @Lifted__PartialApplication__2__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable* }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, %Tuple* }*
  %2 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %1, i64 0, i32 1
  %4 = bitcast %Tuple** %3 to { %Qubit*, i64 }*
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { %Qubit*, i64 }*
  %7 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %6, i64 0, i32 0
  %8 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %4, i64 0, i32 0
  %9 = load %Qubit*, %Qubit** %8
  store %Qubit* %9, %Qubit** %7
  %10 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %6, i64 0, i32 1
  %11 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %4, i64 0, i32 1
  %12 = load i64, i64* %11
  store i64 %12, i64* %10
  %13 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %14 = bitcast %Tuple* %13 to { %Array*, %Tuple* }*
  %15 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %14, i64 0, i32 0
  %16 = load %Array*, %Array** %2
  store %Array* %16, %Array** %15
  %17 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %14, i64 0, i32 1
  store %Tuple* %5, %Tuple** %17
  %18 = getelementptr { %Callable* }, { %Callable* }* %0, i64 0, i32 0
  %19 = load %Callable*, %Callable** %18
  %20 = call %Callable* @__quantum__rt__callable_copy(%Callable* %19)
  call void @__quantum__rt__callable_make_controlled(%Callable* %20)
  call void @__quantum__rt__callable_invoke(%Callable* %20, %Tuple* %13, %Tuple* %result-tuple)
  %21 = bitcast { %Qubit*, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  %22 = bitcast { %Array*, %Tuple* }* %14 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  %23 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %14, i64 0, i32 0
  %24 = load %Array*, %Array** %23
  call void @__quantum__rt__array_unreference(%Array* %24)
  call void @__quantum__rt__callable_unreference(%Callable* %20)
  ret void
}
