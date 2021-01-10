define { i64, { i2, i64 }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %Tuple* null)
  %udt1 = call { i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %0)
  %1 = bitcast { i64 }* %udt1 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %1)
  %2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType2, %Tuple* null)
  %3 = load i2, i2* @PauliX
  %4 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2 }* getelementptr ({ %Callable*, i2 }, { %Callable*, i2 }* null, i32 1) to i64))
  %5 = bitcast %Tuple* %4 to { %Callable*, i2 }*
  %6 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %5, i64 0, i32 0
  %7 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %5, i64 0, i32 1
  store %Callable* %2, %Callable** %6
  call void @__quantum__rt__callable_reference(%Callable* %2)
  store i2 %3, i2* %7
  %8 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %4)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %8)
  %9 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
  %10 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3, %Tuple* null)
  %11 = load i2, i2* @PauliX
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %Callable*, i2, double }*
  %14 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %13, i64 0, i32 0
  %15 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %13, i64 0, i32 1
  %16 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %13, i64 0, i32 2
  store %Callable* %10, %Callable** %14
  call void @__quantum__rt__callable_reference(%Callable* %10)
  store i2 %11, i2* %15
  store double 2.000000e+00, double* %16
  %17 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %12)
  %udt3 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %17)
  %18 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %19 = load { i2, i64 }*, { i2, i64 }** %18
  %20 = bitcast { i2, i64 }* %19 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %20)
  %21 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %21)
  %22 = getelementptr { i64 }, { i64 }* %udt1, i64 0, i32 0
  %23 = load i64, i64* %22
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i2, i64 }* }* getelementptr ({ i64, { i2, i64 }* }, { i64, { i2, i64 }* }* null, i32 1) to i64))
  %25 = bitcast %Tuple* %24 to { i64, { i2, i64 }* }*
  %26 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %25, i64 0, i32 0
  %27 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %25, i64 0, i32 1
  store i64 %23, i64* %26
  store { i2, i64 }* %udt2, { i2, i64 }** %27
  call void @__quantum__rt__tuple_reference(%Tuple* %9)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %1)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %9)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %20)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %21)
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %2)
  call void @__quantum__rt__callable_unreference(%Callable* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %9)
  call void @__quantum__rt__callable_unreference(%Callable* %10)
  call void @__quantum__rt__callable_unreference(%Callable* %17)
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  ret { i64, { i2, i64 }* }* %25
}
