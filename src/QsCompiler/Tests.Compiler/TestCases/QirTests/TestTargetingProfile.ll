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
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %7 = bitcast i8* %6 to i64*
  %8 = load i64, i64* %7, align 4
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %10 = bitcast i8* %9 to i64*
  %item = load i64, i64* %10, align 4
  %11 = add i64 0, %item
  store i64 %11, i64* %sum, align 4
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 1)
  %13 = bitcast i8* %12 to i64*
  %14 = load i64, i64* %13, align 4
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 1)
  %16 = bitcast i8* %15 to i64*
  %item__1 = load i64, i64* %16, align 4
  %17 = add i64 %11, %item__1
  store i64 %17, i64* %sum, align 4
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 2)
  %19 = bitcast i8* %18 to i64*
  %20 = load i64, i64* %19, align 4
  %21 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 2)
  %22 = bitcast i8* %21 to i64*
  %item__2 = load i64, i64* %22, align 4
  %sum__1 = add i64 %17, %item__2
  store i64 %sum__1, i64* %sum, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  ret void
}
