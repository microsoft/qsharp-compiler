define internal %Array* @Microsoft__Quantum__Testing__QIR__Hello__body(i1 %withPunctuation) {
entry:
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %1 = bitcast i8* %0 to %String**
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 1)
  %3 = bitcast i8* %2 to %String**
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 2)
  %5 = bitcast i8* %4 to %String**
  %6 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %7 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i32 0, i32 0))
  %8 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @2, i32 0, i32 0))
  store %String* %6, %String** %1, align 8
  store %String* %7, %String** %3, align 8
  store %String* %8, %String** %5, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  br i1 %withPunctuation, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  br label %header__1

condFalse__1:                                     ; preds = %entry
  %9 = call %Array* @__quantum__rt__array_copy(%Array* %arr, i1 false)
  %10 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 2)
  %12 = bitcast i8* %11 to %String**
  store %String* %10, %String** %12, align 8
  %13 = call i64 @__quantum__rt__array_get_size_1d(%Array* %9)
  %14 = sub i64 %13, 1
  br label %header__2

condContinue__1:                                  ; preds = %exit__2, %exit__1
  %15 = phi %Array* [ %arr, %exit__1 ], [ %9, %exit__2 ]
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  br label %header__3

header__1:                                        ; preds = %exiting__1, %condTrue__1
  %16 = phi i64 [ 0, %condTrue__1 ], [ %21, %exiting__1 ]
  %17 = icmp sle i64 %16, 2
  br i1 %17, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %16)
  %19 = bitcast i8* %18 to %String**
  %20 = load %String*, %String** %19, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %21 = add i64 %16, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 1)
  br label %condContinue__1

header__2:                                        ; preds = %exiting__2, %condFalse__1
  %22 = phi i64 [ 0, %condFalse__1 ], [ %27, %exiting__2 ]
  %23 = icmp sle i64 %22, %14
  br i1 %23, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %22)
  %25 = bitcast i8* %24 to %String**
  %26 = load %String*, %String** %25, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %26, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %27 = add i64 %22, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %10, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  br label %condContinue__1

header__3:                                        ; preds = %exiting__3, %condContinue__1
  %28 = phi i64 [ 0, %condContinue__1 ], [ %33, %exiting__3 ]
  %29 = icmp sle i64 %28, 2
  br i1 %29, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %30 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %28)
  %31 = bitcast i8* %30 to %String**
  %32 = load %String*, %String** %31, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %32, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %33 = add i64 %28, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  ret %Array* %15
}
