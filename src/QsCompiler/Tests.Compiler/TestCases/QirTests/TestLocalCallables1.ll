define { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %1 = bitcast i8* %0 to %Callable**
  %2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__DoNothing, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %2, %Callable** %1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %3 = phi i64 [ 0, %entry ], [ %8, %exiting__1 ]
  %4 = icmp sle i64 %3, 0
  br i1 %4, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %3)
  %6 = bitcast i8* %5 to %Callable**
  %7 = load %Callable*, %Callable** %6
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %7, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %7, i64 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %3, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i64 1)
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %10 = bitcast i8* %9 to %Callable**
  %11 = load %Callable*, %Callable** %10
  %12 = call %Callable* @__quantum__rt__callable_copy(%Callable* %11, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %12, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %12)
  call void @__quantum__rt__callable_invoke(%Callable* %12, %Tuple* null, %Tuple* null)
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %14 = bitcast i8* %13 to %Callable**
  %15 = load %Callable*, %Callable** %14
  %16 = call %Callable* @__quantum__rt__callable_copy(%Callable* %15, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %16, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %16)
  %17 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %18 = bitcast %Tuple* %17 to { %Array*, %Tuple* }*
  %19 = getelementptr inbounds { %Array*, %Tuple* }, { %Array*, %Tuple* }* %18, i32 0, i32 0
  %20 = getelementptr inbounds { %Array*, %Tuple* }, { %Array*, %Tuple* }* %18, i32 0, i32 1
  %21 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  store %Array* %21, %Array** %19
  store %Tuple* null, %Tuple** %20
  call void @__quantum__rt__callable_invoke(%Callable* %16, %Tuple* %17, %Tuple* null)
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %23 = bitcast i8* %22 to %Callable**
  %24 = load %Callable*, %Callable** %23
  call void @__quantum__rt__callable_invoke(%Callable* %24, %Tuple* null, %Tuple* null)
  %fct = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %fct, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i64 1)
  %25 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %26 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %27 = bitcast %Tuple* %26 to { %String* }*
  %28 = getelementptr inbounds { %String* }, { %String* }* %27, i32 0, i32 0
  store %String* %25, %String** %28
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  call void @__quantum__rt__callable_invoke(%Callable* %fct, %Tuple* %26, %Tuple* %29)
  %30 = bitcast %Tuple* %29 to { %String*, { i64, double }* }*
  %31 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %30, i32 0, i32 0
  %str = load %String*, %String** %31
  %32 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %30, i32 0, i32 1
  %33 = load { i64, double }*, { i64, double }** %32
  %34 = getelementptr inbounds { i64, double }, { i64, double }* %33, i32 0, i32 1
  %val = load double, double* %34
  %35 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %36 = bitcast %Tuple* %35 to { %String*, double }*
  %37 = getelementptr inbounds { %String*, double }, { %String*, double }* %36, i32 0, i32 0
  %38 = getelementptr inbounds { %String*, double }, { %String*, double }* %36, i32 0, i32 1
  store %String* %str, %String** %37
  store double %val, double* %38
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %39 = phi i64 [ 0, %exit__1 ], [ %44, %exiting__2 ]
  %40 = icmp sle i64 %39, 0
  br i1 %40, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %41 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %39)
  %42 = bitcast i8* %41 to %Callable**
  %43 = load %Callable*, %Callable** %42
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %43, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %43, i64 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %44 = add i64 %39, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %fct, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i64 -1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %45 = phi i64 [ 0, %exit__2 ], [ %50, %exiting__3 ]
  %46 = icmp sle i64 %45, 0
  br i1 %46, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %47 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %45)
  %48 = bitcast i8* %47 to %Callable**
  %49 = load %Callable*, %Callable** %48
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %49, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %49, i64 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %50 = add i64 %45, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %12, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %12, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %16, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %16, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %fct, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %fct, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i64 -1)
  %51 = bitcast { i64, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %51, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %29, i64 -1)
  ret { %String*, double }* %36
}
