define i64 @Microsoft__Quantum__Testing__QIR__TestControlled__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Qop, %Tuple* null)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { %Callable*, i64 }*
  %3 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %2, i64 0, i32 0
  %4 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %2, i64 0, i32 1
  store %Callable* %0, %Callable** %3
  call void @__quantum__rt__callable_reference(%Callable* %0)
  store i64 1, i64* %4
  %qop = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %1)
  %adj_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_qop)
  %ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_qop)
  %adj_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_ctl_qop)
  %ctl_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_ctl_qop)
  %error_code = alloca i64
  store i64 0, i64* %error_code
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { %Qubit* }*
  %7 = getelementptr { %Qubit* }, { %Qubit* }* %6, i64 0, i32 0
  store %Qubit* %q1, %Qubit** %7
  call void @__quantum__rt__callable_invoke(%Callable* %qop, %Tuple* %5, %Tuple* null)
  %8 = call %Result* @__quantum__qis__mz(%Qubit* %q1)
  %9 = load %Result*, %Result** @ResultOne
  %10 = call i1 @__quantum__rt__result_equal(%Result* %8, %Result* %9)
  %11 = xor i1 %10, true
  br i1 %11, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  store i64 1, i64* %error_code
  br label %continue__1

else__1:                                          ; preds = %entry
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %Qubit* }*
  %14 = getelementptr { %Qubit* }, { %Qubit* }* %13, i64 0, i32 0
  store %Qubit* %q2, %Qubit** %14
  call void @__quantum__rt__callable_invoke(%Callable* %adj_qop, %Tuple* %12, %Tuple* null)
  %15 = call %Result* @__quantum__qis__mz(%Qubit* %q2)
  %16 = load %Result*, %Result** @ResultOne
  %17 = call i1 @__quantum__rt__result_equal(%Result* %15, %Result* %16)
  %18 = xor i1 %17, true
  br i1 %18, label %then0__2, label %else__2

then0__2:                                         ; preds = %else__1
  store i64 2, i64* %error_code
  br label %continue__2

else__2:                                          ; preds = %else__1
  %19 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 0)
  %21 = bitcast i8* %20 to %Qubit**
  store %Qubit* %q1, %Qubit** %21
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %23 = bitcast %Tuple* %22 to { %Array*, %Qubit* }*
  %24 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %23, i64 0, i32 0
  %25 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %23, i64 0, i32 1
  store %Array* %19, %Array** %24
  call void @__quantum__rt__array_reference(%Array* %19)
  store %Qubit* %q3, %Qubit** %25
  %26 = bitcast { %Array*, %Qubit* }* %23 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %26, %Tuple* null)
  %27 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %28 = load %Result*, %Result** @ResultOne
  %29 = call i1 @__quantum__rt__result_equal(%Result* %27, %Result* %28)
  %30 = xor i1 %29, true
  br i1 %30, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code
  br label %continue__3

else__3:                                          ; preds = %else__2
  %31 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %32 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %31, i64 0)
  %33 = bitcast i8* %32 to %Qubit**
  store %Qubit* %q2, %Qubit** %33
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %35 = bitcast %Tuple* %34 to { %Array*, %Qubit* }*
  %36 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %35, i64 0, i32 0
  %37 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %35, i64 0, i32 1
  store %Array* %31, %Array** %36
  call void @__quantum__rt__array_reference(%Array* %31)
  store %Qubit* %q3, %Qubit** %37
  %38 = bitcast { %Array*, %Qubit* }* %35 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %38, %Tuple* null)
  %39 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %40 = load %Result*, %Result** @ResultZero
  %41 = call i1 @__quantum__rt__result_equal(%Result* %39, %Result* %40)
  %42 = xor i1 %41, true
  br i1 %42, label %then0__4, label %else__4

then0__4:                                         ; preds = %else__3
  store i64 4, i64* %error_code
  br label %continue__4

else__4:                                          ; preds = %else__3
  %43 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %44 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %43, i64 0)
  %45 = bitcast i8* %44 to %Qubit**
  store %Qubit* %q1, %Qubit** %45
  %46 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %47 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %46, i64 0)
  %48 = bitcast i8* %47 to %Qubit**
  store %Qubit* %q2, %Qubit** %48
  %49 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %50 = bitcast %Tuple* %49 to { %Array*, %Qubit* }*
  %51 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %50, i64 0, i32 0
  %52 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %50, i64 0, i32 1
  store %Array* %46, %Array** %51
  call void @__quantum__rt__array_reference(%Array* %46)
  store %Qubit* %q3, %Qubit** %52
  %53 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %54 = bitcast %Tuple* %53 to { %Array*, { %Array*, %Qubit* }* }*
  %55 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 0
  %56 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 1
  store %Array* %43, %Array** %55
  call void @__quantum__rt__array_reference(%Array* %43)
  store { %Array*, %Qubit* }* %50, { %Array*, %Qubit* }** %56
  %57 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %50, i64 0, i32 0
  %58 = load %Array*, %Array** %57
  call void @__quantum__rt__array_reference(%Array* %58)
  %59 = bitcast { %Array*, %Qubit* }* %50 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %59)
  %60 = bitcast { %Array*, { %Array*, %Qubit* }* }* %54 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %60, %Tuple* null)
  %61 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %62 = load %Result*, %Result** @ResultOne
  %63 = call i1 @__quantum__rt__result_equal(%Result* %61, %Result* %62)
  %64 = xor i1 %63, true
  br i1 %64, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code
  br label %continue__5

else__5:                                          ; preds = %else__4
  %65 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %65)
  %66 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %66, i64 0)
  %68 = bitcast i8* %67 to %Qubit**
  store %Qubit* %q1, %Qubit** %68
  %69 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %66, i64 1)
  %70 = bitcast i8* %69 to %Qubit**
  store %Qubit* %q2, %Qubit** %70
  %71 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %72 = bitcast %Tuple* %71 to { %Array*, %Qubit* }*
  %73 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %72, i64 0, i32 0
  %74 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %72, i64 0, i32 1
  store %Array* %66, %Array** %73
  call void @__quantum__rt__array_reference(%Array* %66)
  store %Qubit* %q3, %Qubit** %74
  %75 = bitcast { %Array*, %Qubit* }* %72 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %65, %Tuple* %75, %Tuple* null)
  %76 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %77 = load %Result*, %Result** @ResultZero
  %78 = call i1 @__quantum__rt__result_equal(%Result* %76, %Result* %77)
  %79 = xor i1 %78, true
  br i1 %79, label %then0__6, label %else__6

then0__6:                                         ; preds = %else__5
  store i64 6, i64* %error_code
  br label %continue__6

else__6:                                          ; preds = %else__5
  %q4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %80 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %80)
  %81 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %82 = bitcast %Tuple* %81 to { %Qubit* }*
  %83 = getelementptr { %Qubit* }, { %Qubit* }* %82, i64 0, i32 0
  store %Qubit* %q3, %Qubit** %83
  call void @__quantum__rt__callable_invoke(%Callable* %80, %Tuple* %81, %Tuple* null)
  %84 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %84)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %84)
  %85 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %86 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %85, i64 0)
  %87 = bitcast i8* %86 to %Qubit**
  store %Qubit* %q1, %Qubit** %87
  %88 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %89 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %88, i64 0)
  %90 = bitcast i8* %89 to %Qubit**
  store %Qubit* %q2, %Qubit** %90
  %91 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %92 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %91, i64 0)
  %93 = bitcast i8* %92 to %Qubit**
  store %Qubit* %q3, %Qubit** %93
  %94 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %95 = bitcast %Tuple* %94 to { %Array*, %Qubit* }*
  %96 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %95, i64 0, i32 0
  %97 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %95, i64 0, i32 1
  store %Array* %91, %Array** %96
  call void @__quantum__rt__array_reference(%Array* %91)
  store %Qubit* %q4, %Qubit** %97
  %98 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %99 = bitcast %Tuple* %98 to { %Array*, { %Array*, %Qubit* }* }*
  %100 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %99, i64 0, i32 0
  %101 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %99, i64 0, i32 1
  store %Array* %88, %Array** %100
  call void @__quantum__rt__array_reference(%Array* %88)
  store { %Array*, %Qubit* }* %95, { %Array*, %Qubit* }** %101
  %102 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %95, i64 0, i32 0
  %103 = load %Array*, %Array** %102
  call void @__quantum__rt__array_reference(%Array* %103)
  %104 = bitcast { %Array*, %Qubit* }* %95 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %104)
  %105 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %106 = bitcast %Tuple* %105 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %107 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %106, i64 0, i32 0
  %108 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %106, i64 0, i32 1
  store %Array* %85, %Array** %107
  call void @__quantum__rt__array_reference(%Array* %85)
  store { %Array*, { %Array*, %Qubit* }* }* %99, { %Array*, { %Array*, %Qubit* }* }** %108
  %109 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %99, i64 0, i32 0
  %110 = load %Array*, %Array** %109
  call void @__quantum__rt__array_reference(%Array* %110)
  %111 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %99, i64 0, i32 1
  %112 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %111
  %113 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %112, i64 0, i32 0
  %114 = load %Array*, %Array** %113
  call void @__quantum__rt__array_reference(%Array* %114)
  %115 = bitcast { %Array*, %Qubit* }* %112 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %115)
  %116 = bitcast { %Array*, { %Array*, %Qubit* }* }* %99 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %116)
  %117 = bitcast { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %106 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %84, %Tuple* %117, %Tuple* null)
  %118 = call %Result* @__quantum__qis__mz(%Qubit* %q4)
  %119 = load %Result*, %Result** @ResultOne
  %120 = call i1 @__quantum__rt__result_equal(%Result* %118, %Result* %119)
  %121 = xor i1 %120, true
  br i1 %121, label %then0__7, label %continue__7

then0__7:                                         ; preds = %else__6
  store i64 7, i64* %error_code
  br label %continue__7

continue__7:                                      ; preds = %then0__7, %else__6
  call void @__quantum__rt__qubit_release(%Qubit* %q4)
  call void @__quantum__rt__callable_unreference(%Callable* %80)
  %122 = bitcast { %Qubit* }* %82 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %122)
  call void @__quantum__rt__callable_unreference(%Callable* %84)
  call void @__quantum__rt__array_unreference(%Array* %85)
  call void @__quantum__rt__array_unreference(%Array* %88)
  call void @__quantum__rt__array_unreference(%Array* %91)
  %123 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %95, i64 0, i32 0
  %124 = load %Array*, %Array** %123
  call void @__quantum__rt__array_unreference(%Array* %124)
  %125 = bitcast { %Array*, %Qubit* }* %95 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %125)
  %126 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %99, i64 0, i32 0
  %127 = load %Array*, %Array** %126
  call void @__quantum__rt__array_unreference(%Array* %127)
  %128 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %99, i64 0, i32 1
  %129 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %128
  %130 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %129, i64 0, i32 0
  %131 = load %Array*, %Array** %130
  call void @__quantum__rt__array_unreference(%Array* %131)
  %132 = bitcast { %Array*, %Qubit* }* %129 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %132)
  %133 = bitcast { %Array*, { %Array*, %Qubit* }* }* %99 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %133)
  %134 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %106, i64 0, i32 0
  %135 = load %Array*, %Array** %134
  call void @__quantum__rt__array_unreference(%Array* %135)
  %136 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %106, i64 0, i32 1
  %137 = load { %Array*, { %Array*, %Qubit* }* }*, { %Array*, { %Array*, %Qubit* }* }** %136
  %138 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %137, i64 0, i32 0
  %139 = load %Array*, %Array** %138
  call void @__quantum__rt__array_unreference(%Array* %139)
  %140 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %137, i64 0, i32 1
  %141 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %140
  %142 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %141, i64 0, i32 0
  %143 = load %Array*, %Array** %142
  call void @__quantum__rt__array_unreference(%Array* %143)
  %144 = bitcast { %Array*, %Qubit* }* %141 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %144)
  %145 = bitcast { %Array*, { %Array*, %Qubit* }* }* %137 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %145)
  %146 = bitcast { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %106 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %146)
  call void @__quantum__rt__result_unreference(%Result* %118)
  call void @__quantum__rt__result_unreference(%Result* %119)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  call void @__quantum__rt__callable_unreference(%Callable* %65)
  call void @__quantum__rt__array_unreference(%Array* %66)
  %147 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %72, i64 0, i32 0
  %148 = load %Array*, %Array** %147
  call void @__quantum__rt__array_unreference(%Array* %148)
  %149 = bitcast { %Array*, %Qubit* }* %72 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %149)
  call void @__quantum__rt__result_unreference(%Result* %76)
  call void @__quantum__rt__result_unreference(%Result* %77)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_unreference(%Array* %43)
  call void @__quantum__rt__array_unreference(%Array* %46)
  %150 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %50, i64 0, i32 0
  %151 = load %Array*, %Array** %150
  call void @__quantum__rt__array_unreference(%Array* %151)
  %152 = bitcast { %Array*, %Qubit* }* %50 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %152)
  %153 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 0
  %154 = load %Array*, %Array** %153
  call void @__quantum__rt__array_unreference(%Array* %154)
  %155 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 1
  %156 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %155
  %157 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %156, i64 0, i32 0
  %158 = load %Array*, %Array** %157
  call void @__quantum__rt__array_unreference(%Array* %158)
  %159 = bitcast { %Array*, %Qubit* }* %156 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %159)
  %160 = bitcast { %Array*, { %Array*, %Qubit* }* }* %54 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %160)
  call void @__quantum__rt__result_unreference(%Result* %61)
  call void @__quantum__rt__result_unreference(%Result* %62)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_unreference(%Array* %31)
  %161 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %35, i64 0, i32 0
  %162 = load %Array*, %Array** %161
  call void @__quantum__rt__array_unreference(%Array* %162)
  %163 = bitcast { %Array*, %Qubit* }* %35 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %163)
  call void @__quantum__rt__result_unreference(%Result* %39)
  call void @__quantum__rt__result_unreference(%Result* %40)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_unreference(%Array* %19)
  %164 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %23, i64 0, i32 0
  %165 = load %Array*, %Array** %164
  call void @__quantum__rt__array_unreference(%Array* %165)
  %166 = bitcast { %Array*, %Qubit* }* %23 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %166)
  call void @__quantum__rt__result_unreference(%Result* %27)
  call void @__quantum__rt__result_unreference(%Result* %28)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  %167 = bitcast { %Qubit* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %167)
  call void @__quantum__rt__result_unreference(%Result* %15)
  call void @__quantum__rt__result_unreference(%Result* %16)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  %168 = bitcast { %Qubit* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %168)
  call void @__quantum__rt__result_unreference(%Result* %8)
  call void @__quantum__rt__result_unreference(%Result* %9)
  %169 = load i64, i64* %error_code
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %170 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %2, i64 0, i32 0
  %171 = load %Callable*, %Callable** %170
  call void @__quantum__rt__callable_unreference(%Callable* %171)
  %172 = bitcast { %Callable*, i64 }* %2 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %172)
  call void @__quantum__rt__callable_unreference(%Callable* %qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_ctl_qop)
  ret i64 %169
}