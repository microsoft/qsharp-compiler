define double @Microsoft__Quantum__Testing__QIR__TestEntryPoint(%"struct.quantum::Tuple"* %a, i1 %b) #0 {
entry:
  %0 = bitcast %"struct.quantum::Tuple"* %a to { i64, %"struct.quantum::Array"* }*
  %1 = getelementptr { i64, %"struct.quantum::Array"* }, { i64, %"struct.quantum::Array"* }* %0, i64 0, i32 0
  %2 = getelementptr { i64, %"struct.quantum::Array"* }, { i64, %"struct.quantum::Array"* }* %0, i64 0, i32 1
  %3 = load i64, i64* %1
  %4 = load %"struct.quantum::Array"*, %"struct.quantum::Array"** %2
  %5 = bitcast %"struct.quantum::Array"* %4 to %Array*
  %6 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %3)
  %7 = icmp sgt i64 %3, 0
  br i1 %7, label %copy__1, label %next__1

copy__1:                                          ; preds = %entry
  %8 = ptrtoint %Array* %5 to i64
  %9 = sub i64 %3, 1
  br label %header__1

next__1:                                          ; preds = %exit__1, %entry
  %10 = call double @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %6, i1 %b)
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i64 -1)
  ret double %10

header__1:                                        ; preds = %exiting__1, %copy__1
  %11 = phi i64 [ 0, %copy__1 ], [ %19, %exiting__1 ]
  %12 = icmp sle i64 %11, %9
  br i1 %12, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %13 = mul i64 %11, 8
  %14 = add i64 %8, %13
  %15 = inttoptr i64 %14 to double*
  %16 = load double, double* %15
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 %11)
  %18 = bitcast i8* %17 to double*
  store double %16, double* %18
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %19 = add i64 %11, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  br label %next__1
}
