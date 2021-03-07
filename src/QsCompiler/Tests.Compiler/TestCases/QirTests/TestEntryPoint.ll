define double @Microsoft__Quantum__Testing__QIR__TestEntryPoint({ i64, %"struct.quantum::Array"* }* %a, i8 %b) #0 {
entry:
  %0 = getelementptr { i64, %"struct.quantum::Array"* }, { i64, %"struct.quantum::Array"* }* %a, i64 0, i32 0
  %1 = getelementptr { i64, %"struct.quantum::Array"* }, { i64, %"struct.quantum::Array"* }* %a, i64 0, i32 1
  %2 = load i64, i64* %0
  %3 = load %"struct.quantum::Array"*, %"struct.quantum::Array"** %1
  %4 = bitcast %"struct.quantum::Array"* %3 to %Array*
  %5 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %2)
  %6 = icmp sgt i64 %2, 0
  br i1 %6, label %copy__1, label %next__1

copy__1:                                          ; preds = %entry
  %7 = ptrtoint %Array* %4 to i64
  %8 = sub i64 %2, 1
  br label %header__1

next__1:                                          ; preds = %exit__1, %entry
  %9 = bitcast i8 %b to i1
  %10 = call double @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %5, i1 %9)
  call void @__quantum__rt__array_update_reference_count(%Array* %5, i64 -1)
  ret double %10

header__1:                                        ; preds = %exiting__1, %copy__1
  %11 = phi i64 [ 0, %copy__1 ], [ %19, %exiting__1 ]
  %12 = icmp sle i64 %11, %8
  br i1 %12, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %13 = mul i64 %11, 8
  %14 = add i64 %7, %13
  %15 = inttoptr i64 %14 to double*
  %16 = load double, double* %15
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %5, i64 %11)
  %18 = bitcast i8* %17 to double*
  store double %16, double* %18
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %19 = add i64 %11, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  br label %next__1
}
