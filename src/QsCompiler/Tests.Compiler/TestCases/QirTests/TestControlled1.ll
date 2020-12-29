define void @Lifted__PartialApplication__1__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, %Qubit* }*
  %2 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %1, i64 0, i32 1
  %4 = load %Array*, %Array** %2
  %5 = load %Qubit*, %Qubit** %3
  %6 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %0, i64 0, i32 1
  %7 = load i64, i64* %6
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { %Qubit*, i64 }*
  %10 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %9, i64 0, i32 0
  %11 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %9, i64 0, i32 1
  store %Qubit* %5, %Qubit** %10
  store i64 %7, i64* %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { %Qubit*, i64 }* }*
  %14 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 1
  store %Array* %4, %Array** %14
  call void @__quantum__rt__array_reference(%Array* %4)
  store { %Qubit*, i64 }* %9, { %Qubit*, i64 }** %15
  %16 = bitcast { %Qubit*, i64 }* %9 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %16)
  %17 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %0, i64 0, i32 0
  %18 = load %Callable*, %Callable** %17
  %19 = call %Callable* @__quantum__rt__callable_copy(%Callable* %18)
  call void @__quantum__rt__callable_make_controlled(%Callable* %19)
  call void @__quantum__rt__callable_invoke(%Callable* %19, %Tuple* %12, %Tuple* %result-tuple)
  %20 = bitcast { %Qubit*, i64 }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  %21 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 0
  %22 = load %Array*, %Array** %21
  call void @__quantum__rt__array_unreference(%Array* %22)
  %23 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 1
  %24 = load { %Qubit*, i64 }*, { %Qubit*, i64 }** %23
  %25 = bitcast { %Qubit*, i64 }* %24 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %25)
  %26 = bitcast { %Array*, { %Qubit*, i64 }* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %26)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  ret void
}
