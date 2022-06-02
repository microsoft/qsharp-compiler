define internal { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate1__body(%String* %a, i64 %b) {
entry:
  %x = alloca { { double, %String* }*, i64 }*, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %a, i32 1)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %String* }* getelementptr ({ double, %String* }, { double, %String* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %String* }*
  %2 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 1
  store double 1.000000e+00, double* %2, align 8
  store %String* %a, %String** %3, align 8
  %4 = call { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, %String* }* %1, i64 %b)
  store { { double, %String* }*, i64 }* %4, { { double, %String* }*, i64 }** %x, align 8
  %5 = getelementptr inbounds { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %4, i32 0, i32 0
  %6 = load { double, %String* }*, { double, %String* }** %5, align 8
  %7 = bitcast { double, %String* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 1)
  %8 = bitcast { { double, %String* }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i32 -1)
  %9 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %10 = bitcast %Tuple* %9 to { { double, %String* }*, i64 }*
  %11 = getelementptr inbounds { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %10, i32 0, i32 0
  %12 = load { double, %String* }*, { double, %String* }** %11, align 8
  %13 = bitcast { double, %String* }* %12 to %Tuple*
  %14 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %13, i1 false)
  %15 = bitcast %Tuple* %14 to { double, %String* }*
  %16 = getelementptr inbounds { double, %String* }, { double, %String* }* %15, i32 0, i32 1
  %17 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i32 0, i32 0))
  %18 = load %String*, %String** %16, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 -1)
  store %String* %17, %String** %16, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %13, i32 -1)
  store { double, %String* }* %15, { double, %String* }** %11, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i32 1)
  store { { double, %String* }*, i64 }* %10, { { double, %String* }*, i64 }** %x, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %a, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %13, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 -1)
  ret { { double, %String* }*, i64 }* %10
}
