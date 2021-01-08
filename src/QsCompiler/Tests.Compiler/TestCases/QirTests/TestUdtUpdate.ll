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
  %5 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %4, i64 0, i32 0
  %6 = load { double, i64 }*, { double, i64 }** %5
  %7 = bitcast { double, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %7)
  %8 = bitcast { { double, i64 }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %8)
  %9 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %10 = bitcast %Tuple* %9 to { { double, i64 }*, i64 }*
  %11 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %10, i64 0, i32 0
  %12 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %10, i64 0, i32 1
  %13 = load { double, i64 }*, { double, i64 }** %11
  %14 = bitcast { double, i64 }* %13 to %Tuple*
  %15 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %14, i1 false)
  %16 = bitcast %Tuple* %15 to { double, i64 }*
  store { double, i64 }* %16, { double, i64 }** %11
  %17 = getelementptr { double, i64 }, { double, i64 }* %16, i64 0, i32 0
  %18 = getelementptr { double, i64 }, { double, i64 }* %16, i64 0, i32 1
  store i64 2, i64* %18
  %19 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %4, i64 0, i32 0
  %20 = load { double, i64 }*, { double, i64 }** %19
  %21 = bitcast { double, i64 }* %20 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %21)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %8)
  store { { double, i64 }*, i64 }* %10, { { double, i64 }*, i64 }** %x
  %22 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %10, i64 0, i32 0
  %23 = load { double, i64 }*, { double, i64 }** %22
  %24 = bitcast { double, i64 }* %23 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %24)
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
  %25 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %10, i64 0, i32 0
  %26 = load { double, i64 }*, { double, i64 }** %25
  %27 = bitcast { double, i64 }* %26 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %27)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %9)
  call void @__quantum__rt__tuple_unreference(%Tuple* %0)
  %28 = getelementptr { { double, i64 }*, i64 }, { { double, i64 }*, i64 }* %4, i64 0, i32 0
  %29 = load { double, i64 }*, { double, i64 }** %28
  %30 = bitcast { double, i64 }* %29 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %30)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %15)
  ret { { double, i64 }*, i64 }* %10
}
