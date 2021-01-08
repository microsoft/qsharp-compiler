define { i64, i64 }* @Microsoft__Quantum__Testing__QIR__TestArrayLoop__body(%Array* %a) {
entry:
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
  %6 = load { i64, i64 }*, { i64, i64 }** %5
  %7 = bitcast { i64, i64 }* %6 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %7)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_add_access(%Array* %a)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %10 = bitcast %Tuple* %9 to { i64, i64 }*
  %11 = getelementptr { i64, i64 }, { i64, i64 }* %10, i64 0, i32 0
  %12 = getelementptr { i64, i64 }, { i64, i64 }* %10, i64 0, i32 1
  store i64 0, i64* %11
  store i64 0, i64* %12
  %13 = getelementptr { i64, i64 }, { i64, i64 }* %10, i64 0, i32 0
  %14 = load i64, i64* %13
  %x = alloca i64
  store i64 %14, i64* %x
  %15 = getelementptr { i64, i64 }, { i64, i64 }* %10, i64 0, i32 1
  %16 = load i64, i64* %15
  %y = alloca i64
  store i64 %16, i64* %y
  %17 = sub i64 %0, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %18 = phi i64 [ 0, %exit__1 ], [ %29, %exiting__2 ]
  %19 = icmp sle i64 %18, %17
  br i1 %19, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %18)
  %21 = bitcast i8* %20 to { i64, i64 }**
  %z = load { i64, i64 }*, { i64, i64 }** %21
  %22 = bitcast { i64, i64 }* %z to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %22)
  %23 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 0
  %j = load i64, i64* %23
  %24 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 1
  %k = load i64, i64* %24
  %25 = load i64, i64* %x
  %26 = add i64 %25, %j
  store i64 %26, i64* %x
  %27 = load i64, i64* %y
  %28 = add i64 %27, %k
  store i64 %28, i64* %y
  call void @__quantum__rt__tuple_remove_access(%Tuple* %22)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %29 = add i64 %18, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %30 = load i64, i64* %x
  %31 = load i64, i64* %y
  %32 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %33 = bitcast %Tuple* %32 to { i64, i64 }*
  %34 = getelementptr { i64, i64 }, { i64, i64 }* %33, i64 0, i32 0
  %35 = getelementptr { i64, i64 }, { i64, i64 }* %33, i64 0, i32 1
  store i64 %30, i64* %34
  store i64 %31, i64* %35
  %36 = sub i64 %0, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %37 = phi i64 [ 0, %exit__2 ], [ %43, %exiting__3 ]
  %38 = icmp sle i64 %37, %36
  br i1 %38, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %39 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %37)
  %40 = bitcast i8* %39 to { i64, i64 }**
  %41 = load { i64, i64 }*, { i64, i64 }** %40
  %42 = bitcast { i64, i64 }* %41 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %42)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %43 = add i64 %37, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_remove_access(%Array* %a)
  call void @__quantum__rt__tuple_unreference(%Tuple* %9)
  ret { i64, i64 }* %33
}
