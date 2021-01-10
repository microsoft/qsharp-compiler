define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, %Tuple* null)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { %Callable*, double }*
  %3 = getelementptr { %Callable*, double }, { %Callable*, double }* %2, i64 0, i32 0
  %4 = getelementptr { %Callable*, double }, { %Callable*, double }* %2, i64 0, i32 1
  store %Callable* %0, %Callable** %3
  call void @__quantum__rt__callable_reference(%Callable* %0)
  store double 2.500000e-01, double* %4
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %1)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %63, %exiting__1 ]
  %5 = icmp sge i64 %i, 100
  %6 = icmp sle i64 %i, 100
  %7 = select i1 true, i1 %6, i1 %5
  br i1 %7, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { %Qubit* }*
  %10 = getelementptr { %Qubit* }, { %Qubit* }* %9, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %10
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %8, %Tuple* null)
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %12 = bitcast %Tuple* %11 to { %Qubit* }*
  %13 = getelementptr { %Qubit* }, { %Qubit* }* %12, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %13
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %11, %Tuple* null)
  %14 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %15 = load %Result*, %Result** @ResultZero
  %16 = call i1 @__quantum__rt__result_equal(%Result* %14, %Result* %15)
  %17 = xor i1 %16, true
  br i1 %17, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %18 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %18 to { i64, double }*
  %19 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 0
  %20 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 1
  store i64 %a, i64* %19
  store double %b, double* %20
  call void @__quantum__rt__tuple_add_access(%Tuple* %18)
  %21 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %22 to { %String*, %Qubit* }*
  %23 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %24 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  store %String* %21, %String** %23
  call void @__quantum__rt__string_reference(%String* %21)
  store %Qubit* %qb, %Qubit** %24
  call void @__quantum__rt__tuple_add_access(%Tuple* %22)
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %26 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %27 = bitcast %Tuple* %26 to { %Callable*, { i64, double }* }*
  %28 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %27, i64 0, i32 0
  %29 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %27, i64 0, i32 1
  store %Callable* %25, %Callable** %28
  call void @__quantum__rt__callable_reference(%Callable* %25)
  store { i64, double }* %tuple1, { i64, double }** %29
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %26)
  %30 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %31 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %32 = bitcast %Tuple* %31 to { %Callable*, { %String*, %Qubit* }* }*
  %33 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %32, i64 0, i32 0
  %34 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %32, i64 0, i32 1
  store %Callable* %30, %Callable** %33
  call void @__quantum__rt__callable_reference(%Callable* %30)
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %34
  %35 = load %String*, %String** %23
  call void @__quantum__rt__string_reference(%String* %35)
  call void @__quantum__rt__tuple_reference(%Tuple* %22)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %31)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %22, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %18, %Tuple* null)
  %36 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %37 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %38 = bitcast %Tuple* %37 to { %Callable*, { i64, double }* }*
  %39 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %38, i64 0, i32 0
  %40 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %38, i64 0, i32 1
  store %Callable* %36, %Callable** %39
  call void @__quantum__rt__callable_reference(%Callable* %36)
  store { i64, double }* %tuple1, { i64, double }** %40
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %37)
  %41 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %42 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %43 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %44 = bitcast %Tuple* %43 to { %Callable*, %String*, %Qubit* }*
  %45 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %44, i64 0, i32 0
  %46 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %44, i64 0, i32 1
  %47 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %44, i64 0, i32 2
  store %Callable* %41, %Callable** %45
  call void @__quantum__rt__callable_reference(%Callable* %41)
  store %String* %42, %String** %46
  call void @__quantum__rt__string_reference(%String* %42)
  store %Qubit* %qb, %Qubit** %47
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %43)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %22, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %18, %Tuple* null)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %18)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %22)
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  call void @__quantum__rt__string_unreference(%String* %21)
  %48 = load %String*, %String** %23
  call void @__quantum__rt__string_unreference(%String* %48)
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  call void @__quantum__rt__callable_unreference(%Callable* %25)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %30)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %36)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__callable_unreference(%Callable* %41)
  call void @__quantum__rt__string_unreference(%String* %42)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %49 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  %50 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %50)
  %51 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %52 = bitcast %Tuple* %51 to { %Callable*, %Callable* }*
  %53 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %52, i64 0, i32 0
  %54 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %52, i64 0, i32 1
  store %Callable* %49, %Callable** %53
  call void @__quantum__rt__callable_reference(%Callable* %49)
  store %Callable* %50, %Callable** %54
  call void @__quantum__rt__callable_reference(%Callable* %50)
  %55 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %51)
  %56 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %57 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %56, i64 0)
  %58 = bitcast i8* %57 to %Qubit**
  store %Qubit* %qb, %Qubit** %58
  %59 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %56)
  %60 = bitcast { %Array* }* %59 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %55, %Tuple* %60, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %11)
  call void @__quantum__rt__result_unreference(%Result* %14)
  call void @__quantum__rt__callable_unreference(%Callable* %49)
  call void @__quantum__rt__callable_unreference(%Callable* %50)
  call void @__quantum__rt__callable_unreference(%Callable* %55)
  call void @__quantum__rt__array_unreference(%Array* %56)
  %61 = getelementptr { %Array* }, { %Array* }* %59, i64 0, i32 0
  %62 = load %Array*, %Array** %61
  call void @__quantum__rt__array_unreference(%Array* %62)
  call void @__quantum__rt__tuple_unreference(%Tuple* %60)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %63 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
