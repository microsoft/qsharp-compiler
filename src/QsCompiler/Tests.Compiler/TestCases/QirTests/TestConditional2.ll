define %Array* @Microsoft__Quantum__Testing__QIR__Hello__body(i1 %withPunctuation) {
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
  %8 = call %String* @__quantum__rt__string_create(i8* null)
  store %String* %6, %String** %1, align 8
  store %String* %7, %String** %3, align 8
  store %String* %8, %String** %5, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  br i1 %withPunctuation, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  br label %header__1

condFalse__1:                                     ; preds = %entry
  %9 = call %Array* @__quantum__rt__array_copy(%Array* %arr, i1 false)
  %10 = icmp ne %Array* %arr, %9
  %11 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 2)
  %13 = bitcast i8* %12 to %String**
  br i1 %10, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %condFalse__1
  %14 = load %String*, %String** %13, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %14, i32 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condFalse__1
  store %String* %11, %String** %13, align 8
  %15 = call i64 @__quantum__rt__array_get_size_1d(%Array* %9)
  %16 = sub i64 %15, 1
  br label %header__2

condContinue__1:                                  ; preds = %exit__2, %exit__1
  %17 = phi %Array* [ %arr, %exit__1 ], [ %9, %exit__2 ]
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  br label %header__3

header__1:                                        ; preds = %exiting__1, %condTrue__1
  %18 = phi i64 [ 0, %condTrue__1 ], [ %23, %exiting__1 ]
  %19 = icmp sle i64 %18, 2
  br i1 %19, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %18)
  %21 = bitcast i8* %20 to %String**
  %22 = load %String*, %String** %21, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %22, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %23 = add i64 %18, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 1)
  br label %condContinue__1

header__2:                                        ; preds = %exiting__2, %condContinue__2
  %24 = phi i64 [ 0, %condContinue__2 ], [ %29, %exiting__2 ]
  %25 = icmp sle i64 %24, %16
  br i1 %25, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %24)
  %27 = bitcast i8* %26 to %String**
  %28 = load %String*, %String** %27, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %28, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %29 = add i64 %24, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  br label %condContinue__1

header__3:                                        ; preds = %exiting__3, %condContinue__1
  %30 = phi i64 [ 0, %condContinue__1 ], [ %35, %exiting__3 ]
  %31 = icmp sle i64 %30, 2
  br i1 %31, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %32 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %30)
  %33 = bitcast i8* %32 to %String**
  %34 = load %String*, %String** %33, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %34, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %35 = add i64 %30, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  ret %Array* %17
}
