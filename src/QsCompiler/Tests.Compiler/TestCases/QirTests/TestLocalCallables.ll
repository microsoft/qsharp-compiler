define { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %fct = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple, %Tuple* null)
  %0 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { %String* }*
  %3 = getelementptr { %String* }, { %String* }* %2, i64 0, i32 0
  store %String* %0, %String** %3
  call void @__quantum__rt__string_reference(%String* %0)
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %5 = bitcast %Tuple* %4 to { %String*, { i64, double }* }*
  %6 = bitcast { %String*, { i64, double }* }* %5 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %fct, %Tuple* %1, %Tuple* %6)
  %7 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %5, i64 0, i32 0
  %8 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %5, i64 0, i32 1
  %str = load %String*, %String** %7
  %9 = load { i64, double }*, { i64, double }** %8
  %10 = getelementptr { i64, double }, { i64, double }* %9, i64 0, i32 0
  %11 = getelementptr { i64, double }, { i64, double }* %9, i64 0, i32 1
  %val = load double, double* %11
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %String*, double }*
  %14 = getelementptr { %String*, double }, { %String*, double }* %13, i64 0, i32 0
  %15 = getelementptr { %String*, double }, { %String*, double }* %13, i64 0, i32 1
  store %String* %str, %String** %14
  call void @__quantum__rt__string_reference(%String* %str)
  store double %val, double* %15
  call void @__quantum__rt__callable_unreference(%Callable* %fct)
  call void @__quantum__rt__string_unreference(%String* %0)
  %16 = getelementptr { %String* }, { %String* }* %2, i64 0, i32 0
  %17 = load %String*, %String** %16
  call void @__quantum__rt__string_unreference(%String* %17)
  %18 = bitcast { %String* }* %2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  %19 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %5, i64 0, i32 0
  %20 = load %String*, %String** %19
  call void @__quantum__rt__string_unreference(%String* %20)
  %21 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %5, i64 0, i32 1
  %22 = load { i64, double }*, { i64, double }** %21
  %23 = bitcast { i64, double }* %22 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %23)
  %24 = bitcast { %String*, { i64, double }* }* %5 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %24)
  ret { %String*, double }* %13
}

define void @Microsoft__Quantum__Testing__QIR__ReturnTuple__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = load %String*, %Tuple* %arg-tuple
  %1 = call { %String*, { i64, double }* }* @Microsoft__Quantum__Testing__QIR__ReturnTuple__body(%String* %0)
  %2 = bitcast %Tuple* %result-tuple to { %String*, { i64, double }* }*
  %3 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %2, i64 0, i32 0
  %4 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %1, i64 0, i32 0
  %5 = load %String*, %String** %4
  store %String* %5, %String** %3
  %6 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %2, i64 0, i32 1
  %7 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %1, i64 0, i32 1
  %8 = load { i64, double }*, { i64, double }** %7
  store { i64, double }* %8, { i64, double }** %6
  ret void
}
