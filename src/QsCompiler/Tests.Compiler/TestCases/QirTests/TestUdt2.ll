define internal void @Microsoft__Quantum__Testing__QIR__Main__body() {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i2, i64 }* getelementptr ({ i2, i64 }, { i2, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { i2, i64 }*
  %2 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %1, i32 0, i32 1
  store i2 0, i2* %2, align 1
  store i64 0, i64* %3, align 4
  %4 = call { { i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType__body({ i2, i64 }* %1, double 0.000000e+00)
  %5 = call { %Tuple* }* @Microsoft__Quantum__Testing__QIR__Foo__body()
  %6 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 0)
  %8 = bitcast i8* %7 to { %Tuple* }**
  store { %Tuple* }* %5, { %Tuple* }** %8, align 8
  %9 = call { i64 }* @Microsoft__Quantum__Testing__QIR__Bar__body(i64 1)
  %10 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %10, i64 0)
  %12 = bitcast i8* %11 to { i64 }**
  store { i64 }* %9, { i64 }** %12, align 8
  %13 = getelementptr inbounds { { i2, i64 }*, double }, { { i2, i64 }*, double }* %4, i32 0, i32 0
  %14 = load { i2, i64 }*, { i2, i64 }** %13, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  %15 = bitcast { i2, i64 }* %14 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i32 -1)
  %16 = bitcast { { i2, i64 }*, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 -1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %17 = phi i64 [ 0, %entry ], [ %23, %exiting__1 ]
  %18 = icmp sle i64 %17, 0
  br i1 %18, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %19 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %6, i64 %17)
  %20 = bitcast i8* %19 to { %Tuple* }**
  %21 = load { %Tuple* }*, { %Tuple* }** %20, align 8
  %22 = bitcast { %Tuple* }* %21 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %23 = add i64 %17, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i32 -1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %24 = phi i64 [ 0, %exit__1 ], [ %30, %exiting__2 ]
  %25 = icmp sle i64 %24, 0
  br i1 %25, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %10, i64 %24)
  %27 = bitcast i8* %26 to { i64 }**
  %28 = load { i64 }*, { i64 }** %27, align 8
  %29 = bitcast { i64 }* %28 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %29, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %30 = add i64 %24, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %10, i32 -1)
  ret void
}
