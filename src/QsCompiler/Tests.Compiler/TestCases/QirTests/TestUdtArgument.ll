define { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %TupleHeader* null)
  %udt1 = call i64 @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = load i2, i2* @PauliX
  %2 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, i2 }* getelementptr ({ %TupleHeader, %Callable*, i2 }, { %TupleHeader, %Callable*, i2 }* null, i32 1) to i64))
  %3 = bitcast %TupleHeader* %2 to { %TupleHeader, %Callable*, i2 }*
  %4 = getelementptr { %TupleHeader, %Callable*, i2 }, { %TupleHeader, %Callable*, i2 }* %3, i64 0, i32 1
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__TestType2, %TupleHeader* null)
  store %Callable* %5, %Callable** %4
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
  %13 = getelementptr { %TupleHeader, %Callable*, i2, double }, { %TupleHeader, %Callable*, i2, double }* %10, i64 0, i32 2
  store i2 %8, i2* %13
  %14 = getelementptr { %TupleHeader, %Callable*, i2, double }, { %TupleHeader, %Callable*, i2, double }* %10, i64 0, i32 3
  store double 2.000000e+00, double* %14
  %15 = bitcast %TupleHeader* %5 to { %TupleHeader, i2, i64 }*
  %16 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__2, %TupleHeader* %9)
  %udt3 = call { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %16)
  %17 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* getelementptr ({ %TupleHeader, i64, { %TupleHeader, i2, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* null, i32 1) to i64))
  %18 = bitcast %TupleHeader* %17 to { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }*
  %19 = bitcast i64 %udt1 to { %TupleHeader, i64 }*
  %20 = getelementptr { %TupleHeader, i64 }, { %TupleHeader, i64 }* %19, i64 0, i32 1
  %21 = load i64, i64* %20
  %22 = getelementptr { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* %18, i64 0, i32 1
  store i64 %21, i64* %22
  %23 = getelementptr { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* %18, i64 0, i32 2
  store { %TupleHeader, i2, i64 }* %udt2, { %TupleHeader, i2, i64 }** %23
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %7)
  call void @__quantum__rt__callable_unreference(%Callable* %16)
  ret { %TupleHeader, i64, { %TupleHeader, i2, i64 }* }* %18
}
