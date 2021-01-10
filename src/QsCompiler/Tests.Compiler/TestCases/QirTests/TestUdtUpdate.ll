define { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate__body(%String* %a, i64 %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %String* }* getelementptr ({ double, %String* }, { double, %String* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %String* }*
  %2 = getelementptr { double, %String* }, { double, %String* }* %1, i64 0, i32 0
  %3 = getelementptr { double, %String* }, { double, %String* }* %1, i64 0, i32 1
  call void @__quantum__rt__string_reference(%String* %a)
  store double 1.000000e+00, double* %2
  store %String* %a, %String** %3
  %4 = call { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, %String* }* %1, i64 %b)
  %x = alloca { { double, %String* }*, i64 }*
  store { { double, %String* }*, i64 }* %4, { { double, %String* }*, i64 }** %x
  %5 = getelementptr { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %4, i64 0, i32 0
  %6 = load { double, %String* }*, { double, %String* }** %5
  %7 = bitcast { double, %String* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %7)
  %8 = bitcast { { double, %String* }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %8)
  %9 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %10 = bitcast %Tuple* %9 to { { double, %String* }*, i64 }*
  %11 = getelementptr { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %10, i64 0, i32 0
  %12 = getelementptr { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %10, i64 0, i32 1
  %13 = load { double, %String* }*, { double, %String* }** %11
  %14 = bitcast { double, %String* }* %13 to %Tuple*
  %15 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %14, i1 false)
  %16 = bitcast %Tuple* %15 to { double, %String* }*
  store { double, %String* }* %16, { double, %String* }** %11
  %17 = getelementptr { double, %String* }, { double, %String* }* %16, i64 0, i32 0
  %18 = getelementptr { double, %String* }, { double, %String* }* %16, i64 0, i32 1
  %19 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  store %String* %19, %String** %18
  call void @__quantum__rt__tuple_remove_access(%Tuple* %7)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %8)
  store { { double, %String* }*, i64 }* %10, { { double, %String* }*, i64 }** %x
  call void @__quantum__rt__tuple_add_access(%Tuple* %15)
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %15)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %9)
  call void @__quantum__rt__string_unreference(%String* %a)
  call void @__quantum__rt__tuple_unreference(%Tuple* %0)
  %20 = getelementptr { double, %String* }, { double, %String* }* %6, i64 0, i32 1
  %21 = load %String*, %String** %20
  call void @__quantum__rt__string_unreference(%String* %21)
  call void @__quantum__rt__tuple_unreference(%Tuple* %7)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  ret { { double, %String* }*, i64 }* %10
}
