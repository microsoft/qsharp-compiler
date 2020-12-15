define void @Lifted__PartialApplication__1__ctl__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %capture-tuple to { %TupleHeader, %Callable*, i64 }*
  %1 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Array*, %Qubit* }*
  %2 = getelementptr { %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* %1, i64 0, i32 1
  %3 = getelementptr { %TupleHeader, %Array*, %Qubit* }, { %TupleHeader, %Array*, %Qubit* }* %1, i64 0, i32 2
  %4 = load %Qubit*, %Qubit** %3
  %5 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit*, i64 }* getelementptr ({ %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* null, i32 1) to i64))
  %6 = bitcast %TupleHeader* %5 to { %TupleHeader, %Qubit*, i64 }*
  %7 = getelementptr { %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* %6, i64 0, i32 1
  store %Qubit* %4, %Qubit** %7
  %8 = getelementptr { %TupleHeader, %Qubit*, i64 }, { %TupleHeader, %Qubit*, i64 }* %6, i64 0, i32 2
  %9 = getelementptr { %TupleHeader, %Callable*, i64 }, { %TupleHeader, %Callable*, i64 }* %0, i64 0, i32 2
  %10 = load i64, i64* %9
  store i64 %10, i64* %8
  %11 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Array*, %TupleHeader* }* getelementptr ({ %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* null, i32 1) to i64))
  %12 = bitcast %TupleHeader* %11 to { %TupleHeader, %Array*, %TupleHeader* }*
  %13 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %12, i64 0, i32 1
  %14 = load %Array*, %Array** %2
  store %Array* %14, %Array** %13
  %15 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %12, i64 0, i32 2
  store %TupleHeader* %5, %TupleHeader** %15
  %16 = getelementptr { %TupleHeader, %Callable*, i64 }, { %TupleHeader, %Callable*, i64 }* %0, i64 0, i32 1
  %17 = load %Callable*, %Callable** %16
  %18 = call %Callable* @__quantum__rt__callable_copy(%Callable* %17)
  call void @__quantum__rt__callable_make_controlled(%Callable* %18)
  call void @__quantum__rt__callable_invoke(%Callable* %18, %TupleHeader* %11, %TupleHeader* %result-tuple)
  %19 = bitcast { %TupleHeader, %Qubit*, i64 }* %6 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %19)
  %20 = bitcast { %TupleHeader, %Array*, %TupleHeader* }* %12 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %20)
  %21 = getelementptr { %TupleHeader, %Array*, %TupleHeader* }, { %TupleHeader, %Array*, %TupleHeader* }* %12, i64 0, i32 1
  %22 = load %Array*, %Array** %21
  call void @__quantum__rt__array_unreference(%Array* %22)
  call void @__quantum__rt__callable_unreference(%Callable* %18)
  ret void
}
