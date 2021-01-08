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
  %5 = getelementptr { %Callable*, double }, { %Callable*, double }* %2, i64 0, i32 0
  %6 = load %Callable*, %Callable** %5
  call void @__quantum__rt__callable_reference(%Callable* %6)
  call void @__quantum__rt__tuple_reference(%Tuple* %1)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %117, %exiting__1 ]
  %7 = icmp sge i64 %i, 100
  %8 = icmp sle i64 %i, 100
  %9 = select i1 true, i1 %8, i1 %7
  br i1 %9, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %10 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %11 = bitcast %Tuple* %10 to { %Qubit* }*
  %12 = getelementptr { %Qubit* }, { %Qubit* }* %11, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %12
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %10, %Tuple* null)
  %13 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %14 = bitcast %Tuple* %13 to { %Qubit* }*
  %15 = getelementptr { %Qubit* }, { %Qubit* }* %14, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %15
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %13, %Tuple* null)
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
  call void @__quantum__rt__tuple_add_access(%Tuple* %20)
  %23 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %24 to { %String*, %Qubit* }*
  %25 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %26 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  store %String* %23, %String** %25
  call void @__quantum__rt__string_reference(%String* %23)
  store %Qubit* %qb, %Qubit** %26
  call void @__quantum__rt__tuple_add_access(%Tuple* %24)
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %29 = bitcast %Tuple* %28 to { %Callable*, { i64, double }* }*
  %30 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 0
  %31 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 1
  store %Callable* %27, %Callable** %30
  call void @__quantum__rt__callable_reference(%Callable* %27)
  store { i64, double }* %tuple1, { i64, double }** %31
  call void @__quantum__rt__tuple_reference(%Tuple* %20)
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %28)
  %32 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 0
  %33 = load %Callable*, %Callable** %32
  call void @__quantum__rt__callable_reference(%Callable* %33)
  %34 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 1
  %35 = load { i64, double }*, { i64, double }** %34
  %36 = bitcast { i64, double }* %35 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %36)
  call void @__quantum__rt__tuple_reference(%Tuple* %28)
  %37 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %38 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %39 = bitcast %Tuple* %38 to { %Callable*, { %String*, %Qubit* }* }*
  %40 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %39, i64 0, i32 0
  %41 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %39, i64 0, i32 1
  store %Callable* %37, %Callable** %40
  call void @__quantum__rt__callable_reference(%Callable* %37)
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %41
  %42 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %43 = load %String*, %String** %42
  call void @__quantum__rt__string_reference(%String* %43)
  call void @__quantum__rt__tuple_reference(%Tuple* %24)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %38)
  %44 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %39, i64 0, i32 0
  %45 = load %Callable*, %Callable** %44
  call void @__quantum__rt__callable_reference(%Callable* %45)
  %46 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %39, i64 0, i32 1
  %47 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %46
  %48 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %47, i64 0, i32 0
  %49 = load %String*, %String** %48
  call void @__quantum__rt__string_reference(%String* %49)
  %50 = bitcast { %String*, %Qubit* }* %47 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %50)
  call void @__quantum__rt__tuple_reference(%Tuple* %38)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %24, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %20, %Tuple* null)
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %52 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %53 = bitcast %Tuple* %52 to { %Callable*, { i64, double }* }*
  %54 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %53, i64 0, i32 0
  %55 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %53, i64 0, i32 1
  store %Callable* %51, %Callable** %54
  call void @__quantum__rt__callable_reference(%Callable* %51)
  store { i64, double }* %tuple1, { i64, double }** %55
  call void @__quantum__rt__tuple_reference(%Tuple* %20)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %52)
  %56 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %53, i64 0, i32 0
  %57 = load %Callable*, %Callable** %56
  call void @__quantum__rt__callable_reference(%Callable* %57)
  %58 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %53, i64 0, i32 1
  %59 = load { i64, double }*, { i64, double }** %58
  %60 = bitcast { i64, double }* %59 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %60)
  call void @__quantum__rt__tuple_reference(%Tuple* %52)
  %61 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %62 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %63 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %64 = bitcast %Tuple* %63 to { %Callable*, %String*, %Qubit* }*
  %65 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 0
  %66 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 1
  %67 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 2
  store %Callable* %61, %Callable** %65
  call void @__quantum__rt__callable_reference(%Callable* %61)
  store %String* %62, %String** %66
  call void @__quantum__rt__string_reference(%String* %62)
  store %Qubit* %qb, %Qubit** %67
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %63)
  %68 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 0
  %69 = load %Callable*, %Callable** %68
  call void @__quantum__rt__callable_reference(%Callable* %69)
  %70 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 1
  %71 = load %String*, %String** %70
  call void @__quantum__rt__string_reference(%String* %71)
  call void @__quantum__rt__tuple_reference(%Tuple* %63)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %24, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %20, %Tuple* null)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %20)
  call void @__quantum__rt__tuple_remove_access(%Tuple* %24)
  call void @__quantum__rt__tuple_unreference(%Tuple* %20)
  call void @__quantum__rt__string_unreference(%String* %23)
  %72 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %73 = load %String*, %String** %72
  call void @__quantum__rt__string_unreference(%String* %73)
  call void @__quantum__rt__tuple_unreference(%Tuple* %24)
  call void @__quantum__rt__callable_unreference(%Callable* %27)
  %74 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 0
  %75 = load %Callable*, %Callable** %74
  call void @__quantum__rt__callable_unreference(%Callable* %75)
  %76 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 1
  %77 = load { i64, double }*, { i64, double }** %76
  %78 = bitcast { i64, double }* %77 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %78)
  call void @__quantum__rt__tuple_unreference(%Tuple* %28)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %37)
  %79 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %39, i64 0, i32 0
  %80 = load %Callable*, %Callable** %79
  call void @__quantum__rt__callable_unreference(%Callable* %80)
  %81 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %39, i64 0, i32 1
  %82 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %81
  %83 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %82, i64 0, i32 0
  %84 = load %String*, %String** %83
  call void @__quantum__rt__string_unreference(%String* %84)
  %85 = bitcast { %String*, %Qubit* }* %82 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %85)
  call void @__quantum__rt__tuple_unreference(%Tuple* %38)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %51)
  %86 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %53, i64 0, i32 0
  %87 = load %Callable*, %Callable** %86
  call void @__quantum__rt__callable_unreference(%Callable* %87)
  %88 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %53, i64 0, i32 1
  %89 = load { i64, double }*, { i64, double }** %88
  %90 = bitcast { i64, double }* %89 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %90)
  call void @__quantum__rt__tuple_unreference(%Tuple* %52)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__callable_unreference(%Callable* %61)
  call void @__quantum__rt__string_unreference(%String* %62)
  %91 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 0
  %92 = load %Callable*, %Callable** %91
  call void @__quantum__rt__callable_unreference(%Callable* %92)
  %93 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %64, i64 0, i32 1
  %94 = load %String*, %String** %93
  call void @__quantum__rt__string_unreference(%String* %94)
  call void @__quantum__rt__tuple_unreference(%Tuple* %63)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %95 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  %96 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %96)
  %97 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %98 = bitcast %Tuple* %97 to { %Callable*, %Callable* }*
  %99 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %98, i64 0, i32 0
  %100 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %98, i64 0, i32 1
  store %Callable* %95, %Callable** %99
  call void @__quantum__rt__callable_reference(%Callable* %95)
  store %Callable* %96, %Callable** %100
  call void @__quantum__rt__callable_reference(%Callable* %96)
  %101 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %97)
  %102 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %98, i64 0, i32 0
  %103 = load %Callable*, %Callable** %102
  call void @__quantum__rt__callable_reference(%Callable* %103)
  %104 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %98, i64 0, i32 1
  %105 = load %Callable*, %Callable** %104
  call void @__quantum__rt__callable_reference(%Callable* %105)
  call void @__quantum__rt__tuple_reference(%Tuple* %97)
  %106 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %107 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %106, i64 0)
  %108 = bitcast i8* %107 to %Qubit**
  store %Qubit* %qb, %Qubit** %108
  %109 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %106)
  %110 = bitcast { %Array* }* %109 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %101, %Tuple* %110, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__tuple_unreference(%Tuple* %10)
  call void @__quantum__rt__tuple_unreference(%Tuple* %13)
  call void @__quantum__rt__result_unreference(%Result* %16)
  call void @__quantum__rt__callable_unreference(%Callable* %95)
  call void @__quantum__rt__callable_unreference(%Callable* %96)
  %111 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %98, i64 0, i32 0
  %112 = load %Callable*, %Callable** %111
  call void @__quantum__rt__callable_unreference(%Callable* %112)
  %113 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %98, i64 0, i32 1
  %114 = load %Callable*, %Callable** %113
  call void @__quantum__rt__callable_unreference(%Callable* %114)
  call void @__quantum__rt__tuple_unreference(%Tuple* %97)
  call void @__quantum__rt__callable_unreference(%Callable* %101)
  call void @__quantum__rt__array_unreference(%Array* %106)
  %115 = getelementptr { %Array* }, { %Array* }* %109, i64 0, i32 0
  %116 = load %Array*, %Array** %115
  call void @__quantum__rt__array_unreference(%Array* %116)
  call void @__quantum__rt__tuple_unreference(%Tuple* %110)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %117 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %118 = getelementptr { %Callable*, double }, { %Callable*, double }* %2, i64 0, i32 0
  %119 = load %Callable*, %Callable** %118
  call void @__quantum__rt__callable_unreference(%Callable* %119)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
