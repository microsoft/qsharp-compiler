﻿define void @Lifted__PartialApplication__2__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 1
  %2 = load { i64, double }*, { i64, double }** %1
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %4 = bitcast %Tuple* %3 to { { i64, double }*, { %String*, %Qubit* }* }*
  %5 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 0
  %6 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 1
  store { i64, double }* %2, { i64, double }** %5
  %7 = bitcast { i64, double }* %2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %7)
  %8 = bitcast %Tuple* %arg-tuple to { %String*, %Qubit* }*
  store { %String*, %Qubit* }* %8, { %String*, %Qubit* }** %6
  %9 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %8, i64 0, i32 0
  %10 = load %String*, %String** %9
  call void @__quantum__rt__string_reference(%String* %10)
  call void @__quantum__rt__tuple_reference(%Tuple* %arg-tuple)
  %11 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i64 0, i32 0
  %12 = load %Callable*, %Callable** %11
  call void @__quantum__rt__callable_invoke(%Callable* %12, %Tuple* %3, %Tuple* %result-tuple)
  %13 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 0
  %14 = load { i64, double }*, { i64, double }** %13
  %15 = bitcast { i64, double }* %14 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %15)
  %16 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %4, i64 0, i32 1
  %17 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %16
  %18 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %17, i64 0, i32 0
  %19 = load %String*, %String** %18
  call void @__quantum__rt__string_unreference(%String* %19)
  %20 = bitcast { %String*, %Qubit* }* %17 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  call void @__quantum__rt__tuple_unreference(%Tuple* %3)
  ret void
}
