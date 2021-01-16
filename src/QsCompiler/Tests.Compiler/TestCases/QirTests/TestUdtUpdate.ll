define { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate__body(%String* %a, i64 %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %String* }* getelementptr ({ double, %String* }, { double, %String* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %String* }*
  %2 = getelementptr { double, %String* }, { double, %String* }* %1, i64 0, i32 0
  %3 = getelementptr { double, %String* }, { double, %String* }* %1, i64 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %a, i64 1)
  store double 1.000000e+00, double* %2
  store %String* %a, %String** %3
  %4 = call { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, %String* }* %1, i64 %b)
  %x = alloca { { double, %String* }*, i64 }*
  store { { double, %String* }*, i64 }* %4, { { double, %String* }*, i64 }** %x
  %5 = getelementptr { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %4, i64 0, i32 0
  %6 = load { double, %String* }*, { double, %String* }** %5
  %7 = bitcast { double, %String* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %7, i64 1)
  %8 = bitcast { { double, %String* }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %8, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %8, i64 -1)
  %9 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %10 = bitcast %Tuple* %9 to { { double, %String* }*, i64 }*
  %11 = getelementptr { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %10, i64 0, i32 0
  %12 = getelementptr { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %10, i64 0, i32 1
  %13 = load { double, %String* }*, { double, %String* }** %11
  %14 = bitcast { double, %String* }* %13 to %Tuple*
  %15 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %14, i1 false)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %15, i64 1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %14, i64 -1)
  %16 = bitcast %Tuple* %15 to { double, %String* }*
  store { double, %String* }* %16, { double, %String* }** %11
  %17 = getelementptr { double, %String* }, { double, %String* }* %16, i64 0, i32 0
  %18 = getelementptr { double, %String* }, { double, %String* }* %16, i64 0, i32 1
  %19 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  store %String* %19, %String** %18
  store { { double, %String* }*, i64 }* %10, { { double, %String* }*, i64 }** %x
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %9, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %15, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %a, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i64 -1)
  %20 = getelementptr { double, %String* }, { double, %String* }* %6, i64 0, i32 1
  %21 = load %String*, %String** %20
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  ret { { double, %String* }*, i64 }* %10
}
