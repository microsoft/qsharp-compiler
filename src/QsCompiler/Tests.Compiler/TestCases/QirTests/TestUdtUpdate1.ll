define { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate1__body(%String* %a, i64 %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %String* }* getelementptr ({ double, %String* }, { double, %String* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %String* }*
  %2 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 1
  store double 1.000000e+00, double* %2, align 8
  store %String* %a, %String** %3, align 8
  %4 = call { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, %String* }* %1, i64 %b)
  %x = alloca { { double, %String* }*, i64 }*, align 8
  store { { double, %String* }*, i64 }* %4, { { double, %String* }*, i64 }** %x, align 8
  %5 = getelementptr inbounds { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %4, i32 0, i32 0
  %6 = load { double, %String* }*, { double, %String* }** %5, align 8
  %7 = bitcast { double, %String* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 1)
  %8 = bitcast { { double, %String* }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i32 1)
  %9 = getelementptr inbounds { double, %String* }, { double, %String* }* %6, i32 0, i32 1
  %10 = load %String*, %String** %9, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %10, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i32 -1)
  %11 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %12 = bitcast %Tuple* %11 to { { double, %String* }*, i64 }*
  %13 = getelementptr inbounds { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %12, i32 0, i32 0
  %14 = load { double, %String* }*, { double, %String* }** %13, align 8
  %15 = bitcast { double, %String* }* %14 to %Tuple*
  %16 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %15, i1 false)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %15, i32 -1)
  %17 = bitcast %Tuple* %16 to { double, %String* }*
  store { double, %String* }* %17, { double, %String* }** %13, align 8
  %18 = getelementptr inbounds { double, %String* }, { double, %String* }* %17, i32 0, i32 1
  %19 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 1)
  %20 = load %String*, %String** %18, align 8
  store %String* %19, %String** %18, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i32 1)
  store { { double, %String* }*, i64 }* %12, { { double, %String* }*, i64 }** %x, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %10, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 -1)
  ret { { double, %String* }*, i64 }* %12
}
