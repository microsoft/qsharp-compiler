define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate4__body(%Array* %array) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 1)
  %item = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i32 0, i32 0))
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %arr = alloca %Array*, align 8
  store %Array* %0, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  %1 = call i64 @__quantum__rt__array_get_size_1d(%Array* %array)
  %2 = sub i64 %1, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %3 = phi i64 [ 0, %entry ], [ %8, %exiting__1 ]
  %4 = icmp sle i64 %3, %2
  br i1 %4, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array, i64 %3)
  %6 = bitcast i8* %5 to %String**
  %7 = load %String*, %String** %6, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %7, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %3, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %array, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 -1)
  store %Array* %array, %Array** %arr, align 8
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %i = phi i64 [ 0, %exit__1 ], [ %16, %exiting__2 ]
  %9 = icmp sle i64 %i, 9
  br i1 %9, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %10 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %10, i32 -1)
  %11 = call %Array* @__quantum__rt__array_copy(%Array* %10, i1 false)
  %12 = icmp ne %Array* %10, %11
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %11, i64 %i)
  %14 = bitcast i8* %13 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %item, i32 1)
  %15 = load %String*, %String** %14, align 8
  br i1 %12, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %body__2
  call void @__quantum__rt__string_update_reference_count(%String* %item, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %body__2
  store %String* %item, %String** %14, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %11, i32 1)
  store %Array* %11, %Array** %arr, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__1
  %16 = add i64 %i, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %17 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %17, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %item, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  ret %Array* %17
}
