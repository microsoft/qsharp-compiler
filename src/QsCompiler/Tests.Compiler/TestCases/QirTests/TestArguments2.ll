define void @Microsoft__Quantum__Testing__QIR__ArrayArgumentTest({ i64, %ArrayStorage1 } %arr) #0 {
entry:
  %0 = alloca %ArrayStorage1, align 8
  %1 = alloca %ArrayStorage1, align 8
  %squares = alloca { i64, %ArrayStorage1 }, align 8
  %2 = alloca %ArrayStorage1, align 8
  %3 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %4 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  %5 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  %6 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  store %ArrayStorage1 %6, %ArrayStorage1* %2, align 4
  %7 = getelementptr %ArrayStorage1, %ArrayStorage1* %2, i32 0, i32 0, i64 0
  store i64 5, i64* %7, align 4
  %8 = load %ArrayStorage1, %ArrayStorage1* %2, align 4
  %9 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %10 = insertvalue { i64, %ArrayStorage1 } zeroinitializer, i64 %9, 0
  %11 = insertvalue { i64, %ArrayStorage1 } %10, %ArrayStorage1 %8, 1
  store { i64, %ArrayStorage1 } %11, { i64, %ArrayStorage1 }* %squares, align 4
  %12 = extractvalue { i64, %ArrayStorage1 } %arr, 0
  %13 = sub i64 %12, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %idx = phi i64 [ 0, %entry ], [ %32, %exiting__1 ]
  %14 = icmp sle i64 %idx, %13
  br i1 %14, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %15 = load { i64, %ArrayStorage1 }, { i64, %ArrayStorage1 }* %squares, align 4
  %16 = extractvalue { i64, %ArrayStorage1 } %15, 0
  %17 = extractvalue { i64, %ArrayStorage1 } %15, 1
  %18 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  store %ArrayStorage1 %18, %ArrayStorage1* %2, align 4
  %19 = getelementptr %ArrayStorage1, %ArrayStorage1* %2, i32 0, i32 0, i64 %idx
  %20 = load i64, i64* %19, align 4
  %21 = extractvalue { i64, %ArrayStorage1 } %arr, 1
  store %ArrayStorage1 %21, %ArrayStorage1* %2, align 4
  %22 = getelementptr %ArrayStorage1, %ArrayStorage1* %2, i32 0, i32 0, i64 %idx
  %23 = load i64, i64* %22, align 4
  %24 = mul i64 %20, %23
  %25 = extractvalue { i64, %ArrayStorage1 } %15, 1
  %26 = extractvalue { i64, %ArrayStorage1 } %15, 1
  store %ArrayStorage1 %26, %ArrayStorage1* %1, align 4
  %27 = getelementptr %ArrayStorage1, %ArrayStorage1* %1, i32 0, i32 0, i64 %idx
  store i64 %24, i64* %27, align 4
  %28 = load %ArrayStorage1, %ArrayStorage1* %1, align 4
  %29 = extractvalue { i64, %ArrayStorage1 } %15, 0
  %30 = insertvalue { i64, %ArrayStorage1 } zeroinitializer, i64 %29, 0
  %31 = insertvalue { i64, %ArrayStorage1 } %30, %ArrayStorage1 %28, 1
  store { i64, %ArrayStorage1 } %31, { i64, %ArrayStorage1 }* %squares, align 4
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %32 = add i64 %idx, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %33 = load { i64, %ArrayStorage1 }, { i64, %ArrayStorage1 }* %squares, align 4
  %34 = extractvalue { i64, %ArrayStorage1 } %33, 0
  %35 = extractvalue { i64, %ArrayStorage1 } %33, 1
  ret void
}
