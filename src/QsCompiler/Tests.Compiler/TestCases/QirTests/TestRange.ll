define %Range @Microsoft__Quantum__Testing__QIR__TestRange__body() {
entry:
  %0 = load %Range, %Range* @EmptyRange
  %1 = insertvalue %Range %0, i64 0, 0
  %2 = insertvalue %Range %1, i64 2, 1
  %x = insertvalue %Range %2, i64 6, 2
  %a = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 9)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %4 = bitcast i8* %3 to i64*
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %6 = bitcast i8* %5 to i64*
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %8 = bitcast i8* %7 to i64*
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 3)
  %10 = bitcast i8* %9 to i64*
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 4)
  %12 = bitcast i8* %11 to i64*
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 5)
  %14 = bitcast i8* %13 to i64*
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 6)
  %16 = bitcast i8* %15 to i64*
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 7)
  %18 = bitcast i8* %17 to i64*
  %19 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 8)
  %20 = bitcast i8* %19 to i64*
  store i64 0, i64* %4
  store i64 2, i64* %6
  store i64 4, i64* %8
  store i64 6, i64* %10
  store i64 8, i64* %12
  store i64 10, i64* %14
  store i64 12, i64* %16
  store i64 14, i64* %18
  store i64 16, i64* %20
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i64 1)
  %b = call %Array* @__quantum__rt__array_slice_1d(%Array* %a, %Range %x, i1 false)
  call void @__quantum__rt__array_update_alias_count(%Array* %b, i64 1)
  %21 = load %Range, %Range* @EmptyRange
  %22 = insertvalue %Range %21, i64 0, 0
  %23 = insertvalue %Range %22, i64 1, 1
  %y = insertvalue %Range %23, i64 4, 2
  %24 = extractvalue %Range %y, 0
  %25 = extractvalue %Range %y, 1
  %26 = extractvalue %Range %y, 2
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  %27 = icmp sgt i64 %25, 0
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %j = phi i64 [ %24, %preheader__1 ], [ %31, %exiting__1 ]
  %28 = icmp sle i64 %j, %26
  %29 = icmp sge i64 %j, %26
  %30 = select i1 %27, i1 %28, i1 %29
  br i1 %30, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %31 = add i64 %j, %25
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %b, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %a, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %b, i64 -1)
  ret %Range %x
}
