define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 0
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, %Tuple* null)
  store %Callable* %3, %Callable** %2
  call void @__quantum__rt__callable_reference(%Callable* %3)
  %4 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 1
  store double 2.500000e-01, double* %4
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %77, %exiting__1 ]
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
  %21 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %22 to { %String*, %Qubit* }*
  %23 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %24 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  store %String* %21, %String** %23
  call void @__quantum__rt__string_reference(%String* %21)
  store %Qubit* %qb, %Qubit** %24
  %25 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %26 = bitcast %Tuple* %25 to { %Callable*, { i64, double }* }*
  %27 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %26, i64 0, i32 0
  %28 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  store %Callable* %28, %Callable** %27
  call void @__quantum__rt__callable_reference(%Callable* %28)
  %29 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %26, i64 0, i32 1
  store { i64, double }* %tuple1, { i64, double }** %29
  %30 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %30)
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %25)
  %31 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %32 = bitcast %Tuple* %31 to { %Callable*, { %String*, %Qubit* }* }*
  %33 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %32, i64 0, i32 0
  %34 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  store %Callable* %34, %Callable** %33
  call void @__quantum__rt__callable_reference(%Callable* %34)
  %35 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %32, i64 0, i32 1
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %35
  %36 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %37 = load %String*, %String** %36
  call void @__quantum__rt__string_reference(%String* %37)
  %38 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %38)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %31)
  %39 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %39, %Tuple* null)
  %40 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %40, %Tuple* null)
  %41 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %42 = bitcast %Tuple* %41 to { %Callable*, { i64, double }* }*
  %43 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %42, i64 0, i32 0
  %44 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  store %Callable* %44, %Callable** %43
  call void @__quantum__rt__callable_reference(%Callable* %44)
  %45 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %42, i64 0, i32 1
  store { i64, double }* %tuple1, { i64, double }** %45
  %46 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %46)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %41)
  %47 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %48 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %49 = bitcast %Tuple* %48 to { %Callable*, %String*, %Qubit* }*
  %50 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %49, i64 0, i32 0
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  store %Callable* %51, %Callable** %50
  call void @__quantum__rt__callable_reference(%Callable* %51)
  %52 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %49, i64 0, i32 1
  store %String* %47, %String** %52
  call void @__quantum__rt__string_reference(%String* %47)
  %53 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %49, i64 0, i32 2
  store %Qubit* %qb, %Qubit** %53
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %48)
  %54 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %54, %Tuple* null)
  %55 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %55, %Tuple* null)
  %56 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %56)
  call void @__quantum__rt__string_unreference(%String* %21)
  %57 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %58 = load %String*, %String** %57
  call void @__quantum__rt__string_unreference(%String* %58)
  %59 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %59)
  call void @__quantum__rt__callable_unreference(%Callable* %28)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %34)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %44)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__string_unreference(%String* %47)
  call void @__quantum__rt__callable_unreference(%Callable* %51)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %60 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %60)
  %61 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %62 = bitcast %Tuple* %61 to { %Callable*, %Callable* }*
  %63 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %62, i64 0, i32 0
  %64 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  store %Callable* %64, %Callable** %63
  call void @__quantum__rt__callable_reference(%Callable* %64)
  %65 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %62, i64 0, i32 1
  store %Callable* %60, %Callable** %65
  call void @__quantum__rt__callable_reference(%Callable* %60)
  %66 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %61)
  %67 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %68 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %67, i64 0)
  %69 = bitcast i8* %68 to %Qubit**
  store %Qubit* %qb, %Qubit** %69
  %70 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %67)
  %71 = bitcast { %Array* }* %70 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %66, %Tuple* %71, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %72 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %72)
  %73 = bitcast { %Qubit* }* %12 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %73)
  call void @__quantum__rt__result_unreference(%Result* %14)
  call void @__quantum__rt__result_unreference(%Result* %15)
  call void @__quantum__rt__callable_unreference(%Callable* %60)
  call void @__quantum__rt__callable_unreference(%Callable* %64)
  call void @__quantum__rt__callable_unreference(%Callable* %66)
  call void @__quantum__rt__array_unreference(%Array* %67)
  %74 = getelementptr { %Array* }, { %Array* }* %70, i64 0, i32 0
  %75 = load %Array*, %Array** %74
  call void @__quantum__rt__array_unreference(%Array* %75)
  %76 = bitcast { %Array* }* %70 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %76)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %77 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
