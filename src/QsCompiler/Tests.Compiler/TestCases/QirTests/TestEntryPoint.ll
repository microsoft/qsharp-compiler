define double @Microsoft__Quantum__Testing__QIR__TestEntryPoint({ i64, i8* }* %a, i8 %b) #0 {
entry:
  %0 = getelementptr { i64, i8* }, { i64, i8* }* %a, i64 0, i32 0
  %1 = getelementptr { i64, i8* }, { i64, i8* }* %a, i64 0, i32 1
  %2 = load i64, i64* %0, align 4
  %3 = load i8*, i8** %1, align 8
  %4 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %2)
  %5 = ptrtoint i8* %3 to i64
  %6 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %7 = phi i64 [ 0, %entry ], [ %15, %exiting__1 ]
  %8 = icmp sle i64 %7, %6
  br i1 %8, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %9 = mul i64 %7, 8
  %10 = add i64 %5, %9
  %11 = inttoptr i64 %10 to double*
  %12 = load double, double* %11, align 8
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 %7)
  %14 = bitcast i8* %13 to double*
  store double %12, double* %14, align 8
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %15 = add i64 %7, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %16 = bitcast i8 %b to i1
  %17 = call double @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, i1 %16)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i64 -1)
  ret double %17
}
