define internal %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate1__body(%String* %even) {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @0, i32 0, i32 0))
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %6, %exiting__1 ]
  %3 = icmp sle i64 %2, 9
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %2)
  %5 = bitcast i8* %4 to %String**
  store %String* %0, %String** %5, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %6 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %arr = alloca %Array*, align 8
  store %Array* %1, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %i = phi i64 [ 0, %exit__1 ], [ %16, %exiting__2 ]
  %7 = icmp sle i64 %i, 9
  br i1 %7, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %8 = srem i64 %i, 2
  %9 = icmp ne i64 %8, 0
  br i1 %9, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__2
  %10 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @1, i32 0, i32 0))
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__2
  call void @__quantum__rt__string_update_reference_count(%String* %even, i32 1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %str = phi %String* [ %10, %condTrue__1 ], [ %even, %condFalse__1 ]
  %11 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %11, i32 -1)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %11, i1 false)
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 %i)
  %14 = bitcast i8* %13 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 1)
  %15 = load %String*, %String** %14, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  store %String* %str, %String** %14, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i32 1)
  store %Array* %12, %Array** %arr, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__1
  %16 = add i64 %i, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %17 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %17, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret %Array* %17
}
