define void @Microsoft__Quantum__Testing__QIR__TestAccessCounts__ctl(%Array* %__controlQubits__, { %Array*, { %Array* }* }* %0) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits__)
  %1 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i64 0, i32 0
  %coefficients = load %Array*, %Array** %1
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
  %8 = load { double, double }*, { double, double }** %7
  %9 = bitcast { double, double }* %8 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %10 = add i64 %4, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_add_access(%Array* %coefficients)
  %11 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %0, i64 0, i32 1
  %qubits = load { %Array* }*, { %Array* }** %11
  %12 = getelementptr { %Array* }, { %Array* }* %qubits, i64 0, i32 0
  %13 = load %Array*, %Array** %12
  call void @__quantum__rt__array_add_access(%Array* %13)
  %14 = bitcast { %Array* }* %qubits to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %14)
  %15 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %Array*, { %Array* }* }* getelementptr ({ double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* null, i32 1) to i64))
  %16 = bitcast %Tuple* %15 to { double, %Array*, { %Array* }* }*
  %17 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i64 0, i32 0
  %18 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i64 0, i32 1
  %19 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i64 0, i32 2
  store double 0.000000e+00, double* %17
  store %Array* %coefficients, %Array** %18
  %20 = sub i64 %2, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %21 = phi i64 [ 0, %exit__1 ], [ %27, %exiting__2 ]
  %22 = icmp sle i64 %21, %20
  br i1 %22, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %23 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %21)
  %24 = bitcast i8* %23 to { double, double }**
  %25 = load { double, double }*, { double, double }** %24
  %26 = bitcast { double, double }* %25 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %26)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %27 = add i64 %21, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_reference(%Array* %coefficients)
  store { %Array* }* %qubits, { %Array* }** %19
  %28 = getelementptr { %Array* }, { %Array* }* %qubits, i64 0, i32 0
  %29 = load %Array*, %Array** %28
  call void @__quantum__rt__array_reference(%Array* %29)
  call void @__quantum__rt__tuple_reference(%Tuple* %14)
  call void @Microsoft__Quantum__Testing__QIR__ApplyOp__ctl(%Array* %__controlQubits__, { double, %Array*, { %Array* }* }* %16)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits__)
  %30 = sub i64 %2, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %31 = phi i64 [ 0, %exit__2 ], [ %37, %exiting__3 ]
  %32 = icmp sle i64 %31, %30
  br i1 %32, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %33 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %31)
  %34 = bitcast i8* %33 to { double, double }**
  %35 = load { double, double }*, { double, double }** %34
  %36 = bitcast { double, double }* %35 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %36)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %37 = add i64 %31, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_remove_access(%Array* %coefficients)
  %38 = getelementptr { %Array* }, { %Array* }* %qubits, i64 0, i32 0
  %39 = load %Array*, %Array** %38
  call void @__quantum__rt__array_remove_access(%Array* %39)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %14)
  %40 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i64 0, i32 1
  %41 = load %Array*, %Array** %40
  %42 = call i64 @__quantum__rt__array_get_size_1d(%Array* %41)
  %43 = sub i64 %42, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %44 = phi i64 [ 0, %exit__3 ], [ %50, %exiting__4 ]
  %45 = icmp sle i64 %44, %43
  br i1 %45, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %46 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %41, i64 %44)
  %47 = bitcast i8* %46 to { double, double }**
  %48 = load { double, double }*, { double, double }** %47
  %49 = bitcast { double, double }* %48 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %49)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %50 = add i64 %44, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_unreference(%Array* %41)
  %51 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %16, i64 0, i32 2
  %52 = load { %Array* }*, { %Array* }** %51
  %53 = getelementptr { %Array* }, { %Array* }* %52, i64 0, i32 0
  %54 = load %Array*, %Array** %53
  call void @__quantum__rt__array_unreference(%Array* %54)
  %55 = bitcast { %Array* }* %52 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %55)
  call void @__quantum__rt__tuple_unreference(%Tuple* %15)
  ret void
}
