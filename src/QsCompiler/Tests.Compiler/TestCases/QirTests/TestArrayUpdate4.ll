define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate4__body(%Array* %array) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 1)
  %item = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i32 0, i32 0))
  %0 = call %String* @__quantum__rt__string_create(i8* null)
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %arr = alloca %Array*, align 8
  store %Array* %1, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 1)
  %2 = call i64 @__quantum__rt__array_get_size_1d(%Array* %array)
  %3 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %4 = phi i64 [ 0, %entry ], [ %9, %exiting__1 ]
  %5 = icmp sle i64 %4, %3
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array, i64 %4)
  %7 = bitcast i8* %6 to %String**
  %8 = load %String*, %String** %7, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %8, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %9 = add i64 %4, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %array, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 -1)
  store %Array* %array, %Array** %arr, align 8
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %i = phi i64 [ 0, %exit__1 ], [ %17, %exiting__2 ]
  %10 = icmp sle i64 %i, 9
  br i1 %10, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %11 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %11, i32 -1)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %11, i1 false)
  %13 = icmp ne %Array* %11, %12
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 %i)
  %15 = bitcast i8* %14 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %item, i32 1)
  %16 = load %String*, %String** %15, align 8
  br i1 %13, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %body__2
  call void @__quantum__rt__string_update_reference_count(%String* %item, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %body__2
  store %String* %item, %String** %15, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i32 1)
  store %Array* %12, %Array** %arr, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__1
  %17 = add i64 %i, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %18 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %array, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %18, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %item, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  ret %Array* %18
}
