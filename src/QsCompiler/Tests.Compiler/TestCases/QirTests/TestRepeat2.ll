define internal %Array* @Microsoft__Quantum__Testing__QIR__TestRepeat2__body(%Qubit* %q) {
entry:
  %res = alloca %Array*, align 8
  %iter = alloca i64, align 8
  store i64 1, i64* %iter, align 4
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  store %Array* %0, %Array** %res, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 1)
  br label %repeat__1

repeat__1:                                        ; preds = %exit__4, %entry
  call void @__quantum__qis__h__body(%Qubit* %q)
  %1 = load %Array*, %Array** %res, align 8
  %2 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %3 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %3, i64 0)
  %5 = bitcast i8* %4 to %Result**
  store %Result* %2, %Result** %5, align 8
  %6 = call %Array* @__quantum__rt__array_concatenate(%Array* %1, %Array* %3)
  %7 = call i64 @__quantum__rt__array_get_size_1d(%Array* %6)
  %8 = sub i64 %7, 1
  br label %header__1

until__1:                                         ; preds = %exit__2
  %9 = load i64, i64* %iter, align 4
  %10 = icmp sgt i64 %9, 3
  br i1 %10, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %11 = mul i64 %9, 2
  store i64 %11, i64* %iter, align 4
  br label %header__4

rend__1:                                          ; preds = %until__1
  br label %header__3

header__1:                                        ; preds = %exiting__1, %repeat__1
  %12 = phi i64 [ 0, %repeat__1 ], [ %17, %exiting__1 ]
  %13 = icmp sle i64 %12, %8
  br i1 %13, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 %12)
  %15 = bitcast i8* %14 to %Result**
  %16 = load %Result*, %Result** %15, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %16, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %17 = add i64 %12, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %6, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %1, i32 -1)
  %18 = call i64 @__quantum__rt__array_get_size_1d(%Array* %1)
  %19 = sub i64 %18, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %20 = phi i64 [ 0, %exit__1 ], [ %25, %exiting__2 ]
  %21 = icmp sle i64 %20, %19
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %20)
  %23 = bitcast i8* %22 to %Result**
  %24 = load %Result*, %Result** %23, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %24, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %25 = add i64 %20, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %1, i32 -1)
  store %Array* %6, %Array** %res, align 8
  br label %until__1

header__3:                                        ; preds = %exiting__3, %rend__1
  %26 = phi i64 [ 0, %rend__1 ], [ %31, %exiting__3 ]
  %27 = icmp sle i64 %26, 0
  br i1 %27, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %28 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %3, i64 %26)
  %29 = bitcast i8* %28 to %Result**
  %30 = load %Result*, %Result** %29, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %30, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %31 = add i64 %26, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i32 -1)
  %32 = load %Array*, %Array** %res, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %32, i32 -1)
  ret %Array* %32

header__4:                                        ; preds = %exiting__4, %fixup__1
  %33 = phi i64 [ 0, %fixup__1 ], [ %38, %exiting__4 ]
  %34 = icmp sle i64 %33, 0
  br i1 %34, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %35 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %3, i64 %33)
  %36 = bitcast i8* %35 to %Result**
  %37 = load %Result*, %Result** %36, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %37, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %38 = add i64 %33, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i32 -1)
  br label %repeat__1
}
