define { i64, { i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %Tuple* null)
  %udt1 = call { i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = load i2, i2* @PauliX
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2 }* getelementptr ({ %Callable*, i2 }, { %Callable*, i2 }* null, i32 1) to i64))
  %3 = bitcast %Tuple* %2 to { %Callable*, i2 }*
  %4 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %3, i64 0, i32 0
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType2, %Tuple* null)
  store %Callable* %5, %Callable** %4
  call void @__quantum__rt__callable_reference(%Callable* %5)
  %6 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %3, i64 0, i32 1
  store i2 %1, i2* %6
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %2)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %7)
  %8 = load i2, i2* @PauliX
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Callable*, i2, double }*
  %11 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %10, i64 0, i32 0
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3, %Tuple* null)
  store %Callable* %12, %Callable** %11
  call void @__quantum__rt__callable_reference(%Callable* %12)
  %13 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %10, i64 0, i32 1
  store i2 %8, i2* %13
  %14 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %10, i64 0, i32 2
  store double 2.000000e+00, double* %14
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %9)
  %udt3 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %15)
  %16 = getelementptr { i64 }, { i64 }* %udt1, i64 0, i32 0
  %17 = load i64, i64* %16
  %18 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i2, i64 }* }* getelementptr ({ i64, { i2, i64 }* }, { i64, { i2, i64 }* }* null, i32 1) to i64))
  %20 = bitcast %Tuple* %19 to { i64, { i2, i64 }* }*
  %21 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %20, i64 0, i32 0
  %22 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %20, i64 0, i32 1
  store i64 %17, i64* %21
  store { i2, i64 }* %udt2, { i2, i64 }** %22
  %23 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %23)
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %24 = bitcast { i64 }* %udt1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %24)
  call void @__quantum__rt__callable_unreference(%Callable* %5)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  %25 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %25)
  call void @__quantum__rt__callable_unreference(%Callable* %12)
  call void @__quantum__rt__callable_unreference(%Callable* %15)
  %26 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %27 = load { i2, i64 }*, { i2, i64 }** %26
  %28 = bitcast { i2, i64 }* %27 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %28)
  %29 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %29)
  %30 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %30)
  ret { i64, { i2, i64 }* }* %20
}
