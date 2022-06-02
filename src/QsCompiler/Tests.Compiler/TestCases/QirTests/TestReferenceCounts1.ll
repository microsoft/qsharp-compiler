define internal void @Microsoft__Quantum__Testing__QIR__TestRefCountsForItemUpdate__body(i1 %cond) {
entry:
  %ops = alloca %Array*, align 8
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 5)
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 0)
  %3 = bitcast i8* %2 to %Array**
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 1)
  %5 = bitcast i8* %4 to %Array**
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 2)
  %7 = bitcast i8* %6 to %Array**
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 3)
  %9 = bitcast i8* %8 to %Array**
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 4)
  %11 = bitcast i8* %10 to %Array**
  store %Array* %0, %Array** %3, align 8
  store %Array* %0, %Array** %5, align 8
  store %Array* %0, %Array** %7, align 8
  store %Array* %0, %Array** %9, align 8
  store %Array* %0, %Array** %11, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  store %Array* %1, %Array** %ops, align 8
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %12 = phi i64 [ 0, %entry ], [ %17, %exiting__1 ]
  %13 = icmp sle i64 %12, 4
  br i1 %13, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %12)
  %15 = bitcast i8* %14 to %Array**
  %16 = load %Array*, %Array** %15, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %16, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %17 = add i64 %12, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 1)
  br i1 %cond, label %then0__1, label %continue__1

then0__1:                                         ; preds = %exit__1
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 -1)
  %18 = call %Array* @__quantum__rt__array_copy(%Array* %1, i1 false)
  %19 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 0)
  %21 = bitcast i8* %20 to i64*
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 1)
  %23 = bitcast i8* %22 to i64*
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 2)
  %25 = bitcast i8* %24 to i64*
  store i64 0, i64* %21, align 4
  store i64 0, i64* %23, align 4
  store i64 0, i64* %25, align 4
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %18, i64 0)
  %27 = bitcast i8* %26 to %Array**
  call void @__quantum__rt__array_update_alias_count(%Array* %19, i32 1)
  %28 = load %Array*, %Array** %27, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %28, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %28, i32 -1)
  store %Array* %19, %Array** %27, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %18, i32 1)
  store %Array* %18, %Array** %ops, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %exit__1
  %29 = load %Array*, %Array** %ops, align 8
  %30 = call i64 @__quantum__rt__array_get_size_1d(%Array* %29)
  %31 = sub i64 %30, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %continue__1
  %32 = phi i64 [ 0, %continue__1 ], [ %37, %exiting__2 ]
  %33 = icmp sle i64 %32, %31
  br i1 %33, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %34 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %29, i64 %32)
  %35 = bitcast i8* %34 to %Array**
  %36 = load %Array*, %Array** %35, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %36, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %37 = add i64 %32, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %29, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  %38 = sub i64 %30, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %39 = phi i64 [ 0, %exit__2 ], [ %44, %exiting__3 ]
  %40 = icmp sle i64 %39, %38
  br i1 %40, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %41 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %29, i64 %39)
  %42 = bitcast i8* %41 to %Array**
  %43 = load %Array*, %Array** %42, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %43, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %44 = add i64 %39, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %29, i32 -1)
  ret void
}
