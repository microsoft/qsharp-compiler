define { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %TupleHeader* null)
  %udt1 = call { %TupleHeader, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = load i2, i2* @PauliX
  %2 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, i2 }* getelementptr ({ %TupleHeader, %Callable*, i2 }, { %TupleHeader, %Callable*, i2 }* null, i32 1) to i64))
  %3 = bitcast %TupleHeader* %2 to { %TupleHeader, %Callable*, i2 }*
  %4 = getelementptr { %TupleHeader, %Callable*, i2 }, { %TupleHeader, %Callable*, i2 }* %3, i64 0, i32 1
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__TestType2, %TupleHeader* null)
  store %Callable* %5, %Callable** %4
  call void @__quantum__rt__callable_reference(%Callable* %5)
  %6 = getelementptr { %TupleHeader, %Callable*, i2 }, { %TupleHeader, %Callable*, i2 }* %3, i64 0, i32 2
  store i2 %1, i2* %6
  %7 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__1, %TupleHeader* %2)
  %udt2 = call { %TupleHeader, i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %7)
  %8 = load i2, i2* @PauliX
  %9 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, i2, double }* getelementptr ({ %TupleHeader, %Callable*, i2, double }, { %TupleHeader, %Callable*, i2, double }* null, i32 1) to i64))
  %10 = bitcast %TupleHeader* %9 to { %TupleHeader, %Callable*, i2, double }*
  %11 = getelementptr { %TupleHeader, %Callable*, i2, double }, { %TupleHeader, %Callable*, i2, double }* %10, i64 0, i32 1
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__TestType3, %TupleHeader* null)
  store %Callable* %12, %Callable** %11
  call void @__quantum__rt__callable_reference(%Callable* %12)
  %13 = getelementptr { %TupleHeader, %Callable*, i2, double }, { %TupleHeader, %Callable*, i2, double }* %10, i64 0, i32 2
  store i2 %8, i2* %13
  %14 = getelementptr { %TupleHeader, %Callable*, i2, double }, { %TupleHeader, %Callable*, i2, double }* %10, i64 0, i32 3
  store double 2.000000e+00, double* %14
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__2, %TupleHeader* %9)
  %udt3 = call { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %15)
  %16 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* getelementptr ({ %TupleHeader, i64, { %TupleHeader, i2, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* null, i32 1) to i64))
  %17 = bitcast %TupleHeader* %16 to { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }*
  %18 = getelementptr { %TupleHeader, i64 }, { %TupleHeader, i64 }* %udt1, i64 0, i32 1
  %19 = load i64, i64* %18
  %20 = getelementptr { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* %17, i64 0, i32 1
  store i64 %19, i64* %20
  %21 = bitcast { %TupleHeader, i2, i64 }* %udt2 to %TupleHeader*
  call void @__quantum__rt__tuple_reference(%TupleHeader* %21)
  %22 = getelementptr { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* %17, i64 0, i32 2
  store { %TupleHeader, i2, i64 }* %udt2, { %TupleHeader, i2, i64 }** %22
  %23 = bitcast { %TupleHeader, i2, i64 }* %udt2 to %TupleHeader*
  call void @__quantum__rt__tuple_reference(%TupleHeader* %23)
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %24 = bitcast { %TupleHeader, i64 }* %udt1 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %24)
  call void @__quantum__rt__callable_unreference(%Callable* %5)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  %25 = bitcast { %TupleHeader, i2, i64 }* %udt2 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %25)
  call void @__quantum__rt__callable_unreference(%Callable* %12)
  call void @__quantum__rt__callable_unreference(%Callable* %15)
  %26 = bitcast { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %udt3 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %26)
  %27 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %udt3, i64 0, i32 1
  %28 = load { %TupleHeader, i2, i64 }*, { %TupleHeader, i2, i64 }** %27
  %29 = bitcast { %TupleHeader, i2, i64 }* %28 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %29)
  %30 = bitcast { %TupleHeader, i2, i64 }* %udt2 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %30)
  ret { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* %17
}
