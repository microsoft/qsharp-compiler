define { i64, i64 }* @Microsoft__Quantum__Testing__QIR__TestArrayLoop__body(%Array* %a) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %1 = bitcast %Tuple* %0 to { i64, i64 }*
  %2 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 0
  store i64 0, i64* %2
  %3 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 1
  store i64 0, i64* %3
  %4 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 0
  %5 = load i64, i64* %4
  %x = alloca i64
  store i64 %5, i64* %x
  %6 = getelementptr { i64, i64 }, { i64, i64 }* %1, i64 0, i32 1
  %7 = load i64, i64* %6
  %y = alloca i64
  store i64 %7, i64* %y
  %8 = call i64 @__quantum__rt__array_get_length(%Array* %a, i32 0)
  %end__1 = sub i64 %8, 1
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %iter__1 = phi i64 [ 0, %preheader__1 ], [ %20, %exiting__1 ]
  %9 = icmp sge i64 %iter__1, %end__1
  %10 = icmp sle i64 %iter__1, %end__1
  %11 = select i1 true, i1 %10, i1 %9
  br i1 %11, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %iter__1)
  %13 = bitcast i8* %12 to { i64, i64 }**
  %z = load { i64, i64 }*, { i64, i64 }** %13
  %14 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 0
  %j = load i64, i64* %14
  %15 = getelementptr { i64, i64 }, { i64, i64 }* %z, i64 0, i32 1
  %k = load i64, i64* %15
  %16 = load i64, i64* %x
  %17 = add i64 %16, %j
  store i64 %17, i64* %x
  %18 = load i64, i64* %y
  %19 = add i64 %18, %k
  store i64 %19, i64* %y
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %20 = add i64 %iter__1, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %22 = bitcast %Tuple* %21 to { i64, i64 }*
  %23 = load i64, i64* %x
  %24 = getelementptr { i64, i64 }, { i64, i64 }* %22, i64 0, i32 0
  store i64 %23, i64* %24
  %25 = load i64, i64* %y
  %26 = getelementptr { i64, i64 }, { i64, i64 }* %22, i64 0, i32 1
  store i64 %25, i64* %26
  %27 = bitcast { i64, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %27)
  ret { i64, i64 }* %22
}
