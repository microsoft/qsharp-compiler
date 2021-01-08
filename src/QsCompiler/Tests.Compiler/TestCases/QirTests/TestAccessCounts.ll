define void @Microsoft__Quantum__Testing__QIR__TestAccessCounts__ctl(%Array* %__controlQubits__, { %Array*, { %Array* }* }* %arg__1) {
entry:
  call void @__quantum__rt__array_add_access(%Array* %__controlQubits__)
  %0 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %arg__1, i64 0, i32 0
  %1 = load %Array*, %Array** %0
  %2 = call i64 @__quantum__rt__array_get_size_1d(%Array* %1)
  %3 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %4 = phi i64 [ 0, %entry ], [ %10, %exiting__1 ]
  %5 = icmp sle i64 %4, %3
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %1, i64 %4)
  %7 = bitcast i8* %6 to { double, double }**
  %8 = load { double, double }*, { double, double }** %7
  %9 = bitcast { double, double }* %8 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %9)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %10 = add i64 %4, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_add_access(%Array* %1)
  %11 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %arg__1, i64 0, i32 1
  %12 = load { %Array* }*, { %Array* }** %11
  %13 = getelementptr { %Array* }, { %Array* }* %12, i64 0, i32 0
  %14 = load %Array*, %Array** %13
  call void @__quantum__rt__array_add_access(%Array* %14)
  %15 = bitcast { %Array* }* %12 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %15)
  %16 = bitcast { %Array*, { %Array* }* }* %arg__1 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %16)
  %17 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %arg__1, i64 0, i32 0
  %coefficients = load %Array*, %Array** %17
  %18 = call i64 @__quantum__rt__array_get_size_1d(%Array* %coefficients)
  %19 = sub i64 %18, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %20 = phi i64 [ 0, %exit__1 ], [ %26, %exiting__2 ]
  %21 = icmp sle i64 %20, %19
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %20)
  %23 = bitcast i8* %22 to { double, double }**
  %24 = load { double, double }*, { double, double }** %23
  %25 = bitcast { double, double }* %24 to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %25)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %26 = add i64 %20, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_add_access(%Array* %coefficients)
  %27 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %arg__1, i64 0, i32 1
  %qubits = load { %Array* }*, { %Array* }** %27
  %28 = getelementptr { %Array* }, { %Array* }* %qubits, i64 0, i32 0
  %29 = load %Array*, %Array** %28
  call void @__quantum__rt__array_add_access(%Array* %29)
  %30 = bitcast { %Array* }* %qubits to %Tuple*
  call void @__quantum__rt__tuple_add_access(%Tuple* %30)
  %31 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %Array*, { %Array* }* }* getelementptr ({ double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* null, i32 1) to i64))
  %32 = bitcast %Tuple* %31 to { double, %Array*, { %Array* }* }*
  %33 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %32, i64 0, i32 0
  %34 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %32, i64 0, i32 1
  %35 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %32, i64 0, i32 2
  store double 0.000000e+00, double* %33
  store %Array* %coefficients, %Array** %34
  %36 = sub i64 %18, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %37 = phi i64 [ 0, %exit__2 ], [ %43, %exiting__3 ]
  %38 = icmp sle i64 %37, %36
  br i1 %38, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %39 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %37)
  %40 = bitcast i8* %39 to { double, double }**
  %41 = load { double, double }*, { double, double }** %40
  %42 = bitcast { double, double }* %41 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %42)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %43 = add i64 %37, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_reference(%Array* %coefficients)
  store { %Array* }* %qubits, { %Array* }** %35
  %44 = getelementptr { %Array* }, { %Array* }* %qubits, i64 0, i32 0
  %45 = load %Array*, %Array** %44
  call void @__quantum__rt__array_reference(%Array* %45)
  call void @__quantum__rt__tuple_reference(%Tuple* %30)
  call void @Microsoft__Quantum__Testing__QIR__ApplyOp__ctl(%Array* %__controlQubits__, { double, %Array*, { %Array* }* }* %32)
  call void @__quantum__rt__array_remove_access(%Array* %__controlQubits__)
  %46 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %arg__1, i64 0, i32 0
  %47 = load %Array*, %Array** %46
  %48 = call i64 @__quantum__rt__array_get_size_1d(%Array* %47)
  %49 = sub i64 %48, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %50 = phi i64 [ 0, %exit__3 ], [ %56, %exiting__4 ]
  %51 = icmp sle i64 %50, %49
  br i1 %51, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %52 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %47, i64 %50)
  %53 = bitcast i8* %52 to { double, double }**
  %54 = load { double, double }*, { double, double }** %53
  %55 = bitcast { double, double }* %54 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %55)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %56 = add i64 %50, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_remove_access(%Array* %47)
  %57 = getelementptr { %Array*, { %Array* }* }, { %Array*, { %Array* }* }* %arg__1, i64 0, i32 1
  %58 = load { %Array* }*, { %Array* }** %57
  %59 = getelementptr { %Array* }, { %Array* }* %58, i64 0, i32 0
  %60 = load %Array*, %Array** %59
  call void @__quantum__rt__array_remove_access(%Array* %60)
  %61 = bitcast { %Array* }* %58 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %61)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %16)
  %62 = sub i64 %18, 1
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %63 = phi i64 [ 0, %exit__4 ], [ %69, %exiting__5 ]
  %64 = icmp sle i64 %63, %62
  br i1 %64, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %65 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %coefficients, i64 %63)
  %66 = bitcast i8* %65 to { double, double }**
  %67 = load { double, double }*, { double, double }** %66
  %68 = bitcast { double, double }* %67 to %Tuple*
  call void @__quantum__rt__tuple_remove_access(%Tuple* %68)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %69 = add i64 %63, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_remove_access(%Array* %coefficients)
  %70 = getelementptr { %Array* }, { %Array* }* %qubits, i64 0, i32 0
  %71 = load %Array*, %Array** %70
  call void @__quantum__rt__array_remove_access(%Array* %71)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %30)
  %72 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %32, i64 0, i32 1
  %73 = load %Array*, %Array** %72
  %74 = call i64 @__quantum__rt__array_get_size_1d(%Array* %73)
  %75 = sub i64 %74, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %76 = phi i64 [ 0, %exit__5 ], [ %82, %exiting__6 ]
  %77 = icmp sle i64 %76, %75
  br i1 %77, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %78 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %73, i64 %76)
  %79 = bitcast i8* %78 to { double, double }**
  %80 = load { double, double }*, { double, double }** %79
  %81 = bitcast { double, double }* %80 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %81)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %82 = add i64 %76, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_unreference(%Array* %73)
  %83 = getelementptr { double, %Array*, { %Array* }* }, { double, %Array*, { %Array* }* }* %32, i64 0, i32 2
  %84 = load { %Array* }*, { %Array* }** %83
  %85 = getelementptr { %Array* }, { %Array* }* %84, i64 0, i32 0
  %86 = load %Array*, %Array** %85
  call void @__quantum__rt__array_unreference(%Array* %86)
  %87 = bitcast { %Array* }* %84 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %87)
  call void @__quantum__rt__tuple_unreference(%Tuple* %31)
  ret void
}
