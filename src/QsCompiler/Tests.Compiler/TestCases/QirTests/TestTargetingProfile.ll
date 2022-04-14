define internal void @Microsoft__Quantum__Testing__QIR__TestProfileTargeting__body() {
entry:
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %1 = bitcast i8* %0 to i64*
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 1)
  %3 = bitcast i8* %2 to i64*
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 2)
  %5 = bitcast i8* %4 to i64*
  store i64 1, i64* %1, align 4
  store i64 2, i64* %3, align 4
  store i64 3, i64* %5, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %sum = alloca i64, align 8
  store i64 0, i64* %sum, align 4
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %6 = phi i64 [ 0, %entry ], [ %12, %exiting__1 ]
  %7 = icmp sle i64 %6, 2
  br i1 %7, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %6)
  %9 = bitcast i8* %8 to i64*
  %item = load i64, i64* %9, align 4
  %10 = load i64, i64* %sum, align 4
  %11 = add i64 %10, %item
  store i64 %11, i64* %sum, align 4
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %12 = add i64 %6, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %sum__1 = load i64, i64* %sum, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  ret void
}
