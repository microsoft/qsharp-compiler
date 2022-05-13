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
  call void @__quantum__rt__callable_make_adjoint(%Callable* %9)
  call void @__quantum__rt__callable_invoke(%Callable* %9, %Tuple* null, %Tuple* null)
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %0, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %10)
  %11 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Array*, %Tuple* }* getelementptr ({ %Array*, %Tuple* }, { %Array*, %Tuple* }* null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %Array*, %Tuple* }*
  %14 = getelementptr inbounds { %Array*, %Tuple* }, { %Array*, %Tuple* }* %13, i32 0, i32 0
  %15 = getelementptr inbounds { %Array*, %Tuple* }, { %Array*, %Tuple* }* %13, i32 0, i32 1
  store %Array* %11, %Array** %14, align 8
  store %Tuple* null, %Tuple** %15, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %10, %Tuple* %12, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %0, %Tuple* null, %Tuple* null)
  %fct = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %fct, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i32 1)
  %16 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @0, i32 0, i32 0))
  %17 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String* }* getelementptr ({ %String* }, { %String* }* null, i32 1) to i64))
  %18 = bitcast %Tuple* %17 to { %String* }*
  %19 = getelementptr inbounds { %String* }, { %String* }* %18, i32 0, i32 0
  store %String* %16, %String** %19, align 8
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, { i64, double }* }* getelementptr ({ %String*, { i64, double }* }, { %String*, { i64, double }* }* null, i32 1) to i64))
  call void @__quantum__rt__callable_invoke(%Callable* %fct, %Tuple* %17, %Tuple* %20)
  %21 = bitcast %Tuple* %20 to { %String*, { i64, double }* }*
  %22 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %21, i32 0, i32 0
  %str = load %String*, %String** %22, align 8
  %23 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %21, i32 0, i32 1
  %24 = load { i64, double }*, { i64, double }** %23, align 8
  %25 = getelementptr inbounds { i64, double }, { i64, double }* %24, i32 0, i32 1
  %val = load double, double* %25, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 1)
  %26 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %27 = bitcast %Tuple* %26 to { %String*, double }*
  %28 = getelementptr inbounds { %String*, double }, { %String*, double }* %27, i32 0, i32 0
  %29 = getelementptr inbounds { %String*, double }, { %String*, double }* %27, i32 0, i32 1
  store %String* %str, %String** %28, align 8
  store double %val, double* %29, align 8
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %30 = phi i64 [ 0, %exit__1 ], [ %35, %exiting__2 ]
  %31 = icmp sle i64 %30, 0
  br i1 %31, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %32 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %30)
  %33 = bitcast i8* %32 to %Callable**
  %34 = load %Callable*, %Callable** %33, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %34, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %34, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %35 = add i64 %30, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i32 -1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %36 = phi i64 [ 0, %exit__2 ], [ %41, %exiting__3 ]
  %37 = icmp sle i64 %36, 0
  br i1 %37, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %38 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %36)
  %39 = bitcast i8* %38 to %Callable**
  %40 = load %Callable*, %Callable** %39, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %40, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %40, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %41 = add i64 %36, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %11, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 -1)
  %42 = bitcast { i64, double }* %24 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %42, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %20, i32 -1)
  ret { %String*, double }* %27
}
