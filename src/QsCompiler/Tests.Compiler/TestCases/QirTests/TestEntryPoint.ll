define double @Microsoft__Quantum__Testing__QIR__TestEntryPoint(i64 %a__count, double* %a, i1 %b) #0 {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %a__count)
  %1 = icmp sgt i64 %a__count, 0
  br i1 %1, label %copy, label %next

copy:                                             ; preds = %entry
  %2 = ptrtoint double* %a to i64
  %3 = sub i64 %a__count, 1
  br label %header__1

next:                                             ; preds = %exit__1, %entry
  %4 = call double @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %0, i1 %b)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  ret double %4

header__1:                                        ; preds = %exiting__1, %copy
  %5 = phi i64 [ 0, %copy ], [ %13, %exiting__1 ]
  %6 = icmp sle i64 %5, %3
  br i1 %6, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %7 = mul i64 %5, 8
  %8 = add i64 %2, %7
  %9 = inttoptr i64 %8 to double*
  %10 = load double, double* %9, align 8
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %5)
  %12 = bitcast i8* %11 to double*
  store double %10, double* %12, align 8
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %13 = add i64 %5, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  br label %next
}
