define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 1)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
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
  call void @__quantum__rt__string_update_reference_count(%String* %7, i64 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %3, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__string_update_reference_count(%String* %b, i64 1)
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %10 = bitcast i8* %9 to %String**
  %11 = load %String*, %String** %10
  call void @__quantum__rt__string_update_reference_count(%String* %11, i64 -1)
  store %String* %b, %String** %10
  store %Array* %0, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 -1)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %13 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 1)
  %15 = bitcast i8* %14 to %String**
  %16 = load %String*, %String** %15
  call void @__quantum__rt__string_update_reference_count(%String* %16, i64 -1)
  store %String* %13, %String** %15
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  store %Array* %12, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i64 -1)
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 -1)
  ret %Array* %12
}
