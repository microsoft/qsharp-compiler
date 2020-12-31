define { %String*, double }* @Microsoft__Quantum__Testing__QIR__TestLocalCallables__body() {
entry:
  %arr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %1 = bitcast i8* %0 to %Callable**
  %2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__DoNothing, %Tuple* null)
  store %Callable* %2, %Callable** %1
  call void @__quantum__rt__callable_reference(%Callable* %2)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %4 = bitcast i8* %3 to %Callable**
  %5 = load %Callable*, %Callable** %4
  %6 = call %Callable* @__quantum__rt__callable_copy(%Callable* %5)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %6)
  call void @__quantum__rt__callable_invoke(%Callable* %6, %Tuple* null, %Tuple* null)
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %8 = bitcast i8* %7 to %Callable**
  %9 = load %Callable*, %Callable** %8
  %10 = call %Callable* @__quantum__rt__callable_copy(%Callable* %9)
  call void @__quantum__rt__callable_make_controlled(%Callable* %10)
  %11 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 0)
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { %Array*, %Tuple* }*
  %14 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %13, i64 0, i32 0
  %15 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %13, i64 0, i32 1
  store %Array* %11, %Array** %14
  call void @__quantum__rt__array_reference(%Array* %11)
  store %Tuple* null, %Tuple** %15
  %16 = bitcast { %Array*, %Tuple* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %10, %Tuple* %16, %Tuple* null)
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 0)
  %18 = bitcast i8* %17 to %Callable**
  %19 = load %Callable*, %Callable** %18
  call void @__quantum__rt__callable_invoke(%Callable* %19, %Tuple* null, %Tuple* null)
  %fct = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ReturnTuple, %Tuple* null)
  %20 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %22 = bitcast %Tuple* %21 to { %String* }*
  %23 = getelementptr { %String* }, { %String* }* %22, i64 0, i32 0
  store %String* %20, %String** %23
  call void @__quantum__rt__string_reference(%String* %20)
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %25 = bitcast %Tuple* %24 to { %String*, { i64, double }* }*
  %26 = bitcast { %String*, { i64, double }* }* %25 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %fct, %Tuple* %21, %Tuple* %26)
  %27 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %25, i64 0, i32 0
  %28 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %25, i64 0, i32 1
  %str = load %String*, %String** %27
  %29 = load { i64, double }*, { i64, double }** %28
  %30 = getelementptr { i64, double }, { i64, double }* %29, i64 0, i32 0
  %31 = getelementptr { i64, double }, { i64, double }* %29, i64 0, i32 1
  %val = load double, double* %31
  %32 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, double }* getelementptr ({ %String*, double }, { %String*, double }* null, i32 1) to i64))
  %33 = bitcast %Tuple* %32 to { %String*, double }*
  %34 = getelementptr { %String*, double }, { %String*, double }* %33, i64 0, i32 0
  %35 = getelementptr { %String*, double }, { %String*, double }* %33, i64 0, i32 1
  store %String* %str, %String** %34
  call void @__quantum__rt__string_reference(%String* %str)
  store double %val, double* %35
  call void @__quantum__rt__callable_unreference(%Callable* %2)
  call void @__quantum__rt__array_unreference(%Array* %arr)
  call void @__quantum__rt__callable_unreference(%Callable* %5)
  call void @__quantum__rt__callable_unreference(%Callable* %6)
  call void @__quantum__rt__callable_unreference(%Callable* %9)
  call void @__quantum__rt__callable_unreference(%Callable* %10)
  call void @__quantum__rt__array_unreference(%Array* %11)
  %36 = getelementptr { %Array*, %Tuple* }, { %Array*, %Tuple* }* %13, i64 0, i32 0
  %37 = load %Array*, %Array** %36
  call void @__quantum__rt__array_unreference(%Array* %37)
  %38 = bitcast { %Array*, %Tuple* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %38)
  call void @__quantum__rt__callable_unreference(%Callable* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %fct)
  call void @__quantum__rt__string_unreference(%String* %20)
  %39 = getelementptr { %String* }, { %String* }* %22, i64 0, i32 0
  %40 = load %String*, %String** %39
  call void @__quantum__rt__string_unreference(%String* %40)
  %41 = bitcast { %String* }* %22 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %41)
  %42 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %25, i64 0, i32 0
  %43 = load %String*, %String** %42
  call void @__quantum__rt__string_unreference(%String* %43)
  %44 = getelementptr { %String*, { i64, double }* }, { %String*, { i64, double }* }* %25, i64 0, i32 1
  %45 = load { i64, double }*, { i64, double }** %44
  %46 = bitcast { i64, double }* %45 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %46)
  %47 = bitcast { %String*, { i64, double }* }* %25 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %47)
  ret { %String*, double }* %33
}
