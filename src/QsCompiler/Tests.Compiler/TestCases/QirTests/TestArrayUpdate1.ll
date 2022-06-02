define internal %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate1__body(%String* %even) {
entry:
  %arr = alloca %Array*, align 8
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @0, i32 0, i32 0))
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 0)
  %3 = bitcast i8* %2 to %String**
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 1)
  %5 = bitcast i8* %4 to %String**
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 2)
  %7 = bitcast i8* %6 to %String**
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 3)
  %9 = bitcast i8* %8 to %String**
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 4)
  %11 = bitcast i8* %10 to %String**
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 5)
  %13 = bitcast i8* %12 to %String**
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 6)
  %15 = bitcast i8* %14 to %String**
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 7)
  %17 = bitcast i8* %16 to %String**
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 8)
  %19 = bitcast i8* %18 to %String**
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 9)
  %21 = bitcast i8* %20 to %String**
  store %String* %0, %String** %3, align 8
  store %String* %0, %String** %5, align 8
  store %String* %0, %String** %7, align 8
  store %String* %0, %String** %9, align 8
  store %String* %0, %String** %11, align 8
  store %String* %0, %String** %13, align 8
  store %String* %0, %String** %15, align 8
  store %String* %0, %String** %17, align 8
  store %String* %0, %String** %19, align 8
  store %String* %0, %String** %21, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)
  store %Array* %1, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %31, %exiting__1 ]
  %22 = icmp sle i64 %i, 9
  br i1 %22, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %23 = srem i64 %i, 2
  %24 = icmp ne i64 %23, 0
  br i1 %24, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %body__1
  %25 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @1, i32 0, i32 0))
  br label %condContinue__1

condFalse__1:                                     ; preds = %body__1
  call void @__quantum__rt__string_update_reference_count(%String* %even, i32 1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %str = phi %String* [ %25, %condTrue__1 ], [ %even, %condFalse__1 ]
  %26 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %26, i32 -1)
  %27 = call %Array* @__quantum__rt__array_copy(%Array* %26, i1 false)
  %28 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %27, i64 %i)
  %29 = bitcast i8* %28 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 1)
  %30 = load %String*, %String** %29, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %30, i32 -1)
  store %String* %str, %String** %29, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %27, i32 1)
  store %Array* %27, %Array** %arr, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %26, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__1
  %31 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %32 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %32, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret %Array* %32
}
