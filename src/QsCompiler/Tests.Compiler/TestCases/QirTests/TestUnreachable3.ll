define i64 @Microsoft__Quantum__Testing__QIR__TestUnreachable3__body(i64 %a) {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %results = alloca %Array*, align 8
  store %Array* %0, %Array** %results, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  ret i64 %a

entry__unreachable__1:                            ; No predecessors!
  %1 = sub i64 %a, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry__unreachable__1
  %index = phi i64 [ 0, %entry__unreachable__1 ], [ %8, %exiting__1 ]
  %2 = icmp sle i64 %index, %1
  br i1 %2, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %3 = load %Array*, %Array** %results, align 8
  %4 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 0)
  %6 = bitcast i8* %5 to i64*
  store i64 %index, i64* %6, align 4
  %7 = call %Array* @__quantum__rt__array_concatenate(%Array* %3, %Array* %4)
  call void @__quantum__rt__array_update_reference_count(%Array* %7, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %7, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %3, i32 -1)
  store %Array* %7, %Array** %results, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %7, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %3, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %index, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %9 = load %Array*, %Array** %results, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %9, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i32 -1)
  ret i64 %a
}
