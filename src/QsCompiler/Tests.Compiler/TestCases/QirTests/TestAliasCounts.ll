define void @Microsoft__Quantum__Testing__QIR__TestAliasCounts__ctl(%Array* %__controlQubits__, { %Array*, { %Array* }* }* %0) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 1)
  %1 = getelementptr inbounds { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i32 0, i32 0
  %coefficients = load %Array*, %Array** %1, align 8
  %2 = call i64 @__quantum__rt__array_get_size_1d(%Array* %coefficients)
  %3 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %4 = phi i64 [ 0, %entry ], [ %10, %exiting__1 ]
  %5 = icmp sle i64 %4, %3
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %4)
  %7 = bitcast i8* %6 to { double, double }**
  %8 = load { double, double }*, { double, double }** %7, align 8
  %9 = bitcast { double, double }* %8 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %10 = add i64 %4, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %coefficients, i32 1)
  %11 = getelementptr inbounds { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i32 0, i32 1
  %qubits = load { %Array* }*, { %Array* }** %11, align 8
  %12 = getelementptr inbounds { %Array* }, { %Array* }* %qubits, i32 0, i32 0
  %13 = load %Array*, %Array** %12, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %13, i32 1)
  %14 = bitcast { %Array* }* %qubits to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 1)
  %15 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %Array*, { %Array* }* }* getelementptr ({ double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* null, i32 1) to i64))
  %16 = bitcast %Tuple* %15 to { double, %Array*, { %Array* }* }*
  %17 = getelementptr inbounds { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i32 0, i32 0
  %18 = getelementptr inbounds { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i32 0, i32 1
  %19 = getelementptr inbounds { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i32 0, i32 2
  %20 = sub i64 %2, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %21 = phi i64 [ 0, %exit__1 ], [ %27, %exiting__2 ]
  %22 = icmp sle i64 %21, %20
  br i1 %22, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %23 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %21)
  %24 = bitcast i8* %23 to { double, double }**
  %25 = load { double, double }*, { double, double }** %24, align 8
  %26 = bitcast { double, double }* %25 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %27 = add i64 %21, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %coefficients, i32 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %13, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 1)
  store double 0.000000e+00, double* %17, align 8
  store %Array* %coefficients, %Array** %18, align 8
  store { %Array* }* %qubits, { %Array* }** %19, align 8
  call void @Microsoft__Quantum__Testing__QIR__ApplyOp__ctl(%Array* %__controlQubits__, { double, %Array*, { %Array* }* }* %16)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 -1)
  %28 = sub i64 %2, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %29 = phi i64 [ 0, %exit__2 ], [ %35, %exiting__3 ]
  %30 = icmp sle i64 %29, %28
  br i1 %30, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %29)
  %32 = bitcast i8* %31 to { double, double }**
  %33 = load { double, double }*, { double, double }** %32, align 8
  %34 = bitcast { double, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %34, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %35 = add i64 %29, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %coefficients, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %13, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 -1)
  %36 = sub i64 %2, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %37 = phi i64 [ 0, %exit__3 ], [ %43, %exiting__4 ]
  %38 = icmp sle i64 %37, %36
  br i1 %38, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %39 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %37)
  %40 = bitcast i8* %39 to { double, double }**
  %41 = load { double, double }*, { double, double }** %40, align 8
  %42 = bitcast { double, double }* %41 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %42, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %43 = add i64 %37, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %coefficients, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %13, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i32 -1)
  ret void
}
