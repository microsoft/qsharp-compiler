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
  %9 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %5, i64 0, i32 0
  %10 = load %Callable*, %Callable** %9
  call void @__quantum__rt__callable_reference(%Callable* %10)
  call void @__quantum__rt__tuple_reference(%Tuple* %4)
  %udt2 = call { i2, i64 }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %8)
  %11 = bitcast { i2, i64 }* %udt2 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %11)
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TestType3, %Tuple* null)
  %13 = load i2, i2* @PauliX
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i2, double }* getelementptr ({ %Callable*, i2, double }, { %Callable*, i2, double }* null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { %Callable*, i2, double }*
  %16 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %15, i64 0, i32 0
  %17 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %15, i64 0, i32 1
  %18 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %15, i64 0, i32 2
  store %Callable* %12, %Callable** %16
  call void @__quantum__rt__callable_reference(%Callable* %12)
  store i2 %13, i2* %17
  store double 2.000000e+00, double* %18
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %14)
  %20 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %15, i64 0, i32 0
  %21 = load %Callable*, %Callable** %20
  call void @__quantum__rt__callable_reference(%Callable* %21)
  call void @__quantum__rt__tuple_reference(%Tuple* %14)
  %udt3 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR_____GUID___Build__body(%Callable* %19)
  %22 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %23 = load { i2, i64 }*, { i2, i64 }** %22
  %24 = bitcast { i2, i64 }* %23 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %24)
  %25 = bitcast { { i2, i64 }*, double }* %udt3 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %25)
  %26 = getelementptr { i64 }, { i64 }* %udt1, i64 0, i32 0
  %27 = load i64, i64* %26
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i2, i64 }* }* getelementptr ({ i64, { i2, i64 }* }, { i64, { i2, i64 }* }* null, i32 1) to i64))
  %29 = bitcast %Tuple* %28 to { i64, { i2, i64 }* }*
  %30 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %29, i64 0, i32 0
  %31 = getelementptr { i64, { i2, i64 }* }, { i64, { i2, i64 }* }* %29, i64 0, i32 1
  store i64 %27, i64* %30
  store { i2, i64 }* %udt2, { i2, i64 }** %31
  call void @__quantum__rt__tuple_reference(%Tuple* %11)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %1)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %11)
  %32 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %33 = load { i2, i64 }*, { i2, i64 }** %32
  %34 = bitcast { i2, i64 }* %33 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %34)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %25)
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %2)
  %35 = getelementptr { %Callable*, i2 }, { %Callable*, i2 }* %5, i64 0, i32 0
  %36 = load %Callable*, %Callable** %35
  call void @__quantum__rt__callable_unreference(%Callable* %36)
  call void @__quantum__rt__tuple_unreference(%Tuple* %4)
  call void @__quantum__rt__callable_unreference(%Callable* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %11)
  call void @__quantum__rt__callable_unreference(%Callable* %12)
  %37 = getelementptr { %Callable*, i2, double }, { %Callable*, i2, double }* %15, i64 0, i32 0
  %38 = load %Callable*, %Callable** %37
  call void @__quantum__rt__callable_unreference(%Callable* %38)
  call void @__quantum__rt__tuple_unreference(%Tuple* %14)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  %39 = getelementptr { { i2, i64 }*, double }, { { i2, i64 }*, double }* %udt3, i64 0, i32 0
  %40 = load { i2, i64 }*, { i2, i64 }** %39
  %41 = bitcast { i2, i64 }* %40 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %41)
  call void @__quantum__rt__tuple_unreference(%Tuple* %25)
  ret { i64, { i2, i64 }* }* %29
}