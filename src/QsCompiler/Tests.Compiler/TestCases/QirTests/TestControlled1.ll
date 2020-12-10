define void @Lifted__PartialApplication__2__ctl__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %capture-tuple to { %TupleHeader, %Callable* }*
  %1 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Array*, %TupleHeader* }*
  %2 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %1, i64 0, i32 1
  %3 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %1, i64 0, i32 2
  %4 = bitcast %TupleHeader** %3 to { %TupleHeader, %Qubit*, i64 }*
  %5 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit*, i64 }* getelementptr ({ %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* null, i32 1) to i64))
  %6 = bitcast %TupleHeader* %5 to { %TupleHeader, %Qubit*, i64 }*
  %7 = getelementptr { %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* %6, i64 0, i32 1
  %8 = getelementptr { %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* %4, i64 0, i32 1
  %9 = load %Qubit*, %Qubit** %8
  store %Qubit* %9, %Qubit** %7
  %10 = getelementptr { %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* %6, i64 0, i32 2
  %11 = getelementptr { %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* %4, i64 0, i32 2
  %12 = load i64, i64* %11
  store i64 %12, i64* %10
  %13 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Array*, %TupleHeader* }* getelementptr ({ %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* null, i32 1) to i64))
  %14 = bitcast %TupleHeader* %13 to { %TupleHeader, %Array*, %TupleHeader* }*
  %15 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %14, i64 0, i32 1
  %16 = load %Array*, %Array** %2
  store %Array* %16, %Array** %15
  %17 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %14, i64 0, i32 2
  store %TupleHeader* %5, %TupleHeader** %17
  %18 = getelementptr { %TupleHeader, %Callable* }, { %TupleHeader, %Callable* }* %0, i64 0, i32 1
  %19 = load %Callable*, %Callable** %18
  %20 = call %Callable* @__quantum__rt__callable_copy(%Callable* %19)
  call void @__quantum__rt__callable_make_controlled(%Callable* %20)
  call void @__quantum__rt__callable_invoke(%Callable* %20, %TupleHeader* %13, %TupleHeader* %result-tuple)
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %5)
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %13)
  call void @__quantum__rt__callable_unreference(%Callable* %20)
  ret void
}
