define { { double, i64 }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate__body(i64 %a, i64 %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, i64 }* getelementptr ({ double, i64 }, { double, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, i64 }*
  %2 = getelementptr { double, i64 }, { double, i64 }* %1, i64 0, i32 0
  %3 = getelementptr { double, i64 }, { double, i64 }* %1, i64 0, i32 1
  store double 1.000000e+00, double* %2
  store i64 %a, i64* %3
  %4 = call { { double, i64 }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, i64 }* %1, i64 %b)
  %x = alloca { { double, i64 }*, i64 }*
  store { { double, i64 }*, i64 }* %4, { { double, i64 }*, i64 }** %x
  %5 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ { double, i64 }*, i64 }* getelementptr ({ { double, i64 }*, i64 }, { { double, i64 }*, i64 }* null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { { double, i64 }*, i64 }*
  %8 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %5, i64 0, i32 0
  %9 = load { double, i64 }*, { double, i64 }** %8
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, i64 }* getelementptr ({ double, i64 }, { double, i64 }* null, i32 1) to i64))
  %11 = bitcast %Tuple* %10 to { double, i64 }*
  %12 = getelementptr { double, i64 }, { double, i64 }* %9, i64 0, i32 0
  %13 = load double, double* %12
  %14 = getelementptr { double, i64 }, { double, i64 }* %11, i64 0, i32 0
  store double %13, double* %14
  %15 = getelementptr { double, i64 }, { double, i64 }* %9, i64 0, i32 1
  %16 = load i64, i64* %15
  %17 = getelementptr { double, i64 }, { double, i64 }* %11, i64 0, i32 1
  store i64 %16, i64* %17
  %18 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %7, i64 0, i32 0
  store { double, i64 }* %11, { double, i64 }** %18
  %19 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %5, i64 0, i32 1
  %20 = load i64, i64* %19
  %21 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %7, i64 0, i32 1
  store i64 %20, i64* %21
  %22 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %7, i64 0, i32 0
  %23 = load { double, i64 }*, { double, i64 }** %22
  %24 = getelementptr { double, i64 }, { double, i64 }* %23, i64 0, i32 1
  store i64 2, i64* %24
  %25 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  store { { double, i64 }*, i64 }* %7, { { double, i64 }*, i64 }** %x
  %26 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %7, i64 0, i32 0
  %27 = load { double, i64 }*, { double, i64 }** %26
  %28 = bitcast { double, i64 }* %27 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %28)
  %29 = bitcast { { double, i64 }*, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %29)
  %30 = load { { double, i64 }*, i64 }*, { { double, i64 }*, i64 }** %x
  %31 = bitcast { double, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %31)
  %32 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %4, i64 0, i32 0
  %33 = load { double, i64 }*, { double, i64 }** %32
  %34 = bitcast { double, i64 }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  %35 = bitcast { { double, i64 }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %35)
  %36 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %7, i64 0, i32 0
  %37 = load { double, i64 }*, { double, i64 }** %36
  %38 = bitcast { double, i64 }* %37 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %38)
  %39 = bitcast { { double, i64 }*, i64 }* %7 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %39)
  %40 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %25, i64 0, i32 0
  %41 = load { double, i64 }*, { double, i64 }** %40
  %42 = bitcast { double, i64 }* %41 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %42)
  %43 = bitcast { { double, i64 }*, i64 }* %25 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %43)
  ret { { double, i64 }*, i64 }* %30
}
