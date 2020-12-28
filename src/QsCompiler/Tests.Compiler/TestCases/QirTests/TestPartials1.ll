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
  call void @__quantum__rt__array_reference(%Array* %4)
  %5 = bitcast { double, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %5)
  call void @__quantum__rt__array_reference(%Array* %4)
  %6 = bitcast { double, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %6)
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %157, %exiting__1 ]
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
  %13 = bitcast { %Qubit* }* %11 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %13, %Tuple* null)
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { %Qubit* }*
  %16 = getelementptr { %Qubit* }, { %Qubit* }* %15, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %16
  %17 = bitcast { %Qubit* }* %15 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %17, %Tuple* null)
  %18 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %19 = load %Result*, %Result** @ResultZero
  %20 = call i1 @__quantum__rt__result_equal(%Result* %18, %Result* %19)
  %21 = xor i1 %20, true
  br i1 %21, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %22 to { i64, double }*
  %23 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 0
  %24 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 1
  store i64 %a, i64* %23
  store double %b, double* %24
  %25 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %26 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %26 to { %String*, %Qubit* }*
  %27 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %28 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  store %String* %25, %String** %27
  call void @__quantum__rt__string_reference(%String* %25)
  store %Qubit* %qb, %Qubit** %28
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %30 = bitcast %Tuple* %29 to { %Callable*, { i64, double }* }*
  %31 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %30, i64 0, i32 0
  %32 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  store %Callable* %32, %Callable** %31
  call void @__quantum__rt__callable_reference(%Callable* %32)
  %33 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %30, i64 0, i32 1
  store { i64, double }* %tuple1, { i64, double }** %33
  %34 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %34)
  call void @__quantum__rt__array_reference(%Array* %4)
  %35 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %35)
  %36 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %37 = load { i64, double }*, { i64, double }** %36
  %38 = bitcast { i64, double }* %37 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %38)
  %39 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %40 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %39
  %41 = bitcast { %String*, %Qubit* }* %40 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %41)
  %42 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %40, i64 0, i32 0
  %43 = load %String*, %String** %42
  call void @__quantum__rt__string_reference(%String* %43)
  call void @__quantum__rt__array_reference(%Array* %4)
  %44 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %44)
  %45 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %46 = load { i64, double }*, { i64, double }** %45
  %47 = bitcast { i64, double }* %46 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %47)
  %48 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %49 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %48
  %50 = bitcast { %String*, %Qubit* }* %49 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %50)
  %51 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %49, i64 0, i32 0
  %52 = load %String*, %String** %51
  call void @__quantum__rt__string_reference(%String* %52)
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %29)
  %53 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %54 = bitcast %Tuple* %53 to { %Callable*, { %String*, %Qubit* }* }*
  %55 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %54, i64 0, i32 0
  %56 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  store %Callable* %56, %Callable** %55
  call void @__quantum__rt__callable_reference(%Callable* %56)
  %57 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %54, i64 0, i32 1
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %57
  %58 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %58)
  %59 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %60 = load %String*, %String** %59
  call void @__quantum__rt__string_reference(%String* %60)
  call void @__quantum__rt__array_reference(%Array* %4)
  %61 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %61)
  %62 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %63 = load { i64, double }*, { i64, double }** %62
  %64 = bitcast { i64, double }* %63 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %64)
  %65 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %66 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %65
  %67 = bitcast { %String*, %Qubit* }* %66 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %67)
  %68 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %66, i64 0, i32 0
  %69 = load %String*, %String** %68
  call void @__quantum__rt__string_reference(%String* %69)
  call void @__quantum__rt__array_reference(%Array* %4)
  %70 = bitcast { { i64, double }*, { %String*, %Qubit* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %70)
  %71 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 0
  %72 = load { i64, double }*, { i64, double }** %71
  %73 = bitcast { i64, double }* %72 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %73)
  %74 = getelementptr { { i64, double }*, { %String*, %Qubit* }* }, { { i64, double }*, { %String*, %Qubit* }* }* %7, i64 0, i32 1
  %75 = load { %String*, %Qubit* }*, { %String*, %Qubit* }** %74
  %76 = bitcast { %String*, %Qubit* }* %75 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %76)
  %77 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %75, i64 0, i32 0
  %78 = load %String*, %String** %77
  call void @__quantum__rt__string_reference(%String* %78)
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %53)
  %79 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %79, %Tuple* null)
  %80 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %80, %Tuple* null)
  %81 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %82 = bitcast %Tuple* %81 to { %Callable*, { i64, double }* }*
  %83 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %82, i64 0, i32 0
  %84 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  store %Callable* %84, %Callable** %83
  call void @__quantum__rt__callable_reference(%Callable* %84)
  %85 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %82, i64 0, i32 1
  store { i64, double }* %tuple1, { i64, double }** %85
  %86 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %86)
  call void @__quantum__rt__array_reference(%Array* %4)
  %87 = bitcast { { i64, double }*, %String*, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %87)
  %88 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 0
  %89 = load { i64, double }*, { i64, double }** %88
  %90 = bitcast { i64, double }* %89 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %90)
  %91 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 1
  %92 = load %String*, %String** %91
  call void @__quantum__rt__string_reference(%String* %92)
  call void @__quantum__rt__array_reference(%Array* %4)
  %93 = bitcast { { i64, double }*, %String*, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %93)
  %94 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 0
  %95 = load { i64, double }*, { i64, double }** %94
  %96 = bitcast { i64, double }* %95 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %96)
  %97 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 1
  %98 = load %String*, %String** %97
  call void @__quantum__rt__string_reference(%String* %98)
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %81)
  %99 = call %String* @__quantum__rt__string_create(i32 0, [0 x i8] zeroinitializer)
  %100 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %101 = bitcast %Tuple* %100 to { %Callable*, %String*, %Qubit* }*
  %102 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %101, i64 0, i32 0
  %103 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  store %Callable* %103, %Callable** %102
  call void @__quantum__rt__callable_reference(%Callable* %103)
  %104 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %101, i64 0, i32 1
  store %String* %99, %String** %104
  call void @__quantum__rt__string_reference(%String* %99)
  %105 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %101, i64 0, i32 2
  store %Qubit* %qb, %Qubit** %105
  call void @__quantum__rt__array_reference(%Array* %4)
  %106 = bitcast { { i64, double }*, %String*, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %106)
  %107 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 0
  %108 = load { i64, double }*, { i64, double }** %107
  %109 = bitcast { i64, double }* %108 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %109)
  %110 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 1
  %111 = load %String*, %String** %110
  call void @__quantum__rt__string_reference(%String* %111)
  call void @__quantum__rt__array_reference(%Array* %4)
  %112 = bitcast { { i64, double }*, %String*, %Qubit* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %112)
  %113 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 0
  %114 = load { i64, double }*, { i64, double }** %113
  %115 = bitcast { i64, double }* %114 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %115)
  %116 = getelementptr { { i64, double }*, %String*, %Qubit* }, { { i64, double }*, %String*, %Qubit* }* %7, i64 0, i32 1
  %117 = load %String*, %String** %116
  call void @__quantum__rt__string_reference(%String* %117)
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %100)
  %118 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %118, %Tuple* null)
  %119 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %119, %Tuple* null)
  %120 = bitcast { i64, double }* %tuple1 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %120)
  call void @__quantum__rt__string_unreference(%String* %25)
  %121 = bitcast { %String*, %Qubit* }* %tuple2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %121)
  %122 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %123 = load %String*, %String** %122
  call void @__quantum__rt__string_unreference(%String* %123)
  call void @__quantum__rt__callable_unreference(%Callable* %32)
  call void @__quantum__rt__callable_unreference(%Callable* %partial1)
  call void @__quantum__rt__callable_unreference(%Callable* %56)
  call void @__quantum__rt__callable_unreference(%Callable* %partial2)
  call void @__quantum__rt__callable_unreference(%Callable* %84)
  call void @__quantum__rt__callable_unreference(%Callable* %partial3)
  call void @__quantum__rt__string_unreference(%String* %99)
  call void @__quantum__rt__callable_unreference(%Callable* %103)
  call void @__quantum__rt__callable_unreference(%Callable* %partial4)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %124 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %124)
  %125 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %126 = bitcast %Tuple* %125 to { %Callable*, %Callable* }*
  %127 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %126, i64 0, i32 0
  %128 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  store %Callable* %128, %Callable** %127
  call void @__quantum__rt__callable_reference(%Callable* %128)
  %129 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %126, i64 0, i32 1
  store %Callable* %124, %Callable** %129
  call void @__quantum__rt__callable_reference(%Callable* %124)
  call void @__quantum__rt__array_reference(%Array* %4)
  %130 = bitcast { %Callable*, { %Array* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %130)
  %131 = getelementptr { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %7, i64 0, i32 0
  %132 = load %Callable*, %Callable** %131
  call void @__quantum__rt__callable_reference(%Callable* %132)
  %133 = getelementptr { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %7, i64 0, i32 1
  %134 = load { %Array* }*, { %Array* }** %133
  %135 = bitcast { %Array* }* %134 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %135)
  %136 = getelementptr { %Array* }, { %Array* }* %134, i64 0, i32 0
  %137 = load %Array*, %Array** %136
  call void @__quantum__rt__array_reference(%Array* %137)
  call void @__quantum__rt__array_reference(%Array* %4)
  %138 = bitcast { %Callable*, { %Array* }* }* %7 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %138)
  %139 = getelementptr { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %7, i64 0, i32 0
  %140 = load %Callable*, %Callable** %139
  call void @__quantum__rt__callable_reference(%Callable* %140)
  %141 = getelementptr { %Callable*, { %Array* }* }, { %Callable*, { %Array* }* }* %7, i64 0, i32 1
  %142 = load { %Array* }*, { %Array* }** %141
  %143 = bitcast { %Array* }* %142 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %143)
  %144 = getelementptr { %Array* }, { %Array* }* %142, i64 0, i32 0
  %145 = load %Array*, %Array** %144
  call void @__quantum__rt__array_reference(%Array* %145)
  %146 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %125)
  %147 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %148 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %147, i64 0)
  %149 = bitcast i8* %148 to %Qubit**
  store %Qubit* %qb, %Qubit** %149
  %150 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %147)
  %151 = bitcast { %Array* }* %150 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %146, %Tuple* %151, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %152 = bitcast { %Qubit* }* %11 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %152)
  %153 = bitcast { %Qubit* }* %15 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %153)
  call void @__quantum__rt__result_unreference(%Result* %18)
  call void @__quantum__rt__result_unreference(%Result* %19)
  call void @__quantum__rt__callable_unreference(%Callable* %124)
  call void @__quantum__rt__callable_unreference(%Callable* %128)
  call void @__quantum__rt__callable_unreference(%Callable* %146)
  call void @__quantum__rt__array_unreference(%Array* %147)
  %154 = bitcast { %Array* }* %150 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %154)
  %155 = getelementptr { %Array* }, { %Array* }* %150, i64 0, i32 0
  %156 = load %Array*, %Array** %155
  call void @__quantum__rt__array_unreference(%Array* %156)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %157 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %rotate)
  call void @__quantum__rt__callable_unreference(%Callable* %unrotate)
  ret i1 true
}
