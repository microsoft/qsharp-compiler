define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 0
  %3 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, %Tuple* null)
  store %Callable* %4, %Callable** %2
  store double 2.500000e-01, double* %3
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
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
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %21 to { %String*, %Qubit* }*
  %22 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %23 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  %24 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %String* %24, %String** %22
  store %Qubit* %qb, %Qubit** %23
  call void @__quantum__rt__tuple_add_access(%Tuple* %21)
  %25 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %26 = bitcast %Tuple* %25 to { %Callable*, { i64, double }* }*
  %27 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %26, i64 0, i32 0
  %28 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %26, i64 0, i32 1
  %29 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  store %Callable* %29, %Callable** %27
  store { i64, double }* %tuple1, { i64, double }** %28
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %25)
  %30 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %31 = bitcast %Tuple* %30 to { %Callable*, { %String*, %Qubit* }* }*
  %32 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %31, i64 0, i32 0
  %33 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %31, i64 0, i32 1
  %34 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %35 = load %String*, %String** %22
  call void @__quantum__rt__string_reference(%String* %35)
  call void @__quantum__rt__tuple_reference(%Tuple* %21)
  store %Callable* %34, %Callable** %32
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %33
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %30)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %21, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %18, %Tuple* null)
  %36 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %37 = bitcast %Tuple* %36 to { %Callable*, { i64, double }* }*
  %38 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %37, i64 0, i32 0
  %39 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %37, i64 0, i32 1
  %40 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  store %Callable* %40, %Callable** %38
  store { i64, double }* %tuple1, { i64, double }** %39
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %36)
  %41 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %42 = bitcast %Tuple* %41 to { %Callable*, %String*, %Qubit* }*
  %43 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %42, i64 0, i32 0
  %44 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %42, i64 0, i32 1
  %45 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %42, i64 0, i32 2
  %46 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %47 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %Callable* %46, %Callable** %43
  store %String* %47, %String** %44
  store %Qubit* %qb, %Qubit** %45
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %41)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %21, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %18, %Tuple* null)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %18)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %21)
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  %48 = load %String*, %String** %22
  call void @__quantum__rt__string_unreference(%String* %48)
  call void @__quantum__rt__tuple_unreference(%Tuple* %21)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %49 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %50 = bitcast %Tuple* %49 to { %Callable*, %Callable* }*
  %51 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %50, i64 0, i32 0
  %52 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %50, i64 0, i32 1
  %53 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  %54 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %54)
  store %Callable* %53, %Callable** %51
  store %Callable* %54, %Callable** %52
  %55 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %49)
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
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
