define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate2__body(%Array* %array, %String* %even) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i64 1)
  %arr = alloca %Array*
  store %Array* %array, %Array** %arr
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i64 1)
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %array)
  %1 = sub i64 %0, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %7, %exiting__1 ]
  %3 = icmp sle i64 %2, %1
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array, i64 %2)
  %5 = bitcast i8* %4 to %String**
  %6 = load %String*, %String** %5
  call void @__quantum__rt__string_update_reference_count(%String* %6, i64 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %7 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %array, i64 1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %i = phi i64 [ 0, %exit__1 ], [ %18, %exiting__2 ]
  %8 = icmp sle i64 %i, 9
  br i1 %8, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %9 = srem i64 %i, 2
  %10 = icmp ne i64 %9, 0
  br i1 %10, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__2
  %11 = call %String* @__quantum__rt__string_create(i32 3, i8* getelementptr inbounds ([4 x i8], [4 x i8]* @1, i32 0, i32 0))
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__2
  call void @__quantum__rt__string_update_reference_count(%String* %even, i64 1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %str = phi %String* [ %11, %condTrue__1 ], [ %even, %condFalse__1 ]
  %12 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i64 -1)
  %13 = call %Array* @__quantum__rt__array_copy(%Array* %12, i1 false)
  %14 = icmp ne %Array* %12, %13
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %13, i64 %i)
  %16 = bitcast i8* %15 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 1)
  %17 = load %String*, %String** %16
  br i1 %14, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %condContinue__1
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i64 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condContinue__1
  store %String* %str, %String** %16
  call void @__quantum__rt__array_update_reference_count(%Array* %13, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %13, i64 1)
  store %Array* %13, %Array** %arr
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %13, i64 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__2
  %18 = add i64 %i, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %19 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %19, i64 -1)
  ret %Array* %19
}
