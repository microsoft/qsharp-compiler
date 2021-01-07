define { i64, i64 }* @Microsoft__Quantum__Testing__QIR__TestArrayLoop__body(%Array* %a) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %a)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %1 = bitcast %Tuple* %0 to { i64, i64 }*
  %2 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 0
  %3 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 1
  store i64 0, i64* %2
  store i64 0, i64* %3
  %4 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 0
  %5 = load i64, i64* %4
  %x = alloca i64
  store i64 %5, i64* %x
  %6 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 1
  %7 = load i64, i64* %6
  %y = alloca i64
  store i64 %7, i64* %y
  %8 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %9 = sub i64 %8, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %10 = phi i64 [ 0, %entry ], [ %21, %exiting__1 ]
  %11 = icmp sle i64 %10, %9
  br i1 %11, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %10)
  %13 = bitcast i8* %12 to { i64, i64 }**
  %z = load { i64, i64 }*, { i64, i64 }** %13
  %14 = bitcast { i64, i64 }* %z to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %14)
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
  call void @__quantum__rt__tuple_remove_access(%Tuple* %14)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %21 = add i64 %10, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %22 = load i64, i64* %x
  %23 = load i64, i64* %y
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %25 = bitcast %Tuple* %24 to { i64, i64 }*
  %26 = getelementptr { i64, i64 }, { i64, i64 }* %25, i64 0, i32 0
  %27 = getelementptr { i64, i64 }, { i64, i64 }* %25, i64 0, i32 1
  store i64 %22, i64* %26
  store i64 %23, i64* %27
  call void @__quantum__rt__array_remove_access(%Array* %a)
  call void @__quantum__rt__tuple_unreference(%Tuple* %0)
  ret { i64, i64 }* %25
}
