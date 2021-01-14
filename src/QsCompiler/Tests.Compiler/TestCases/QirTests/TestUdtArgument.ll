define { i64, { i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %Tuple* null)
  %udt1 = call { i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = bitcast { i64 }* %udt1 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %1)
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2 }* getelementptr ({ %Callable*, i2 }, { %Callable*, i2 }* null, i32 1) to i64))
  %3 = bitcast %Tuple* %2 to { %Callable*, i2 }*
  %4 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %3, i64 0, i32 0
  %5 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %3, i64 0, i32 1
  %6 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType2, %Tuple* null)
  %7 = load i2, i2* @PauliX
  store %Callable* %6, %Callable** %4
  store i2 %7, i2* %5
  %8 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %2)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %8)
  %9 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %11 = bitcast %Tuple* %10 to { %Callable*, i2, double }*
  %12 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 0
  %13 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 1
  %14 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 2
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3, %Tuple* null)
  %16 = load i2, i2* @PauliX
  store %Callable* %15, %Callable** %12
  store i2 %16, i2* %13
  store double 2.000000e+00, double* %14
  %17 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %10)
  %udt3 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %17)
  %18 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %19 = load { i2, i64 }*, { i2, i64 }** %18
  %20 = bitcast { i2, i64 }* %19 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %20)
  %21 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %21)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i2, i64 }* }* getelementptr ({ i64, { i2, i64 }* }, { i64, { i2, i64 }* }* null, i32 1) to i64))
  %23 = bitcast %Tuple* %22 to { i64, { i2, i64 }* }*
  %24 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %23, i64 0, i32 0
  %25 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %23, i64 0, i32 1
  %26 = getelementptr { i64 }, { i64 }* %udt1, i64 0, i32 0
  %27 = load i64, i64* %26
  call void @__quantum__rt__tuple_reference(%Tuple* %9)
  store i64 %27, i64* %24
  store { i2, i64 }* %udt2, { i2, i64 }** %25
  call void @__quantum__rt__tuple_remove_access(%Tuple* %1)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %9)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %20)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %21)
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %9)
  call void @__quantum__rt__callable_unreference(%Callable* %17)
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  ret { i64, { i2, i64 }* }* %23
}
