define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %y)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %y)
  %0 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %1 = call i64 @__quantum__rt__array_get_size_1d(%Array* %0)
  %2 = sub i64 %1, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %3 = phi i64 [ 0, %entry ], [ %8, %exiting__1 ]
  %4 = icmp sle i64 %3, %2
  br i1 %4, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %3)
  %6 = bitcast i8* %5 to %String**
  %7 = load %String*, %String** %6
  call void @__quantum__rt__string_reference(%String* %7)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %3, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__string_reference(%String* %b)
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %10 = bitcast i8* %9 to %String**
  %11 = load %String*, %String** %10
  call void @__quantum__rt__string_unreference(%String* %11)
  store %String* %b, %String** %10
  call void @__quantum__rt__array_remove_access(%Array* %y)
  store %Array* %0, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %0)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %13 = call i64 @__quantum__rt__array_get_size_1d(%Array* %12)
  %14 = sub i64 %13, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %15 = phi i64 [ 0, %exit__1 ], [ %20, %exiting__2 ]
  %16 = icmp sle i64 %15, %14
  br i1 %16, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 %15)
  %18 = bitcast i8* %17 to %String**
  %19 = load %String*, %String** %18
  call void @__quantum__rt__string_reference(%String* %19)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %20 = add i64 %15, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %21 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 1)
  %23 = bitcast i8* %22 to %String**
  %24 = load %String*, %String** %23
  call void @__quantum__rt__string_unreference(%String* %24)
  store %String* %21, %String** %23
  call void @__quantum__rt__array_remove_access(%Array* %0)
  store %Array* %12, %Array** %x
  call void @__quantum__rt__array_add_access(%Array* %12)
  call void @__quantum__rt__array_remove_access(%Array* %y)
  call void @__quantum__rt__array_remove_access(%Array* %12)
  %25 = sub i64 %1, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %26 = phi i64 [ 0, %exit__2 ], [ %31, %exiting__3 ]
  %27 = icmp sle i64 %26, %25
  br i1 %27, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %28 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %26)
  %29 = bitcast i8* %28 to %String**
  %30 = load %String*, %String** %29
  call void @__quantum__rt__string_unreference(%String* %30)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %31 = add i64 %26, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_unreference(%Array* %0)
  ret %Array* %12
}
