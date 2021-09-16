define internal void @Microsoft__Quantum__Testing__QIR__TestRefCountsForItemUpdate__body(i1 %cond) {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 5)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %6, %exiting__1 ]
  %3 = icmp sle i64 %2, 4
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %2)
  %5 = bitcast i8* %4 to %Array**
  store %Array* %0, %Array** %5, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %6 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %ops = alloca %Array*, align 8
  store %Array* %1, %Array** %ops, align 8
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %7 = phi i64 [ 0, %exit__1 ], [ %12, %exiting__2 ]
  %8 = icmp sle i64 %7, 4
  br i1 %8, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %7)
  %10 = bitcast i8* %9 to %Array**
  %11 = load %Array*, %Array** %10, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %11, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %12 = add i64 %7, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 1)
  br i1 %cond, label %then0__1, label %continue__1

then0__1:                                         ; preds = %exit__2
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 -1)
  %13 = call %Array* @__quantum__rt__array_copy(%Array* %1, i1 false)
  %14 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  br label %header__3

continue__1:                                      ; preds = %exit__3, %exit__2
  %15 = load %Array*, %Array** %ops, align 8
  %16 = call i64 @__quantum__rt__array_get_size_1d(%Array* %15)
  %17 = sub i64 %16, 1
  br label %header__4

header__3:                                        ; preds = %exiting__3, %then0__1
  %18 = phi i64 [ 0, %then0__1 ], [ %22, %exiting__3 ]
  %19 = icmp sle i64 %18, 2
  br i1 %19, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %14, i64 %18)
  %21 = bitcast i8* %20 to i64*
  store i64 0, i64* %21, align 4
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %22 = add i64 %18, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  %23 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %13, i64 0)
  %24 = bitcast i8* %23 to %Array**
  call void @__quantum__rt__array_update_alias_count(%Array* %14, i32 1)
  %25 = load %Array*, %Array** %24, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %25, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %25, i32 -1)
  store %Array* %14, %Array** %24, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %13, i32 1)
  store %Array* %13, %Array** %ops, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  br label %continue__1

header__4:                                        ; preds = %exiting__4, %continue__1
  %26 = phi i64 [ 0, %continue__1 ], [ %31, %exiting__4 ]
  %27 = icmp sle i64 %26, %17
  br i1 %27, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %28 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %15, i64 %26)
  %29 = bitcast i8* %28 to %Array**
  %30 = load %Array*, %Array** %29, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %30, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %31 = add i64 %26, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_alias_count(%Array* %15, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  %32 = sub i64 %16, 1
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %33 = phi i64 [ 0, %exit__4 ], [ %38, %exiting__5 ]
  %34 = icmp sle i64 %33, %32
  br i1 %34, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %35 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %15, i64 %33)
  %36 = bitcast i8* %35 to %Array**
  %37 = load %Array*, %Array** %36, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %37, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %38 = add i64 %33, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_reference_count(%Array* %15, i32 -1)
  ret void
}
