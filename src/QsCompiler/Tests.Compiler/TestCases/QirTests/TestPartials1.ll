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
  %i = phi i64 [ 0, %preheader__1 ], [ %79, %exiting__1 ]
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
  %11 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %11, %Tuple* null)
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %Qubit* }*
  %14 = getelementptr { %Qubit* }, { %Qubit* }* %13, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %14
  %15 = bitcast { %Qubit* }* %13 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %15, %Tuple* null)
  %16 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %17 = load %Result*, %Result** @ResultZero
  %18 = call i1 @__quantum__rt__result_equal(%Result* %16, %Result* %17)
  %19 = xor i1 %18, true
  br i1 %19, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %20 to { i64, double }*
  %21 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 0
  %22 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 1
  store i64 %a, i64* %21
  store double %b, double* %22
  %23 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %24 to { %String*, %Qubit* }*
  %25 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %26 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  store %String* %23, %String** %25
  call void @__quantum__rt__string_reference(%String* %23)
  store %Qubit* %qb, %Qubit** %26
  %27 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %28 = bitcast %Tuple* %27 to { %Callable*, { i64, double }* }*
  %29 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %28, i64 0, i32 0
  %30 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  store %Callable* %30, %Callable** %29
  call void @__quantum__rt__callable_reference(%Callable* %30)
  %31 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %28, i64 0, i32 1
  store { i64, double }* %tuple1, { i64, double }** %31
  %32 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %32)
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %27)
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %34 = bitcast %Tuple* %33 to { %Callable*, { %String*, %Qubit* }* }*
  %35 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %34, i64 0, i32 0
  %36 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  store %Callable* %36, %Callable** %35
  call void @__quantum__rt__callable_reference(%Callable* %36)
  %37 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %34, i64 0, i32 1
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %37
  %38 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %39 = load %String*, %String** %38
  call void @__quantum__rt__string_reference(%String* %39)
  %40 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %40)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %33)
  %41 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %41, %Tuple* null)
  %42 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %42, %Tuple* null)
  %43 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %44 = bitcast %Tuple* %43 to { %Callable*, { i64, double }* }*
  %45 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %44, i64 0, i32 0
  %46 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  store %Callable* %46, %Callable** %45
  call void @__quantum__rt__callable_reference(%Callable* %46)
  %47 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %44, i64 0, i32 1
  store { i64, double }* %tuple1, { i64, double }** %47
  %48 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %48)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %43)
  %49 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %50 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %51 = bitcast %Tuple* %50 to { %Callable*, %String*, %Qubit* }*
  %52 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %51, i64 0, i32 0
  %53 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  store %Callable* %53, %Callable** %52
  call void @__quantum__rt__callable_reference(%Callable* %53)
  %54 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %51, i64 0, i32 1
  store %String* %49, %String** %54
  call void @__quantum__rt__string_reference(%String* %49)
  %55 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %51, i64 0, i32 2
  store %Qubit* %qb, %Qubit** %55
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %50)
  %56 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %56, %Tuple* null)
  %57 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %57, %Tuple* null)
  %58 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %58)
  call void @__quantum__rt__string_unreference(%String* %23)
  %59 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %60 = load %String*, %String** %59
  call void @__quantum__rt__string_unreference(%String* %60)
  %61 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %61)
  call void @__quantum__rt__callable_unreference(%Callable* %30)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %36)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %46)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__string_unreference(%String* %49)
  call void @__quantum__rt__callable_unreference(%Callable* %53)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %62 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %62)
  %63 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %64 = bitcast %Tuple* %63 to { %Callable*, %Callable* }*
  %65 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %64, i64 0, i32 0
  %66 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  store %Callable* %66, %Callable** %65
  call void @__quantum__rt__callable_reference(%Callable* %66)
  %67 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %64, i64 0, i32 1
  store %Callable* %62, %Callable** %67
  call void @__quantum__rt__callable_reference(%Callable* %62)
  %68 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %63)
  %69 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %70 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %69, i64 0)
  %71 = bitcast i8* %70 to %Qubit**
  store %Qubit* %qb, %Qubit** %71
  %72 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %69)
  %73 = bitcast { %Array* }* %72 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %68, %Tuple* %73, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %74 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %74)
  %75 = bitcast { %Qubit* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %75)
  call void @__quantum__rt__result_unreference(%Result* %16)
  call void @__quantum__rt__result_unreference(%Result* %17)
  call void @__quantum__rt__callable_unreference(%Callable* %62)
  call void @__quantum__rt__callable_unreference(%Callable* %66)
  call void @__quantum__rt__callable_unreference(%Callable* %68)
  call void @__quantum__rt__array_unreference(%Array* %69)
  %76 = getelementptr { %Array* }, { %Array* }* %72, i64 0, i32 0
  %77 = load %Array*, %Array** %76
  call void @__quantum__rt__array_unreference(%Array* %77)
  %78 = bitcast { %Array* }* %72 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %78)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %79 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
