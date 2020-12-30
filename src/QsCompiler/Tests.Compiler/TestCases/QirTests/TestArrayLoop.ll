define { i64, i64 }* @Microsoft__Quantum__Testing__QIR__TestArrayLoop__body(%Array* %a) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %1 = bitcast %Tuple* %0 to { i64, i64 }*
  %2 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 0
  %3 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 1
  store i64 0, i64* %2
  store i64 0, i64* %3
  %4 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 0
  %5 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 1
  %6 = load i64, i64* %4
  %x = alloca i64
  store i64 %6, i64* %x
  %7 = load i64, i64* %5
  %y = alloca i64
  store i64 %7, i64* %y
  %8 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %9 = sub i64 %8, 1
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %10 = phi i64 [ 0, %preheader__1 ], [ %22, %exiting__1 ]
  %11 = icmp sge i64 %10, %9
  %12 = icmp sle i64 %10, %9
  %13 = select i1 true, i1 %12, i1 %11
  br i1 %13, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %10)
  %15 = bitcast i8* %14 to { i64, i64 }**
  %z = load { i64, i64 }*, { i64, i64 }** %15
  %16 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 0
  %17 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 1
  %j = load i64, i64* %16
  %k = load i64, i64* %17
  %18 = load i64, i64* %x
  %19 = add i64 %18, %j
  store i64 %19, i64* %x
  %20 = load i64, i64* %y
  %21 = add i64 %20, %k
  store i64 %21, i64* %y
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %22 = add i64 %10, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %23 = load i64, i64* %x
  %24 = load i64, i64* %y
  %25 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %26 = bitcast %Tuple* %25 to { i64, i64 }*
  %27 = getelementptr { i64, i64 }, { i64, i64 }* %26, i64 0, i32 0
  %28 = getelementptr { i64, i64 }, { i64, i64 }* %26, i64 0, i32 1
  store i64 %23, i64* %27
  store i64 %24, i64* %28
  %29 = bitcast { i64, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %29)
  ret { i64, i64 }* %26
}
