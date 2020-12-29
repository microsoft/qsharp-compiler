define { i64, { i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %Tuple* null)
  %udt1 = call { i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType2, %Tuple* null)
  %2 = load i2, i2* @PauliX
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2 }* getelementptr ({ %Callable*, i2 }, { %Callable*, i2 }* null, i32 1) to i64))
  %4 = bitcast %Tuple* %3 to { %Callable*, i2 }*
  %5 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %4, i64 0, i32 0
  %6 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %4, i64 0, i32 1
  store %Callable* %1, %Callable** %5
  call void @__quantum__rt__callable_reference(%Callable* %1)
  store i2 %2, i2* %6
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %3)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %7)
  %8 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3, %Tuple* null)
  %9 = load i2, i2* @PauliX
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %11 = bitcast %Tuple* %10 to { %Callable*, i2, double }*
  %12 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 0
  %13 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 1
  %14 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 2
  store %Callable* %8, %Callable** %12
  call void @__quantum__rt__callable_reference(%Callable* %8)
  store i2 %9, i2* %13
  store double 2.000000e+00, double* %14
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %10)
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
  call void @__quantum__rt__callable_unreference(%Callable* %1)
  %25 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %4, i64 0, i32 0
  %26 = load %Callable*, %Callable** %25
  call void @__quantum__rt__callable_unreference(%Callable* %26)
  %27 = bitcast { %Callable*, i2 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %27)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  %28 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %28)
  call void @__quantum__rt__callable_unreference(%Callable* %8)
  %29 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %11, i64 0, i32 0
  %30 = load %Callable*, %Callable** %29
  call void @__quantum__rt__callable_unreference(%Callable* %30)
  %31 = bitcast { %Callable*, i2, double }* %11 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %31)
  call void @__quantum__rt__callable_unreference(%Callable* %15)
  %32 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %33 = load { i2, i64 }*, { i2, i64 }** %32
  %34 = bitcast { i2, i64 }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  %35 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %35)
  %36 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %36)
  ret { i64, { i2, i64 }* }* %20
}