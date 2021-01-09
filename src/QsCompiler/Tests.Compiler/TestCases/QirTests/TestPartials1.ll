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
  call void @__quantum__rt__callable_reference(%Callable* %0)
  call void @__quantum__rt__tuple_reference(%Tuple* %1)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %93, %exiting__1 ]
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
  %30 = load %Callable*, %Callable** %28
  call void @__quantum__rt__callable_reference(%Callable* %30)
  %31 = load { i64, double }*, { i64, double }** %29
  %32 = bitcast { i64, double }* %31 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %32)
  call void @__quantum__rt__tuple_reference(%Tuple* %26)
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %35 = bitcast %Tuple* %34 to { %Callable*, { %String*, %Qubit* }* }*
  %36 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %35, i64 0, i32 0
  %37 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %35, i64 0, i32 1
  store %Callable* %33, %Callable** %36
  call void @__quantum__rt__callable_reference(%Callable* %33)
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %37
  %38 = load %String*, %String** %23
  call void @__quantum__rt__string_reference(%String* %38)
  call void @__quantum__rt__tuple_reference(%Tuple* %22)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %34)
  %39 = load %Callable*, %Callable** %36
  call void @__quantum__rt__callable_reference(%Callable* %39)
  %40 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %37
  %41 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %40, i64 0, i32 0
  %42 = load %String*, %String** %41
  call void @__quantum__rt__string_reference(%String* %42)
  %43 = bitcast { %String*, %Qubit* }* %40 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %43)
  call void @__quantum__rt__tuple_reference(%Tuple* %34)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %22, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %18, %Tuple* null)
  %44 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %45 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %46 = bitcast %Tuple* %45 to { %Callable*, { i64, double }* }*
  %47 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %46, i64 0, i32 0
  %48 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %46, i64 0, i32 1
  store %Callable* %44, %Callable** %47
  call void @__quantum__rt__callable_reference(%Callable* %44)
  store { i64, double }* %tuple1, { i64, double }** %48
  call void @__quantum__rt__tuple_reference(%Tuple* %18)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %45)
  %49 = load %Callable*, %Callable** %47
  call void @__quantum__rt__callable_reference(%Callable* %49)
  %50 = load { i64, double }*, { i64, double }** %48
  %51 = bitcast { i64, double }* %50 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %51)
  call void @__quantum__rt__tuple_reference(%Tuple* %45)
  %52 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %53 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %54 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %55 = bitcast %Tuple* %54 to { %Callable*, %String*, %Qubit* }*
  %56 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %55, i64 0, i32 0
  %57 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %55, i64 0, i32 1
  %58 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %55, i64 0, i32 2
  store %Callable* %52, %Callable** %56
  call void @__quantum__rt__callable_reference(%Callable* %52)
  store %String* %53, %String** %57
  call void @__quantum__rt__string_reference(%String* %53)
  store %Qubit* %qb, %Qubit** %58
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %54)
  %59 = load %Callable*, %Callable** %56
  call void @__quantum__rt__callable_reference(%Callable* %59)
  %60 = load %String*, %String** %57
  call void @__quantum__rt__string_reference(%String* %60)
  call void @__quantum__rt__tuple_reference(%Tuple* %54)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %22, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %18, %Tuple* null)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %18)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %22)
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  call void @__quantum__rt__string_unreference(%String* %21)
  %61 = load %String*, %String** %23
  call void @__quantum__rt__string_unreference(%String* %61)
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  call void @__quantum__rt__callable_unreference(%Callable* %25)
  %62 = load %Callable*, %Callable** %28
  call void @__quantum__rt__callable_unreference(%Callable* %62)
  %63 = load { i64, double }*, { i64, double }** %29
  %64 = bitcast { i64, double }* %63 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %64)
  call void @__quantum__rt__tuple_unreference(%Tuple* %26)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %33)
  %65 = load %Callable*, %Callable** %36
  call void @__quantum__rt__callable_unreference(%Callable* %65)
  %66 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %37
  %67 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %66, i64 0, i32 0
  %68 = load %String*, %String** %67
  call void @__quantum__rt__string_unreference(%String* %68)
  %69 = bitcast { %String*, %Qubit* }* %66 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %69)
  call void @__quantum__rt__tuple_unreference(%Tuple* %34)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %44)
  %70 = load %Callable*, %Callable** %47
  call void @__quantum__rt__callable_unreference(%Callable* %70)
  %71 = load { i64, double }*, { i64, double }** %48
  %72 = bitcast { i64, double }* %71 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %72)
  call void @__quantum__rt__tuple_unreference(%Tuple* %45)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__callable_unreference(%Callable* %52)
  call void @__quantum__rt__string_unreference(%String* %53)
  %73 = load %Callable*, %Callable** %56
  call void @__quantum__rt__callable_unreference(%Callable* %73)
  %74 = load %String*, %String** %57
  call void @__quantum__rt__string_unreference(%String* %74)
  call void @__quantum__rt__tuple_unreference(%Tuple* %54)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %75 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  %76 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %76)
  %77 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %78 = bitcast %Tuple* %77 to { %Callable*, %Callable* }*
  %79 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %78, i64 0, i32 0
  %80 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %78, i64 0, i32 1
  store %Callable* %75, %Callable** %79
  call void @__quantum__rt__callable_reference(%Callable* %75)
  store %Callable* %76, %Callable** %80
  call void @__quantum__rt__callable_reference(%Callable* %76)
  %81 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %77)
  %82 = load %Callable*, %Callable** %79
  call void @__quantum__rt__callable_reference(%Callable* %82)
  %83 = load %Callable*, %Callable** %80
  call void @__quantum__rt__callable_reference(%Callable* %83)
  call void @__quantum__rt__tuple_reference(%Tuple* %77)
  %84 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %85 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %84, i64 0)
  %86 = bitcast i8* %85 to %Qubit**
  store %Qubit* %qb, %Qubit** %86
  %87 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %84)
  %88 = bitcast { %Array* }* %87 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %81, %Tuple* %88, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__tuple_unreference(%Tuple* %8)
  call void @__quantum__rt__tuple_unreference(%Tuple* %11)
  call void @__quantum__rt__result_unreference(%Result* %14)
  call void @__quantum__rt__callable_unreference(%Callable* %75)
  call void @__quantum__rt__callable_unreference(%Callable* %76)
  %89 = load %Callable*, %Callable** %79
  call void @__quantum__rt__callable_unreference(%Callable* %89)
  %90 = load %Callable*, %Callable** %80
  call void @__quantum__rt__callable_unreference(%Callable* %90)
  call void @__quantum__rt__tuple_unreference(%Tuple* %77)
  call void @__quantum__rt__callable_unreference(%Callable* %81)
  call void @__quantum__rt__array_unreference(%Array* %84)
  %91 = getelementptr { %Array* }, { %Array* }* %87, i64 0, i32 0
  %92 = load %Array*, %Array** %91
  call void @__quantum__rt__array_unreference(%Array* %92)
  call void @__quantum__rt__tuple_unreference(%Tuple* %88)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %93 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}