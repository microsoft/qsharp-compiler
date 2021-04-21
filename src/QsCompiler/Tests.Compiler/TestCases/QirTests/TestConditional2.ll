define %Array* @Microsoft__Quantum__Testing__QIR__Hello__body(i1 %withPunctuation) {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i32 0, i32 0))
  %2 = call %String* @__quantum__rt__string_create(i8* null)
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
  %10 = icmp ne %Array* %arr, %9
  %11 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 2)
  %13 = bitcast i8* %12 to %String**
  store %String* %11, %String** %13, align 8
  %14 = call i64 @__quantum__rt__array_get_size_1d(%Array* %9)
  %15 = sub i64 %14, 1
  br label %header__2

condContinue__1:                                  ; preds = %exit__2, %exit__1
  %16 = phi %Array* [ %arr, %exit__1 ], [ %9, %exit__2 ]
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %2, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  ret %Array* %16

header__1:                                        ; preds = %exiting__1, %condTrue__1
  %17 = phi i64 [ 0, %condTrue__1 ], [ %22, %exiting__1 ]
  %18 = icmp sle i64 %17, 2
  br i1 %18, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %19 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %17)
  %20 = bitcast i8* %19 to %String**
  %21 = load %String*, %String** %20, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %21, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %22 = add i64 %17, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 1)
  br label %condContinue__1

header__2:                                        ; preds = %exiting__2, %condFalse__1
  %23 = phi i64 [ 0, %condFalse__1 ], [ %28, %exiting__2 ]
  %24 = icmp sle i64 %23, %15
  br i1 %24, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %25 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %23)
  %26 = bitcast i8* %25 to %String**
  %27 = load %String*, %String** %26, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %27, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %28 = add i64 %23, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  br label %condContinue__1
}
