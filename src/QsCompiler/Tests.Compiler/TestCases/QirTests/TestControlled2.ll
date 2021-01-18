define void @Lifted__PartialApplication__2__ctl__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %Array*, { %Qubit*, i64 }* }*
  %1 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %0, i64 0, i32 0
  %2 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %0, i64 0, i32 1
  %3 = load %Array*, %Array** %1
  %4 = load { %Qubit*, i64 }*, { %Qubit*, i64 }** %2
  %5 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %4, i64 0, i32 0
  %6 = load %Qubit*, %Qubit** %5
  %7 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %4, i64 0, i32 1
  %8 = load i64, i64* %7
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit*, i64 }* getelementptr ({ %Qubit*, i64 }, { %Qubit*, i64 }* null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Qubit*, i64 }*
  %11 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %10, i64 0, i32 0
  %12 = getelementptr { %Qubit*, i64 }, { %Qubit*, i64 }* %10, i64 0, i32 1
  store %Qubit* %6, %Qubit** %11
  store i64 %8, i64* %12
  %13 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %14 = bitcast %Tuple* %13 to { %Array*, { %Qubit*, i64 }* }*
  %15 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %14, i64 0, i32 0
  %16 = getelementptr { %Array*, { %Qubit*, i64 }* }, { %Array*, { %Qubit*, i64 }* }* %14, i64 0, i32 1
  store %Array* %3, %Array** %15
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 1)
  store { %Qubit*, i64 }* %10, { %Qubit*, i64 }** %16
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 1)
  %17 = bitcast %Tuple* %capture-tuple to { %Callable* }*
  %18 = getelementptr { %Callable* }, { %Callable* }* %17, i64 0, i32 0
  %19 = load %Callable*, %Callable** %18
  %20 = call %Callable* @__quantum__rt__callable_copy(%Callable* %19, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %20, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %20)
  call void @__quantum__rt__callable_invoke(%Callable* %20, %Tuple* %13, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %13, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %20, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %20, i64 -1)
  ret void
}
