define { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %1 = bitcast i8* %0 to %Callable**
  %2 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__DoNothing, %Tuple* null)
  store %Callable* %2, %Callable** %1
  call void @__quantum__rt__array_update_access_count(%Array* %arr, i64 1)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %4 = bitcast i8* %3 to %Callable**
  %5 = load %Callable*, %Callable** %4
  %6 = call %Callable* @__quantum__rt__callable_copy(%Callable* %5, i1 true)
  %7 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %8 = bitcast %Tuple* %7 to { i64 }*
  %9 = getelementptr { i64 }, { i64 }* %8, i64 0, i32 0
  store i64 1, i64* %9
  call void @__quantum__rt__callable_memory_management(%Callable* %6, %Tuple* %7, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i64 -1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %6)
  call void @__quantum__rt__callable_invoke(%Callable* %6, %Tuple* null, %Tuple* null)
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %11 = bitcast i8* %10 to %Callable**
  %12 = load %Callable*, %Callable** %11
  %13 = call %Callable* @__quantum__rt__callable_copy(%Callable* %12, i1 true)
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { i64 }*
  %16 = getelementptr { i64 }, { i64 }* %15, i64 0, i32 0
  store i64 1, i64* %16
  call void @__quantum__rt__callable_memory_management(%Callable* %13, %Tuple* %14, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i64 -1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %13)
  %17 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %18 = bitcast %Tuple* %17 to { %Array*, %Tuple* }*
  %19 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %18, i64 0, i32 0
  %20 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %18, i64 0, i32 1
  %21 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  store %Array* %21, %Array** %19
  store %Tuple* null, %Tuple** %20
  call void @__quantum__rt__callable_invoke(%Callable* %13, %Tuple* %17, %Tuple* null)
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %23 = bitcast i8* %22 to %Callable**
  %24 = load %Callable*, %Callable** %23
  call void @__quantum__rt__callable_invoke(%Callable* %24, %Tuple* null, %Tuple* null)
  %fct = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple, %Tuple* null)
  %25 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %26 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %27 = bitcast %Tuple* %26 to { %String* }*
  %28 = getelementptr { %String* }, { %String* }* %27, i64 0, i32 0
  store %String* %25, %String** %28
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 1)
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  call void @__quantum__rt__callable_invoke(%Callable* %fct, %Tuple* %26, %Tuple* %29)
  %30 = bitcast %Tuple* %29 to { %String*, { i64, double }* }*
  %31 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %30, i64 0, i32 0
  %str = load %String*, %String** %31
  %32 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %30, i64 0, i32 1
  %33 = load { i64, double }*, { i64, double }** %32
  %34 = getelementptr { i64, double }, { i64, double }* %33, i64 0, i32 1
  %val = load double, double* %34
  %35 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %36 = bitcast %Tuple* %35 to { %String*, double }*
  %37 = getelementptr { %String*, double }, { %String*, double }* %36, i64 0, i32 0
  %38 = getelementptr { %String*, double }, { %String*, double }* %36, i64 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 1)
  store %String* %str, %String** %37
  store double %val, double* %38
  call void @__quantum__rt__array_update_access_count(%Array* %arr, i64 -1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %39 = phi i64 [ 0, %entry ], [ %47, %exiting__1 ]
  %40 = icmp sle i64 %39, 0
  br i1 %40, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %41 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %39)
  %42 = bitcast i8* %41 to %Callable**
  %43 = load %Callable*, %Callable** %42
  %44 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %45 = bitcast %Tuple* %44 to { i64 }*
  %46 = getelementptr { i64 }, { i64 }* %45, i64 0, i32 0
  store i64 -1, i64* %46
  call void @__quantum__rt__callable_memory_management(%Callable* %43, %Tuple* %44, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %44, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %43, i64 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %47 = add i64 %39, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i64 -1)
  %48 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %49 = bitcast %Tuple* %48 to { i64 }*
  %50 = getelementptr { i64 }, { i64 }* %49, i64 0, i32 0
  store i64 -1, i64* %50
  call void @__quantum__rt__callable_memory_management(%Callable* %6, %Tuple* %48, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %6, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %13, %Tuple* %48, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %13, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %fct, %Tuple* %48, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %fct, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %str, i64 -1)
  %51 = bitcast { i64, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %51, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %29, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %48, i64 -1)
  ret { %String*, double }* %36
}
