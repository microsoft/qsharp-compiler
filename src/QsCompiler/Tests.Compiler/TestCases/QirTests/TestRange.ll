define internal %Range @Microsoft__Quantum__Testing__QIR__TestRange__body() {
entry:
  %a = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 9)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %1 = bitcast i8* %0 to i64*
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %3 = bitcast i8* %2 to i64*
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %5 = bitcast i8* %4 to i64*
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 3)
  %7 = bitcast i8* %6 to i64*
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 4)
  %9 = bitcast i8* %8 to i64*
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 5)
  %11 = bitcast i8* %10 to i64*
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 6)
  %13 = bitcast i8* %12 to i64*
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 7)
  %15 = bitcast i8* %14 to i64*
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 8)
  %17 = bitcast i8* %16 to i64*
  store i64 0, i64* %1, align 4
  store i64 2, i64* %3, align 4
  store i64 4, i64* %5, align 4
  store i64 6, i64* %7, align 4
  store i64 8, i64* %9, align 4
  store i64 10, i64* %11, align 4
  store i64 12, i64* %13, align 4
  store i64 14, i64* %15, align 4
  store i64 16, i64* %17, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i32 1)
  %b = call %Array* @__quantum__rt__array_slice_1d(%Array* %a, %Range { i64 0, i64 2, i64 6 }, i1 true)
  call void @__quantum__rt__array_update_alias_count(%Array* %b, i32 1)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %j = phi i64 [ 0, %preheader__1 ], [ %21, %exiting__1 ]
  %18 = icmp sle i64 %j, 4
  %19 = icmp sge i64 %j, 4
  %20 = select i1 true, i1 %18, i1 %19
  br i1 %20, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %21 = add i64 %j, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %b, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %a, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %b, i32 -1)
  ret %Range { i64 0, i64 2, i64 6 }
}
