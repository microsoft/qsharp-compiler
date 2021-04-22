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
  %updated = bitcast %Tuple* %7 to { %String*, %Array* }*
  %8 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %updated, i32 0, i32 1
  %9 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 0)
  %11 = bitcast i8* %10 to double*
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 1)
  %13 = bitcast i8* %12 to double*
  store double 1.000000e-01, double* %11, align 8
  store double 2.000000e-01, double* %13, align 8
  store %Array* %9, %Array** %8, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %9, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 1)
  br i1 %cond, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %9, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  ret { %String*, %Array* }* %s

else__1:                                          ; preds = %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %9, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  ret { %String*, %Array* }* %s

continue__1:                                      ; No predecessors!
  %14 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([28 x i8], [28 x i8]* @1, i32 0, i32 0))
  %15 = getelementptr inbounds { %String*, %Array* }, { %String*, %Array* }* %s, i32 0, i32 0
  %16 = load %String*, %String** %15, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %9, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__fail(%String* %14)
  unreachable
}
