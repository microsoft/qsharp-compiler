define i64 @Microsoft__Quantum__Testing__QIR__TestControlled__body() #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, i64 }*
  %2 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %1, i64 0, i32 0
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Qop, %Tuple* null)
  store %Callable* %3, %Callable** %2
  call void @__quantum__rt__callable_reference(%Callable* %3)
  %4 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %1, i64 0, i32 1
  store i64 1, i64* %4
  %qop = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
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
  %8 = bitcast { %Qubit* }* %6 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %qop, %Tuple* %8, %Tuple* null)
  %9 = call %Result* @__quantum__qis__mz(%Qubit* %q1)
  %10 = load %Result*, %Result** @ResultOne
  %11 = call i1 @__quantum__rt__result_equal(%Result* %9, %Result* %10)
  %12 = xor i1 %11, true
  br i1 %12, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  store i64 1, i64* %error_code
  br label %continue__1

else__1:                                          ; preds = %entry
  %13 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %14 = bitcast %Tuple* %13 to { %Qubit* }*
  %15 = getelementptr { %Qubit* }, { %Qubit* }* %14, i64 0, i32 0
  store %Qubit* %q2, %Qubit** %15
  %16 = bitcast { %Qubit* }* %14 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %adj_qop, %Tuple* %16, %Tuple* null)
  %17 = call %Result* @__quantum__qis__mz(%Qubit* %q2)
  %18 = load %Result*, %Result** @ResultOne
  %19 = call i1 @__quantum__rt__result_equal(%Result* %17, %Result* %18)
  %20 = xor i1 %19, true
  br i1 %20, label %then0__2, label %else__2

then0__2:                                         ; preds = %else__1
  store i64 2, i64* %error_code
  br label %continue__2

else__2:                                          ; preds = %else__1
  %21 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %21, i64 0)
  %23 = bitcast i8* %22 to %Qubit**
  store %Qubit* %q1, %Qubit** %23
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %25 = bitcast %Tuple* %24 to { %Array*, %Qubit* }*
  %26 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %25, i64 0, i32 0
  %27 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %25, i64 0, i32 1
  store %Array* %21, %Array** %26
  call void @__quantum__rt__array_reference(%Array* %21)
  store %Qubit* %q3, %Qubit** %27
  %28 = bitcast { %Array*, %Qubit* }* %25 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %28, %Tuple* null)
  %29 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %30 = load %Result*, %Result** @ResultOne
  %31 = call i1 @__quantum__rt__result_equal(%Result* %29, %Result* %30)
  %32 = xor i1 %31, true
  br i1 %32, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code
  br label %continue__3

else__3:                                          ; preds = %else__2
  %33 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %34 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %33, i64 0)
  %35 = bitcast i8* %34 to %Qubit**
  store %Qubit* %q2, %Qubit** %35
  %36 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %37 = bitcast %Tuple* %36 to { %Array*, %Qubit* }*
  %38 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %37, i64 0, i32 0
  %39 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %37, i64 0, i32 1
  store %Array* %33, %Array** %38
  call void @__quantum__rt__array_reference(%Array* %33)
  store %Qubit* %q3, %Qubit** %39
  %40 = bitcast { %Array*, %Qubit* }* %37 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %40, %Tuple* null)
  %41 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %42 = load %Result*, %Result** @ResultZero
  %43 = call i1 @__quantum__rt__result_equal(%Result* %41, %Result* %42)
  %44 = xor i1 %43, true
  br i1 %44, label %then0__4, label %else__4

then0__4:                                         ; preds = %else__3
  store i64 4, i64* %error_code
  br label %continue__4

else__4:                                          ; preds = %else__3
  %45 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %46 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %45, i64 0)
  %47 = bitcast i8* %46 to %Qubit**
  store %Qubit* %q1, %Qubit** %47
  %48 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %49 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %48, i64 0)
  %50 = bitcast i8* %49 to %Qubit**
  store %Qubit* %q2, %Qubit** %50
  %51 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %52 = bitcast %Tuple* %51 to { %Array*, %Qubit* }*
  %53 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %52, i64 0, i32 0
  %54 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %52, i64 0, i32 1
  store %Array* %48, %Array** %53
  call void @__quantum__rt__array_reference(%Array* %48)
  store %Qubit* %q3, %Qubit** %54
  %55 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %56 = bitcast %Tuple* %55 to { %Array*, { %Array*, %Qubit* }* }*
  %57 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %56, i64 0, i32 0
  %58 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %56, i64 0, i32 1
  store %Array* %45, %Array** %57
  call void @__quantum__rt__array_reference(%Array* %45)
  store { %Array*, %Qubit* }* %52, { %Array*, %Qubit* }** %58
  %59 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %52, i64 0, i32 0
  %60 = load %Array*, %Array** %59
  call void @__quantum__rt__array_reference(%Array* %60)
  %61 = bitcast { %Array*, %Qubit* }* %52 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %61)
  %62 = bitcast { %Array*, { %Array*, %Qubit* }* }* %56 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %62, %Tuple* null)
  %63 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %64 = load %Result*, %Result** @ResultOne
  %65 = call i1 @__quantum__rt__result_equal(%Result* %63, %Result* %64)
  %66 = xor i1 %65, true
  br i1 %66, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code
  br label %continue__5

else__5:                                          ; preds = %else__4
  %67 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %67)
  %68 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %69 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %68, i64 0)
  %70 = bitcast i8* %69 to %Qubit**
  store %Qubit* %q1, %Qubit** %70
  %71 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %68, i64 1)
  %72 = bitcast i8* %71 to %Qubit**
  store %Qubit* %q2, %Qubit** %72
  %73 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %74 = bitcast %Tuple* %73 to { %Array*, %Qubit* }*
  %75 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %74, i64 0, i32 0
  %76 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %74, i64 0, i32 1
  store %Array* %68, %Array** %75
  call void @__quantum__rt__array_reference(%Array* %68)
  store %Qubit* %q3, %Qubit** %76
  %77 = bitcast { %Array*, %Qubit* }* %74 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %67, %Tuple* %77, %Tuple* null)
  %78 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %79 = load %Result*, %Result** @ResultZero
  %80 = call i1 @__quantum__rt__result_equal(%Result* %78, %Result* %79)
  %81 = xor i1 %80, true
  br i1 %81, label %then0__6, label %else__6

then0__6:                                         ; preds = %else__5
  store i64 6, i64* %error_code
  br label %continue__6

else__6:                                          ; preds = %else__5
  %q4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %82 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %82)
  %83 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %84 = bitcast %Tuple* %83 to { %Qubit* }*
  %85 = getelementptr { %Qubit* }, { %Qubit* }* %84, i64 0, i32 0
  store %Qubit* %q3, %Qubit** %85
  %86 = bitcast { %Qubit* }* %84 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %82, %Tuple* %86, %Tuple* null)
  %87 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop)
  call void @__quantum__rt__callable_make_controlled(%Callable* %87)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %87)
  %88 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %89 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %88, i64 0)
  %90 = bitcast i8* %89 to %Qubit**
  store %Qubit* %q1, %Qubit** %90
  %91 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %92 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %91, i64 0)
  %93 = bitcast i8* %92 to %Qubit**
  store %Qubit* %q2, %Qubit** %93
  %94 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %95 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %94, i64 0)
  %96 = bitcast i8* %95 to %Qubit**
  store %Qubit* %q3, %Qubit** %96
  %97 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %98 = bitcast %Tuple* %97 to { %Array*, %Qubit* }*
  %99 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %98, i64 0, i32 0
  %100 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %98, i64 0, i32 1
  store %Array* %94, %Array** %99
  call void @__quantum__rt__array_reference(%Array* %94)
  store %Qubit* %q4, %Qubit** %100
  %101 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %102 = bitcast %Tuple* %101 to { %Array*, { %Array*, %Qubit* }* }*
  %103 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %102, i64 0, i32 0
  %104 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %102, i64 0, i32 1
  store %Array* %91, %Array** %103
  call void @__quantum__rt__array_reference(%Array* %91)
  store { %Array*, %Qubit* }* %98, { %Array*, %Qubit* }** %104
  %105 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %98, i64 0, i32 0
  %106 = load %Array*, %Array** %105
  call void @__quantum__rt__array_reference(%Array* %106)
  %107 = bitcast { %Array*, %Qubit* }* %98 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %107)
  %108 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %109 = bitcast %Tuple* %108 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %110 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %109, i64 0, i32 0
  %111 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %109, i64 0, i32 1
  store %Array* %88, %Array** %110
  call void @__quantum__rt__array_reference(%Array* %88)
  store { %Array*, { %Array*, %Qubit* }* }* %102, { %Array*, { %Array*, %Qubit* }* }** %111
  %112 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %102, i64 0, i32 0
  %113 = load %Array*, %Array** %112
  call void @__quantum__rt__array_reference(%Array* %113)
  %114 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %102, i64 0, i32 1
  %115 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %114
  %116 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %115, i64 0, i32 0
  %117 = load %Array*, %Array** %116
  call void @__quantum__rt__array_reference(%Array* %117)
  %118 = bitcast { %Array*, %Qubit* }* %115 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %118)
  %119 = bitcast { %Array*, { %Array*, %Qubit* }* }* %102 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %119)
  %120 = bitcast { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %109 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %87, %Tuple* %120, %Tuple* null)
  %121 = call %Result* @__quantum__qis__mz(%Qubit* %q4)
  %122 = load %Result*, %Result** @ResultOne
  %123 = call i1 @__quantum__rt__result_equal(%Result* %121, %Result* %122)
  %124 = xor i1 %123, true
  br i1 %124, label %then0__7, label %continue__7

then0__7:                                         ; preds = %else__6
  store i64 7, i64* %error_code
  br label %continue__7

continue__7:                                      ; preds = %then0__7, %else__6
  call void @__quantum__rt__qubit_release(%Qubit* %q4)
  call void @__quantum__rt__callable_unreference(%Callable* %82)
  %125 = bitcast { %Qubit* }* %84 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %125)
  call void @__quantum__rt__callable_unreference(%Callable* %87)
  call void @__quantum__rt__array_unreference(%Array* %88)
  call void @__quantum__rt__array_unreference(%Array* %91)
  call void @__quantum__rt__array_unreference(%Array* %94)
  %126 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %98, i64 0, i32 0
  %127 = load %Array*, %Array** %126
  call void @__quantum__rt__array_unreference(%Array* %127)
  %128 = bitcast { %Array*, %Qubit* }* %98 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %128)
  %129 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %102, i64 0, i32 0
  %130 = load %Array*, %Array** %129
  call void @__quantum__rt__array_unreference(%Array* %130)
  %131 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %102, i64 0, i32 1
  %132 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %131
  %133 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %132, i64 0, i32 0
  %134 = load %Array*, %Array** %133
  call void @__quantum__rt__array_unreference(%Array* %134)
  %135 = bitcast { %Array*, %Qubit* }* %132 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %135)
  %136 = bitcast { %Array*, { %Array*, %Qubit* }* }* %102 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %136)
  %137 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %109, i64 0, i32 0
  %138 = load %Array*, %Array** %137
  call void @__quantum__rt__array_unreference(%Array* %138)
  %139 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %109, i64 0, i32 1
  %140 = load { %Array*, { %Array*, %Qubit* }* }*, { %Array*, { %Array*, %Qubit* }* }** %139
  %141 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %140, i64 0, i32 0
  %142 = load %Array*, %Array** %141
  call void @__quantum__rt__array_unreference(%Array* %142)
  %143 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %140, i64 0, i32 1
  %144 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %143
  %145 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %144, i64 0, i32 0
  %146 = load %Array*, %Array** %145
  call void @__quantum__rt__array_unreference(%Array* %146)
  %147 = bitcast { %Array*, %Qubit* }* %144 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %147)
  %148 = bitcast { %Array*, { %Array*, %Qubit* }* }* %140 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %148)
  %149 = bitcast { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %109 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %149)
  call void @__quantum__rt__result_unreference(%Result* %121)
  call void @__quantum__rt__result_unreference(%Result* %122)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  call void @__quantum__rt__callable_unreference(%Callable* %67)
  call void @__quantum__rt__array_unreference(%Array* %68)
  %150 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %74, i64 0, i32 0
  %151 = load %Array*, %Array** %150
  call void @__quantum__rt__array_unreference(%Array* %151)
  %152 = bitcast { %Array*, %Qubit* }* %74 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %152)
  call void @__quantum__rt__result_unreference(%Result* %78)
  call void @__quantum__rt__result_unreference(%Result* %79)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_unreference(%Array* %45)
  call void @__quantum__rt__array_unreference(%Array* %48)
  %153 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %52, i64 0, i32 0
  %154 = load %Array*, %Array** %153
  call void @__quantum__rt__array_unreference(%Array* %154)
  %155 = bitcast { %Array*, %Qubit* }* %52 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %155)
  %156 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %56, i64 0, i32 0
  %157 = load %Array*, %Array** %156
  call void @__quantum__rt__array_unreference(%Array* %157)
  %158 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %56, i64 0, i32 1
  %159 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %158
  %160 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %159, i64 0, i32 0
  %161 = load %Array*, %Array** %160
  call void @__quantum__rt__array_unreference(%Array* %161)
  %162 = bitcast { %Array*, %Qubit* }* %159 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %162)
  %163 = bitcast { %Array*, { %Array*, %Qubit* }* }* %56 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %163)
  call void @__quantum__rt__result_unreference(%Result* %63)
  call void @__quantum__rt__result_unreference(%Result* %64)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_unreference(%Array* %33)
  %164 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %37, i64 0, i32 0
  %165 = load %Array*, %Array** %164
  call void @__quantum__rt__array_unreference(%Array* %165)
  %166 = bitcast { %Array*, %Qubit* }* %37 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %166)
  call void @__quantum__rt__result_unreference(%Result* %41)
  call void @__quantum__rt__result_unreference(%Result* %42)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_unreference(%Array* %21)
  %167 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %25, i64 0, i32 0
  %168 = load %Array*, %Array** %167
  call void @__quantum__rt__array_unreference(%Array* %168)
  %169 = bitcast { %Array*, %Qubit* }* %25 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %169)
  call void @__quantum__rt__result_unreference(%Result* %29)
  call void @__quantum__rt__result_unreference(%Result* %30)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  %170 = bitcast { %Qubit* }* %14 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %170)
  call void @__quantum__rt__result_unreference(%Result* %17)
  call void @__quantum__rt__result_unreference(%Result* %18)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  %171 = bitcast { %Qubit* }* %6 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %171)
  call void @__quantum__rt__result_unreference(%Result* %9)
  call void @__quantum__rt__result_unreference(%Result* %10)
  %172 = load i64, i64* %error_code
  call void @__quantum__rt__callable_unreference(%Callable* %3)
  call void @__quantum__rt__callable_unreference(%Callable* %qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_ctl_qop)
  ret i64 %172
}
