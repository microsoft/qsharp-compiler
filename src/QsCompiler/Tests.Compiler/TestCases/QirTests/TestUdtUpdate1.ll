define { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate1__body(%String* %a, i64 %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %String* }* getelementptr ({ double, %String* }, { double, %String* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %String* }*
  %2 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 1
  store double 1.000000e+00, double* %2
  store %String* %a, %String** %3
  %4 = call { { double, %String* }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ double, %String* }* %1, i64 %b)
  %x = alloca { { double, %String* }*, i64 }*
  store { { double, %String* }*, i64 }* %4, { { double, %String* }*, i64 }** %x
  %5 = getelementptr inbounds { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %4, i32 0, i32 0
  %6 = load { double, %String* }*, { double, %String* }** %5
  %7 = bitcast { double, %String* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i64 1)
  %8 = bitcast { { double, %String* }*, i64 }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i64 1)
  %9 = getelementptr inbounds { double, %String* }, { double, %String* }* %6, i32 0, i32 1
  %10 = load %String*, %String** %9
  call void @__quantum__rt__string_update_reference_count(%String* %10, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %8, i64 -1)
  %11 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %12 = icmp ne %Tuple* %8, %11
  %13 = bitcast %Tuple* %11 to { { double, %String* }*, i64 }*
  %14 = getelementptr inbounds { { double, %String* }*, i64 }, { { double, %String* }*, i64 }* %13, i32 0, i32 0
  %15 = load { double, %String* }*, { double, %String* }** %14
  %16 = bitcast { double, %String* }* %15 to %Tuple*
  %17 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %16, i1 false)
  %18 = icmp ne %Tuple* %16, %17
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %17, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i64 -1)
  br i1 %18, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %entry
  %19 = bitcast %Tuple* %17 to { double, %String* }*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %entry
  store { double, %String* }* %19, { double, %String* }** %14
  %20 = getelementptr inbounds { double, %String* }, { double, %String* }* %19, i32 0, i32 1
  %21 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 1)
  %22 = load %String*, %String** %20
  br i1 %12, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %condContinue__1
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i64 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condContinue__1
  store %String* %21, %String** %20
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i64 1)
  store { { double, %String* }*, i64 }* %13, { { double, %String* }*, i64 }** %x
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %17, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %10, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  ret { { double, %String* }*, i64 }* %13
}
