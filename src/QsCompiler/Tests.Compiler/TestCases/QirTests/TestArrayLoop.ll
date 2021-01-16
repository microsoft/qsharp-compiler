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
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %7, i64 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_access_count(%Array* %a, i64 1)
  %x = alloca i64
  store i64 0, i64* %x
  %y = alloca i64
  store i64 0, i64* %y
  %9 = sub i64 %0, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %10 = phi i64 [ 0, %exit__1 ], [ %21, %exiting__2 ]
  %11 = icmp sle i64 %10, %9
  br i1 %11, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %10)
  %13 = bitcast i8* %12 to { i64, i64 }**
  %z = load { i64, i64 }*, { i64, i64 }** %13
  %14 = bitcast { i64, i64 }* %z to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %14, i64 1)
  %15 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 0
  %j = load i64, i64* %15
  %16 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 1
  %k = load i64, i64* %16
  %17 = load i64, i64* %x
  %18 = add i64 %17, %j
  store i64 %18, i64* %x
  %19 = load i64, i64* %y
  %20 = add i64 %19, %k
  store i64 %20, i64* %y
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %14, i64 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %21 = add i64 %10, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %23 = bitcast %Tuple* %22 to { i64, i64 }*
  %24 = getelementptr { i64, i64 }, { i64, i64 }* %23, i64 0, i32 0
  %25 = getelementptr { i64, i64 }, { i64, i64 }* %23, i64 0, i32 1
  %26 = load i64, i64* %x
  %27 = load i64, i64* %y
  store i64 %26, i64* %24
  store i64 %27, i64* %25
  %28 = sub i64 %0, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %29 = phi i64 [ 0, %exit__2 ], [ %35, %exiting__3 ]
  %30 = icmp sle i64 %29, %28
  br i1 %30, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %29)
  %32 = bitcast i8* %31 to { i64, i64 }**
  %33 = load { i64, i64 }*, { i64, i64 }** %32
  %34 = bitcast { i64, i64 }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %34, i64 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %35 = add i64 %29, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_access_count(%Array* %a, i64 -1)
  ret { i64, i64 }* %23
}
