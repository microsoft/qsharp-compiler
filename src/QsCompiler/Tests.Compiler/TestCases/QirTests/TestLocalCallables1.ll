define { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %1 = bitcast i8* %0 to %Callable**
  %2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__DoNothing, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %2, %Callable** %1
  call void @__quantum__rt__array_update_access_count(%Array* %arr, i64 1)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %4 = bitcast i8* %3 to %Callable**
  %5 = load %Callable*, %Callable** %4
  %6 = call %Callable* @__quantum__rt__callable_copy(%Callable* %5, i1 true)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %6, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %6)
  call void @__quantum__rt__callable_invoke(%Callable* %6, %Tuple* null, %Tuple* null)
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %8 = bitcast i8* %7 to %Callable**
  %9 = load %Callable*, %Callable** %8
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %9, i1 true)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %10, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %10)
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %12 = bitcast %Tuple* %11 to { %Array*, %Tuple* }*
  %13 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %12, i64 0, i32 0
  %14 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %12, i64 0, i32 1
  %15 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  store %Array* %15, %Array** %13
  store %Tuple* null, %Tuple** %14
  call void @__quantum__rt__callable_invoke(%Callable* %10, %Tuple* %11, %Tuple* null)
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %17 = bitcast i8* %16 to %Callable**
  %18 = load %Callable*, %Callable** %17
  call void @__quantum__rt__callable_invoke(%Callable* %18, %Tuple* null, %Tuple* null)
  %fct = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %19 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %21 = bitcast %Tuple* %20 to { %String* }*
  %22 = getelementptr { %String* }, { %String* }* %21, i64 0, i32 0
  store %String* %19, %String** %22
  call void @__quantum__rt__string_update_reference_count(%String* %19, i64 1)
  %23 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  call void @__quantum__rt__callable_invoke(%Callable* %fct, %Tuple* %20, %Tuple* %23)
  %24 = bitcast %Tuple* %23 to { %String*, { i64, double }* }*
  %25 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %24, i64 0, i32 0
  %str = load %String*, %String** %25
  %26 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %24, i64 0, i32 1
  %27 = load { i64, double }*, { i64, double }** %26
  %28 = getelementptr { i64, double }, { i64, double }* %27, i64 0, i32 1
  %val = load double, double* %28
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %30 = bitcast %Tuple* %29 to { %String*, double }*
  %31 = getelementptr { %String*, double }, { %String*, double }* %30, i64 0, i32 0
  %32 = getelementptr { %String*, double }, { %String*, double }* %30, i64 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 1)
  store %String* %str, %String** %31
  store double %val, double* %32
  call void @__quantum__rt__array_update_access_count(%Array* %arr, i64 -1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %33 = phi i64 [ 0, %entry ], [ %38, %exiting__1 ]
  %34 = icmp sle i64 %33, 0
  br i1 %34, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %35 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %33)
  %36 = bitcast i8* %35 to %Callable**
  %37 = load %Callable*, %Callable** %36
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %37, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %37, i64 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %38 = add i64 %33, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %6, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %6, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %10, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %15, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %fct, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %fct, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %20, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 -1)
  %39 = bitcast { i64, double }* %27 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %39, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %23, i64 -1)
  ret { %String*, double }* %30
}
