define void @Lifted__PartialApplication__1__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, %Qubit* }*
  %1 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %0, i64 0, i32 0
  %2 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %0, i64 0, i32 1
  %3 = load %Array*, %Array** %1
  %4 = load %Qubit*, %Qubit** %2
  %5 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %6 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %5, i64 0, i32 1
  %7 = load i64, i64* %6
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { %Qubit*, i64 }*
  %10 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %9, i64 0, i32 0
  %11 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %9, i64 0, i32 1
  store %Qubit* %4, %Qubit** %10
  store i64 %7, i64* %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, { %Qubit*, i64 }* }*
  %14 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %13, i64 0, i32 1
  store %Array* %3, %Array** %14
  call void @__quantum__rt__array_reference(%Array* %3)
  store { %Qubit*, i64 }* %9, { %Qubit*, i64 }** %15
  call void @__quantum__rt__tuple_reference(%Tuple* %8)
  %16 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %5, i64 0, i32 0
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* %12, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  call void @__quantum__rt__array_unreference(%Array* %3)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %12)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  ret void
}