define { %String*, %Array* }* @Microsoft__Quantum__Testing__QIR__TestPendingRefCountIncreases__body(i1 %cond) {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 0)
  %3 = bitcast i8* %2 to double*
  store double 0.000000e+00, double* %3, align 8
  %s = call { %String*, %Array* }* @Microsoft__Quantum__Testing__QIR__HasString__body(%String* %0, %Array* %1)
  %4 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %s, i32 0, i32 1
  %5 = load %Array*, %Array** %4, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 1)
  %6 = bitcast { %String*, %Array* }* %s to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 1)
  %7 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %6, i1 false)
  %8 = icmp ne %Tuple* %6, %7
  %updated = bitcast %Tuple* %7 to { %String*, %Array* }*
  %9 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %updated, i32 0, i32 1
  %10 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %10, i64 0)
  %12 = bitcast i8* %11 to double*
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %10, i64 1)
  %14 = bitcast i8* %13 to double*
  store double 1.000000e-01, double* %12, align 8
  store double 2.000000e-01, double* %14, align 8
  br i1 %8, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %entry
  %15 = load %Array*, %Array** %9, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %15, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %entry
  store %Array* %10, %Array** %9, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %10, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 1)
  br i1 %cond, label %then0__1, label %else__1

then0__1:                                         ; preds = %condContinue__1
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %10, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  ret { %String*, %Array* }* %s

else__1:                                          ; preds = %condContinue__1
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %10, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  ret { %String*, %Array* }* %s

continue__1:                                      ; No predecessors!
  %16 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @1, i32 0, i32 0))
  %17 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %s, i32 0, i32 0
  %18 = load %String*, %String** %17, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %10, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__fail(%String* %16)
  unreachable
}
