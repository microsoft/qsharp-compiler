define void @Microsoft__Quantum__Testing__QIR__TestRefCountsForItemUpdate__body(i1 %cond) {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 5)
  %1 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %6, %exiting__1 ]
  %3 = icmp sle i64 %2, 4
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %2)
  %5 = bitcast i8* %4 to %Array**
  store %Array* %1, %Array** %5, align 8
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %6 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %ops = alloca %Array*, align 8
  store %Array* %0, %Array** %ops, align 8
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %7 = phi i64 [ 0, %exit__1 ], [ %12, %exiting__2 ]
  %8 = icmp sle i64 %7, 4
  br i1 %8, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %7)
  %10 = bitcast i8* %9 to %Array**
  %11 = load %Array*, %Array** %10, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %11, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %12 = add i64 %7, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %13 = phi i64 [ 0, %exit__2 ], [ %18, %exiting__3 ]
  %14 = icmp sle i64 %13, 4
  br i1 %14, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %13)
  %16 = bitcast i8* %15 to %Array**
  %17 = load %Array*, %Array** %16, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i32 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %18 = add i64 %13, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  br i1 %cond, label %then0__1, label %continue__1

then0__1:                                         ; preds = %exit__3
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 -1)
  %19 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %20 = icmp ne %Array* %0, %19
  %21 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  br label %header__4

continue__1:                                      ; preds = %exit__4, %exit__3
  %22 = load %Array*, %Array** %ops, align 8
  %23 = call i64 @__quantum__rt__array_get_size_1d(%Array* %22)
  %24 = sub i64 %23, 1
  br label %header__5

header__4:                                        ; preds = %exiting__4, %then0__1
  %25 = phi i64 [ 0, %then0__1 ], [ %29, %exiting__4 ]
  %26 = icmp sle i64 %25, 2
  br i1 %26, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %27 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %21, i64 %25)
  %28 = bitcast i8* %27 to i64*
  store i64 0, i64* %28, align 4
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %29 = add i64 %25, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  %30 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 0)
  %31 = bitcast i8* %30 to %Array**
  call void @__quantum__rt__array_update_reference_count(%Array* %21, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %21, i32 1)
  %32 = load %Array*, %Array** %31, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %32, i32 -1)
  store %Array* %21, %Array** %31, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %19, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %19, i32 1)
  store %Array* %19, %Array** %ops, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %21, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %32, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %19, i32 -1)
  br label %continue__1

header__5:                                        ; preds = %exiting__5, %continue__1
  %33 = phi i64 [ 0, %continue__1 ], [ %38, %exiting__5 ]
  %34 = icmp sle i64 %33, %24
  br i1 %34, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %35 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %22, i64 %33)
  %36 = bitcast i8* %35 to %Array**
  %37 = load %Array*, %Array** %36, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %37, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %38 = add i64 %33, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_alias_count(%Array* %22, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  %39 = sub i64 %23, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %40 = phi i64 [ 0, %exit__5 ], [ %45, %exiting__6 ]
  %41 = icmp sle i64 %40, %39
  br i1 %41, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %42 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %22, i64 %40)
  %43 = bitcast i8* %42 to %Array**
  %44 = load %Array*, %Array** %43, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %44, i32 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %45 = add i64 %40, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %22, i32 -1)
  ret void
}
