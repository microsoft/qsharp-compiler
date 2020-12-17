define void @Lifted__PartialApplication__1__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = bitcast %Tuple* %arg-tuple to { %Array*, %Qubit* }*
  %2 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %1, i64 0, i32 0
  %3 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %1, i64 0, i32 1
  %4 = load %Qubit*, %Qubit** %3
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { %Qubit*, i64 }*
  %7 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %6, i64 0, i32 0
  store %Qubit* %4, %Qubit** %7
  %8 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %6, i64 0, i32 1
  %9 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %0, i64 0, i32 1
  %10 = load i64, i64* %9
  store i64 %10, i64* %8
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %12 = bitcast %Tuple* %11 to { %Array*, %Tuple* }*
  %13 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %12, i64 0, i32 0
  %14 = load %Array*, %Array** %2
  store %Array* %14, %Array** %13
  %15 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %12, i64 0, i32 1
  store %Tuple* %5, %Tuple** %15
  %16 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %0, i64 0, i32 0
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %11, %Tuple* %result-tuple)
  %19 = bitcast { %Qubit*, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
  %20 = bitcast { %Array*, %Tuple* }* %12 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  %21 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %12, i64 0, i32 0
  %22 = load %Array*, %Array** %21
  call void @__quantum__rt__array_unreference(%Array* %22)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  ret void
}
