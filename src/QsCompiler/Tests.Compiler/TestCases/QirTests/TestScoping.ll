define i64 @Microsoft__Quantum__Testing__QIR__TestScoping__body(%Array* %a) {
entry:
  %sum = alloca i64
  store i64 0, i64* %sum
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %1 = sub i64 %0, 1
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %2 = phi i64 [ 0, %preheader__1 ], [ %13, %exiting__1 ]
  %3 = icmp sge i64 %2, %1
  %4 = icmp sle i64 %2, %1
  %5 = select i1 true, i1 %4, i1 %3
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %2)
  %7 = bitcast i8* %6 to i64*
  %i = load i64, i64* %7
  %8 = icmp sgt i64 %i, 5
  br i1 %8, label %then0__1, label %else__1

then0__1:                                         ; preds = %body__1
  %x = add i64 %i, 3
  %9 = load i64, i64* %sum
  %10 = add i64 %9, %x
  store i64 %10, i64* %sum
  br label %continue__1

else__1:                                          ; preds = %body__1
  %x1 = mul i64 %i, 2
  %11 = load i64, i64* %sum
  %12 = add i64 %11, %x1
  store i64 %12, i64* %sum
  br label %continue__1

continue__1:                                      ; preds = %else__1, %then0__1
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %13 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %14 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %15 = sub i64 %14, 1
  br label %preheader__2

preheader__2:                                     ; preds = %exit__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__2
  %16 = phi i64 [ 0, %preheader__2 ], [ %24, %exiting__2 ]
  %17 = icmp sge i64 %16, %15
  %18 = icmp sle i64 %16, %15
  %19 = select i1 true, i1 %18, i1 %17
  br i1 %19, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %16)
  %21 = bitcast i8* %20 to i64*
  %i2 = load i64, i64* %21
  %22 = load i64, i64* %sum
  %23 = add i64 %22, %i2
  store i64 %23, i64* %sum
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %24 = add i64 %16, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %25 = load i64, i64* %sum
  ret i64 %25
}
