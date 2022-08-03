define internal { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__DoNothing__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %0, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i32 1)
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
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 1)
  %17 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %18 = bitcast %Tuple* %17 to { %String*, double }*
  %19 = getelementptr inbounds { %String*, double }, { %String*, double }* %18, i32 0, i32 0
  %20 = getelementptr inbounds { %String*, double }, { %String*, double }* %18, i32 0, i32 1
  store %String* %str, %String** %19, align 8
  store double %val, double* %20, align 8
  call void @__quantum__rt__callable_make_adjoint(%Callable* %9)
  call void @__quantum__rt__callable_make_controlled(%Callable* %10)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %21 = phi i64 [ 0, %exit__1 ], [ %26, %exiting__2 ]
  %22 = icmp sle i64 %21, 0
  br i1 %22, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %23 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %21)
  %24 = bitcast i8* %23 to %Callable**
  %25 = load %Callable*, %Callable** %24, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %25, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %25, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %26 = add i64 %21, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i32 -1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %27 = phi i64 [ 0, %exit__2 ], [ %32, %exiting__3 ]
  %28 = icmp sle i64 %27, 0
  br i1 %28, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %29 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %27)
  %30 = bitcast i8* %29 to %Callable**
  %31 = load %Callable*, %Callable** %30, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %31, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %31, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %32 = add i64 %27, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %fct, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %str, i32 -1)
  %33 = bitcast { i64, double }* %15 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %33, i32 -1)
  %34 = bitcast { %String*, { i64, double }* }* %12 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %34, i32 -1)
  ret { %String*, double }* %18
}
