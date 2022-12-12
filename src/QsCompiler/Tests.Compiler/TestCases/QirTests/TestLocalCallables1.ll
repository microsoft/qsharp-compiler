define internal { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__DoNothing__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %2 = bitcast i8* %1 to %Callable**
  store %Callable* %0, %Callable** %2, align 8
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %3 = phi i64 [ 0, %entry ], [ %8, %exiting__1 ]
  %4 = icmp sle i64 %3, 0
  br i1 %4, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %3)
  %6 = bitcast i8* %5 to %Callable**
  %7 = load %Callable*, %Callable** %6, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %7, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %7, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %3, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %9 = call %Callable* @__quantum__rt__callable_copy(%Callable* %0, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 1)
  call void @__quantum__qis__donothing__adj()
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %0, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 1)
  %__controlQubits__ = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 1)
  call void @__quantum__qis__donothing__ctl(%Array* %__controlQubits__, %Tuple* null)
  call void @__quantum__rt__array_update_alias_count(%Array* %__controlQubits__, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %__controlQubits__, i32 -1)
  call void @__quantum__qis__donothing__body()
  %fct = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %fct, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i32 1)
  %11 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @0, i32 0, i32 0))
  %12 = call { %String*, { i64, double }* }* @Microsoft__Quantum__Testing__QIR__ReturnTuple__body(%String* %11)
  %13 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %12, i32 0, i32 0
  %str = load %String*, %String** %13, align 8
  %14 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %12, i32 0, i32 1
  %15 = load { i64, double }*, { i64, double }** %14, align 8
  %16 = getelementptr inbounds { i64, double }, { i64, double }* %15, i32 0, i32 1
  %val = load double, double* %16, align 8
  %ops = call %Array* @Microsoft__Quantum__Testing__QIR__LazyConstruction__body(i1 true)
  %17 = call i64 @__quantum__rt__array_get_size_1d(%Array* %ops)
  %18 = sub i64 %17, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %19 = phi i64 [ 0, %exit__1 ], [ %24, %exiting__2 ]
  %20 = icmp sle i64 %19, %18
  br i1 %20, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %21 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %ops, i64 %19)
  %22 = bitcast i8* %21 to %Callable**
  %23 = load %Callable*, %Callable** %22, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %23, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %23, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %24 = add i64 %19, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %ops, i32 1)
  %25 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %ops, i64 1)
  %26 = bitcast i8* %25 to %Callable**
  %27 = load %Callable*, %Callable** %26, align 8
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64 }* getelementptr ({ i64 }, { i64 }* null, i32 1) to i64))
  %29 = bitcast %Tuple* %28 to { i64 }*
  %30 = getelementptr inbounds { i64 }, { i64 }* %29, i32 0, i32 0
  store i64 2, i64* %30, align 4
  call void @__quantum__rt__callable_invoke(%Callable* %27, %Tuple* %28, %Tuple* null)
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 1)
  %31 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %32 = bitcast %Tuple* %31 to { %String*, double }*
  %33 = getelementptr inbounds { %String*, double }, { %String*, double }* %32, i32 0, i32 0
  %34 = getelementptr inbounds { %String*, double }, { %String*, double }* %32, i32 0, i32 1
  store %String* %str, %String** %33, align 8
  store double %val, double* %34, align 8
  call void @__quantum__rt__callable_make_adjoint(%Callable* %9)
  call void @__quantum__rt__callable_make_controlled(%Callable* %10)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %35 = phi i64 [ 0, %exit__2 ], [ %40, %exiting__3 ]
  %36 = icmp sle i64 %35, 0
  br i1 %36, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %37 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %35)
  %38 = bitcast i8* %37 to %Callable**
  %39 = load %Callable*, %Callable** %38, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %39, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %39, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %40 = add i64 %35, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i32 -1)
  %41 = sub i64 %17, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %42 = phi i64 [ 0, %exit__3 ], [ %47, %exiting__4 ]
  %43 = icmp sle i64 %42, %41
  br i1 %43, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %44 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %ops, i64 %42)
  %45 = bitcast i8* %44 to %Callable**
  %46 = load %Callable*, %Callable** %45, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %46, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %46, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %47 = add i64 %42, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_alias_count(%Array* %ops, i32 -1)
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %48 = phi i64 [ 0, %exit__4 ], [ %53, %exiting__5 ]
  %49 = icmp sle i64 %48, 0
  br i1 %49, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %50 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %48)
  %51 = bitcast i8* %50 to %Callable**
  %52 = load %Callable*, %Callable** %51, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %52, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %52, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %53 = add i64 %48, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 -1)
  %54 = bitcast { i64, double }* %15 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %54, i32 -1)
  %55 = bitcast { %String*, { i64, double }* }* %12 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %55, i32 -1)
  %56 = sub i64 %17, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %57 = phi i64 [ 0, %exit__5 ], [ %62, %exiting__6 ]
  %58 = icmp sle i64 %57, %56
  br i1 %58, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %59 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %ops, i64 %57)
  %60 = bitcast i8* %59 to %Callable**
  %61 = load %Callable*, %Callable** %60, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %61, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %61, i32 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %62 = add i64 %57, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %ops, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %28, i32 -1)
  ret { %String*, double }* %32
}
