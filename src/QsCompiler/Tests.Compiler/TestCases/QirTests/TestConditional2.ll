define internal %Array* @Microsoft__Quantum__Testing__QIR__Hello__body(i1 %withPunctuation) {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i32 0, i32 0))
  %2 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @2, i32 0, i32 0))
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %4 = bitcast i8* %3 to %String**
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 1)
  %6 = bitcast i8* %5 to %String**
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 2)
  %8 = bitcast i8* %7 to %String**
  store %String* %0, %String** %4, align 8
  store %String* %1, %String** %6, align 8
  store %String* %2, %String** %8, align 8
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
  br label %header__2

condContinue__1:                                  ; preds = %exit__2, %exit__1
  %13 = phi %Array* [ %arr, %exit__1 ], [ %9, %exit__2 ]
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  br label %header__3

header__1:                                        ; preds = %exiting__1, %condTrue__1
  %14 = phi i64 [ 0, %condTrue__1 ], [ %19, %exiting__1 ]
  %15 = icmp sle i64 %14, 2
  br i1 %15, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %14)
  %17 = bitcast i8* %16 to %String**
  %18 = load %String*, %String** %17, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %19 = add i64 %14, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 1)
  br label %condContinue__1

header__2:                                        ; preds = %exiting__2, %condFalse__1
  %20 = phi i64 [ 0, %condFalse__1 ], [ %25, %exiting__2 ]
  %21 = icmp sle i64 %20, 2
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %20)
  %23 = bitcast i8* %22 to %String**
  %24 = load %String*, %String** %23, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %24, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %25 = add i64 %20, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %10, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  br label %condContinue__1

header__3:                                        ; preds = %exiting__3, %condContinue__1
  %26 = phi i64 [ 0, %condContinue__1 ], [ %31, %exiting__3 ]
  %27 = icmp sle i64 %26, 2
  br i1 %27, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %28 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %26)
  %29 = bitcast i8* %28 to %String**
  %30 = load %String*, %String** %29, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %30, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %31 = add i64 %26, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  ret %Array* %13
}
