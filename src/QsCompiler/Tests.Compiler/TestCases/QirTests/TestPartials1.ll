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
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %107, %exiting__1 ]
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
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %26 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %27 = bitcast %Tuple* %26 to { %Callable*, { i64, double }* }*
  %28 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %27, i64 0, i32 0
  %29 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %27, i64 0, i32 1
  store %Callable* %25, %Callable** %28
  call void @__quantum__rt__callable_reference(%Callable* %25)
  store { i64, double }* %tuple1, { i64, double }** %29
  %30 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %30)
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %26)
  %31 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %32 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %33 = bitcast %Tuple* %32 to { %Callable*, { %String*, %Qubit* }* }*
  %34 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %33, i64 0, i32 0
  %35 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %33, i64 0, i32 1
  store %Callable* %31, %Callable** %34
  call void @__quantum__rt__callable_reference(%Callable* %31)
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %35
  %36 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %37 = load %String*, %String** %36
  call void @__quantum__rt__string_reference(%String* %37)
  %38 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %38)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %32)
  %39 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %39, %Tuple* null)
  %40 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %40, %Tuple* null)
  %41 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %42 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %43 = bitcast %Tuple* %42 to { %Callable*, { i64, double }* }*
  %44 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %43, i64 0, i32 0
  %45 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %43, i64 0, i32 1
  store %Callable* %41, %Callable** %44
  call void @__quantum__rt__callable_reference(%Callable* %41)
  store { i64, double }* %tuple1, { i64, double }** %45
  %46 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %46)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %42)
  %47 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %48 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %49 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %50 = bitcast %Tuple* %49 to { %Callable*, %String*, %Qubit* }*
  %51 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %50, i64 0, i32 0
  %52 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %50, i64 0, i32 1
  %53 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %50, i64 0, i32 2
  store %Callable* %47, %Callable** %51
  call void @__quantum__rt__callable_reference(%Callable* %47)
  store %String* %48, %String** %52
  call void @__quantum__rt__string_reference(%String* %48)
  store %Qubit* %qb, %Qubit** %53
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %49)
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
  call void @__quantum__rt__callable_unreference(%Callable* %25)
  %60 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %27, i64 0, i32 0
  %61 = load %Callable*, %Callable** %60
  call void @__quantum__rt__callable_unreference(%Callable* %61)
  %62 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %27, i64 0, i32 1
  %63 = load { i64, double }*, { i64, double }** %62
  %64 = bitcast { i64, double }* %63 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %64)
  %65 = bitcast { %Callable*, { i64, double }* }* %27 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %65)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %31)
  %66 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %33, i64 0, i32 0
  %67 = load %Callable*, %Callable** %66
  call void @__quantum__rt__callable_unreference(%Callable* %67)
  %68 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %33, i64 0, i32 1
  %69 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %68
  %70 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %69, i64 0, i32 0
  %71 = load %String*, %String** %70
  call void @__quantum__rt__string_unreference(%String* %71)
  %72 = bitcast { %String*, %Qubit* }* %69 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %72)
  %73 = bitcast { %Callable*, { %String*, %Qubit* }* }* %33 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %73)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %41)
  %74 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %43, i64 0, i32 0
  %75 = load %Callable*, %Callable** %74
  call void @__quantum__rt__callable_unreference(%Callable* %75)
  %76 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %43, i64 0, i32 1
  %77 = load { i64, double }*, { i64, double }** %76
  %78 = bitcast { i64, double }* %77 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %78)
  %79 = bitcast { %Callable*, { i64, double }* }* %43 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %79)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__callable_unreference(%Callable* %47)
  call void @__quantum__rt__string_unreference(%String* %48)
  %80 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %50, i64 0, i32 0
  %81 = load %Callable*, %Callable** %80
  call void @__quantum__rt__callable_unreference(%Callable* %81)
  %82 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %50, i64 0, i32 1
  %83 = load %String*, %String** %82
  call void @__quantum__rt__string_unreference(%String* %83)
  %84 = bitcast { %Callable*, %String*, %Qubit* }* %50 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %84)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %85 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  %86 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %86)
  %87 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %88 = bitcast %Tuple* %87 to { %Callable*, %Callable* }*
  %89 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %88, i64 0, i32 0
  %90 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %88, i64 0, i32 1
  store %Callable* %85, %Callable** %89
  call void @__quantum__rt__callable_reference(%Callable* %85)
  store %Callable* %86, %Callable** %90
  call void @__quantum__rt__callable_reference(%Callable* %86)
  %91 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %87)
  %92 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %93 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %92, i64 0)
  %94 = bitcast i8* %93 to %Qubit**
  store %Qubit* %qb, %Qubit** %94
  %95 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %92)
  %96 = bitcast { %Array* }* %95 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %91, %Tuple* %96, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %97 = bitcast { %Qubit* }* %9 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %97)
  %98 = bitcast { %Qubit* }* %12 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %98)
  call void @__quantum__rt__result_unreference(%Result* %14)
  call void @__quantum__rt__result_unreference(%Result* %15)
  call void @__quantum__rt__callable_unreference(%Callable* %85)
  call void @__quantum__rt__callable_unreference(%Callable* %86)
  %99 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %88, i64 0, i32 0
  %100 = load %Callable*, %Callable** %99
  call void @__quantum__rt__callable_unreference(%Callable* %100)
  %101 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %88, i64 0, i32 1
  %102 = load %Callable*, %Callable** %101
  call void @__quantum__rt__callable_unreference(%Callable* %102)
  %103 = bitcast { %Callable*, %Callable* }* %88 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %103)
  call void @__quantum__rt__callable_unreference(%Callable* %91)
  call void @__quantum__rt__array_unreference(%Array* %92)
  %104 = getelementptr { %Array* }, { %Array* }* %95, i64 0, i32 0
  %105 = load %Array*, %Array** %104
  call void @__quantum__rt__array_unreference(%Array* %105)
  %106 = bitcast { %Array* }* %95 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %106)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %107 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %108 = getelementptr { %Callable*, double }, { %Callable*, double }* %2, i64 0, i32 0
  %109 = load %Callable*, %Callable** %108
  call void @__quantum__rt__callable_unreference(%Callable* %109)
  %110 = bitcast { %Callable*, double }* %2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %110)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
