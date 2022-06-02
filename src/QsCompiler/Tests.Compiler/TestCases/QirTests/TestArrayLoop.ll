define internal { i64, i64 }* @Microsoft__Quantum__Testing__QIR__TestArrayLoop__body(%Array* %a) {
entry:
  %y = alloca i64, align 8
  %x = alloca i64, align 8
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %1 = sub i64 %0, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %8, %exiting__1 ]
  %3 = icmp sle i64 %2, %1
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %2)
  %5 = bitcast i8* %4 to { i64, i64 }**
  %6 = load { i64, i64 }*, { i64, i64 }** %5, align 8
  %7 = bitcast { i64, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %7, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i32 1)
  store i64 0, i64* %x, align 4
  store i64 0, i64* %y, align 4
  %9 = sub i64 %0, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %10 = phi i64 [ 0, %exit__1 ], [ %21, %exiting__2 ]
  %11 = icmp sle i64 %10, %9
  br i1 %11, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %10)
  %13 = bitcast i8* %12 to { i64, i64 }**
  %z = load { i64, i64 }*, { i64, i64 }** %13, align 8
  %14 = bitcast { i64, i64 }* %z to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 1)
  %15 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %z, i32 0, i32 0
  %j = load i64, i64* %15, align 4
  %16 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %z, i32 0, i32 1
  %k = load i64, i64* %16, align 4
  %17 = load i64, i64* %x, align 4
  %18 = add i64 %17, %j
  store i64 %18, i64* %x, align 4
  %19 = load i64, i64* %y, align 4
  %20 = add i64 %19, %k
  store i64 %20, i64* %y, align 4
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %21 = add i64 %10, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %22 = call %Result* @__quantum__rt__result_get_zero()
  %23 = call %Result* @__quantum__rt__result_get_one()
  %24 = call i1 @__quantum__rt__result_equal(%Result* %22, %Result* %23)
  %25 = select i1 %24, i64 1, i64 3
  %sizedArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %25)
  %26 = sub i64 %25, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %27 = phi i64 [ 0, %exit__2 ], [ %31, %exiting__3 ]
  %28 = icmp sle i64 %27, %26
  br i1 %28, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %29 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %sizedArr, i64 %27)
  %30 = bitcast i8* %29 to i64*
  store i64 3, i64* %30, align 4
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %31 = add i64 %27, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %sizedArr, i32 1)
  %32 = load i64, i64* %x, align 4
  %33 = load i64, i64* %y, align 4
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i64 }* getelementptr ({ i64, i64 }, { i64, i64 }* null, i32 1) to i64))
  %35 = bitcast %Tuple* %34 to { i64, i64 }*
  %36 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %35, i32 0, i32 0
  %37 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %35, i32 0, i32 1
  store i64 %32, i64* %36, align 4
  store i64 %33, i64* %37, align 4
  %38 = sub i64 %0, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %39 = phi i64 [ 0, %exit__3 ], [ %45, %exiting__4 ]
  %40 = icmp sle i64 %39, %38
  br i1 %40, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %41 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %39)
  %42 = bitcast i8* %41 to { i64, i64 }**
  %43 = load { i64, i64 }*, { i64, i64 }** %42, align 8
  %44 = bitcast { i64, i64 }* %43 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %44, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %45 = add i64 %39, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %sizedArr, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %sizedArr, i32 -1)
  ret { i64, i64 }* %35
}
