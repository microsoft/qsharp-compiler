define void @Microsoft__Quantum__Testing__QIR__TestRefCountsForItemUpdate__body(i1 %cond) {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 5)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %1 = phi i64 [ 0, %entry ], [ %6, %exiting__1 ]
  %2 = icmp sle i64 %1, 4
  br i1 %2, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %3 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %1)
  %5 = bitcast i8* %4 to %Array**
  store %Array* %3, %Array** %5
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %6 = add i64 %1, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %ops = alloca %Array*
  store %Array* %0, %Array** %ops
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %7 = phi i64 [ 0, %exit__1 ], [ %12, %exiting__2 ]
  %8 = icmp sle i64 %7, 4
  br i1 %8, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %7)
  %10 = bitcast i8* %9 to %Array**
  %11 = load %Array*, %Array** %10
  call void @__quantum__rt__array_update_alias_count(%Array* %11, i64 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %12 = add i64 %7, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i64 1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %13 = phi i64 [ 0, %exit__2 ], [ %18, %exiting__3 ]
  %14 = icmp sle i64 %13, 4
  br i1 %14, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %13)
  %16 = bitcast i8* %15 to %Array**
  %17 = load %Array*, %Array** %16
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i64 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %18 = add i64 %13, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 1)
  br i1 %cond, label %then0__1, label %continue__1

then0__1:                                         ; preds = %exit__3
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i64 -1)
  %19 = call { i1, %Array* }* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %20 = getelementptr { i1, %Array* }, { i1, %Array* }* %19, i64 0, i32 0
  %21 = getelementptr { i1, %Array* }, { i1, %Array* }* %19, i64 0, i32 1
  %22 = load i1, i1* %20
  %23 = load %Array*, %Array** %21
  %24 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  br label %header__4

continue__1:                                      ; preds = %condContinue__1, %exit__3
  %25 = load %Array*, %Array** %ops
  %26 = call i64 @__quantum__rt__array_get_size_1d(%Array* %25)
  %27 = sub i64 %26, 1
  br label %header__5

header__4:                                        ; preds = %exiting__4, %then0__1
  %28 = phi i64 [ 0, %then0__1 ], [ %32, %exiting__4 ]
  %29 = icmp sle i64 %28, 2
  br i1 %29, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %30 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %24, i64 %28)
  %31 = bitcast i8* %30 to i64*
  store i64 0, i64* %31
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %32 = add i64 %28, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  %33 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %23, i64 0)
  %34 = bitcast i8* %33 to %Array**
  call void @__quantum__rt__array_update_reference_count(%Array* %24, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %24, i64 1)
  %35 = load %Array*, %Array** %34
  call void @__quantum__rt__array_update_alias_count(%Array* %35, i64 -1)
  br i1 %22, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %exit__4
  call void @__quantum__rt__array_update_reference_count(%Array* %24, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i64 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %exit__4
  store %Array* %24, %Array** %34
  call void @__quantum__rt__array_update_reference_count(%Array* %23, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %23, i64 1)
  store %Array* %23, %Array** %ops
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %24, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %23, i64 -1)
  br label %continue__1

header__5:                                        ; preds = %exiting__5, %continue__1
  %36 = phi i64 [ 0, %continue__1 ], [ %41, %exiting__5 ]
  %37 = icmp sle i64 %36, %27
  br i1 %37, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %38 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %25, i64 %36)
  %39 = bitcast i8* %38 to %Array**
  %40 = load %Array*, %Array** %39
  call void @__quantum__rt__array_update_alias_count(%Array* %40, i64 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %41 = add i64 %36, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_alias_count(%Array* %25, i64 -1)
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %42 = phi i64 [ 0, %exit__5 ], [ %47, %exiting__6 ]
  %43 = icmp sle i64 %42, 4
  br i1 %43, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %44 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %42)
  %45 = bitcast i8* %44 to %Array**
  %46 = load %Array*, %Array** %45
  call void @__quantum__rt__array_update_reference_count(%Array* %46, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %47 = add i64 %42, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  %48 = sub i64 %26, 1
  br label %header__7

header__7:                                        ; preds = %exiting__7, %exit__6
  %49 = phi i64 [ 0, %exit__6 ], [ %54, %exiting__7 ]
  %50 = icmp sle i64 %49, %48
  br i1 %50, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %51 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %25, i64 %49)
  %52 = bitcast i8* %51 to %Array**
  %53 = load %Array*, %Array** %52
  call void @__quantum__rt__array_update_reference_count(%Array* %53, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %54 = add i64 %49, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_reference_count(%Array* %25, i64 -1)
  ret void
}
