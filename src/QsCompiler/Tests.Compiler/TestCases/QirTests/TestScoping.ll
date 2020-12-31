define i64 @Microsoft__Quantum__Testing__QIR__TestScoping__body(%Array* %a) {
entry:
  %sum = alloca i64
  store i64 0, i64* %sum
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %1 = sub i64 %0, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %11, %exiting__1 ]
  %3 = icmp sle i64 %2, %1
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %2)
  %5 = bitcast i8* %4 to i64*
  %i = load i64, i64* %5
  %6 = icmp sgt i64 %i, 5
  br i1 %6, label %then0__1, label %else__1

then0__1:                                         ; preds = %body__1
  %x = add i64 %i, 3
  %7 = load i64, i64* %sum
  %8 = add i64 %7, %x
  store i64 %8, i64* %sum
  br label %continue__1

else__1:                                          ; preds = %body__1
  %x1 = mul i64 %i, 2
  %9 = load i64, i64* %sum
  %10 = add i64 %9, %x1
  store i64 %10, i64* %sum
  br label %continue__1

continue__1:                                      ; preds = %else__1, %then0__1
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %11 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %12 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %13 = sub i64 %12, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %14 = phi i64 [ 0, %exit__1 ], [ %20, %exiting__2 ]
  %15 = icmp sle i64 %14, %13
  br i1 %15, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %14)
  %17 = bitcast i8* %16 to i64*
  %i2 = load i64, i64* %17
  %18 = load i64, i64* %sum
  %19 = add i64 %18, %i2
  store i64 %19, i64* %sum
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %20 = add i64 %14, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %21 = load i64, i64* %sum
  ret i64 %21
}
