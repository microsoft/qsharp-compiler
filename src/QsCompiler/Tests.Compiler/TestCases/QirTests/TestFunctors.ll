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
  %5 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %2, i64 0, i32 0
  %6 = load %Callable*, %Callable** %5
  call void @__quantum__rt__callable_reference(%Callable* %6)
  call void @__quantum__rt__tuple_reference(%Tuple* %1)
  %adj_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_qop)
  %ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_qop)
  %adj_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_ctl_qop)
  %ctl_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_ctl_qop)
  %error_code = alloca i64
  store i64 0, i64* %error_code
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %7 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %8 = bitcast %Tuple* %7 to { %Qubit* }*
  %9 = getelementptr { %Qubit* }, { %Qubit* }* %8, i64 0, i32 0
  store %Qubit* %q1, %Qubit** %9
  call void @__quantum__rt__callable_invoke(%Callable* %qop, %Tuple* %7, %Tuple* null)
  %10 = call %Result* @__quantum__qis__mz(%Qubit* %q1)
  %11 = load %Result*, %Result** @ResultOne
  %12 = call i1 @__quantum__rt__result_equal(%Result* %10, %Result* %11)
  %13 = xor i1 %12, true
  br i1 %13, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  store i64 1, i64* %error_code
  br label %continue__1

else__1:                                          ; preds = %entry
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { %Qubit* }*
  %16 = getelementptr { %Qubit* }, { %Qubit* }* %15, i64 0, i32 0
  store %Qubit* %q2, %Qubit** %16
  call void @__quantum__rt__callable_invoke(%Callable* %adj_qop, %Tuple* %14, %Tuple* null)
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
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %24, %Tuple* null)
  %28 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %29 = load %Result*, %Result** @ResultOne
  %30 = call i1 @__quantum__rt__result_equal(%Result* %28, %Result* %29)
  %31 = xor i1 %30, true
  br i1 %31, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code
  br label %continue__3

else__3:                                          ; preds = %else__2
  %32 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %33 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %32, i64 0)
  %34 = bitcast i8* %33 to %Qubit**
  store %Qubit* %q2, %Qubit** %34
  %35 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %36 = bitcast %Tuple* %35 to { %Array*, %Qubit* }*
  %37 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %36, i64 0, i32 0
  %38 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %36, i64 0, i32 1
  store %Array* %32, %Array** %37
  call void @__quantum__rt__array_reference(%Array* %32)
  store %Qubit* %q3, %Qubit** %38
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %35, %Tuple* null)
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
  call void @__quantum__rt__tuple_reference(%Tuple* %49)
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %53, %Tuple* null)
  %59 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %60 = load %Result*, %Result** @ResultOne
  %61 = call i1 @__quantum__rt__result_equal(%Result* %59, %Result* %60)
  %62 = xor i1 %61, true
  br i1 %62, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code
  br label %continue__5

else__5:                                          ; preds = %else__4
  %63 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %63)
  %64 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %65 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %64, i64 0)
  %66 = bitcast i8* %65 to %Qubit**
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %64, i64 1)
  %68 = bitcast i8* %67 to %Qubit**
  store %Qubit* %q1, %Qubit** %66
  store %Qubit* %q2, %Qubit** %68
  %69 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %70 = bitcast %Tuple* %69 to { %Array*, %Qubit* }*
  %71 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %70, i64 0, i32 0
  %72 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %70, i64 0, i32 1
  store %Array* %64, %Array** %71
  call void @__quantum__rt__array_reference(%Array* %64)
  store %Qubit* %q3, %Qubit** %72
  call void @__quantum__rt__callable_invoke(%Callable* %63, %Tuple* %69, %Tuple* null)
  %73 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %74 = load %Result*, %Result** @ResultZero
  %75 = call i1 @__quantum__rt__result_equal(%Result* %73, %Result* %74)
  %76 = xor i1 %75, true
  br i1 %76, label %then0__6, label %else__6

then0__6:                                         ; preds = %else__5
  store i64 6, i64* %error_code
  br label %continue__6

else__6:                                          ; preds = %else__5
  %q4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %77 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %77)
  %78 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %79 = bitcast %Tuple* %78 to { %Qubit* }*
  %80 = getelementptr { %Qubit* }, { %Qubit* }* %79, i64 0, i32 0
  store %Qubit* %q3, %Qubit** %80
  call void @__quantum__rt__callable_invoke(%Callable* %77, %Tuple* %78, %Tuple* null)
  %81 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %81)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %81)
  %82 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %83 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %82, i64 0)
  %84 = bitcast i8* %83 to %Qubit**
  store %Qubit* %q1, %Qubit** %84
  %85 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %86 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %85, i64 0)
  %87 = bitcast i8* %86 to %Qubit**
  store %Qubit* %q2, %Qubit** %87
  %88 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %89 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %88, i64 0)
  %90 = bitcast i8* %89 to %Qubit**
  store %Qubit* %q3, %Qubit** %90
  %91 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %92 = bitcast %Tuple* %91 to { %Array*, %Qubit* }*
  %93 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %92, i64 0, i32 0
  %94 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %92, i64 0, i32 1
  store %Array* %88, %Array** %93
  call void @__quantum__rt__array_reference(%Array* %88)
  store %Qubit* %q4, %Qubit** %94
  %95 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %96 = bitcast %Tuple* %95 to { %Array*, { %Array*, %Qubit* }* }*
  %97 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %96, i64 0, i32 0
  %98 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %96, i64 0, i32 1
  store %Array* %85, %Array** %97
  call void @__quantum__rt__array_reference(%Array* %85)
  store { %Array*, %Qubit* }* %92, { %Array*, %Qubit* }** %98
  %99 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %92, i64 0, i32 0
  %100 = load %Array*, %Array** %99
  call void @__quantum__rt__array_reference(%Array* %100)
  call void @__quantum__rt__tuple_reference(%Tuple* %91)
  %101 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %102 = bitcast %Tuple* %101 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %103 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %102, i64 0, i32 0
  %104 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %102, i64 0, i32 1
  store %Array* %82, %Array** %103
  call void @__quantum__rt__array_reference(%Array* %82)
  store { %Array*, { %Array*, %Qubit* }* }* %96, { %Array*, { %Array*, %Qubit* }* }** %104
  %105 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %96, i64 0, i32 0
  %106 = load %Array*, %Array** %105
  call void @__quantum__rt__array_reference(%Array* %106)
  %107 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %96, i64 0, i32 1
  %108 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %107
  %109 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %108, i64 0, i32 0
  %110 = load %Array*, %Array** %109
  call void @__quantum__rt__array_reference(%Array* %110)
  %111 = bitcast { %Array*, %Qubit* }* %108 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %111)
  call void @__quantum__rt__tuple_reference(%Tuple* %95)
  call void @__quantum__rt__callable_invoke(%Callable* %81, %Tuple* %101, %Tuple* null)
  %112 = call %Result* @__quantum__qis__mz(%Qubit* %q4)
  %113 = load %Result*, %Result** @ResultOne
  %114 = call i1 @__quantum__rt__result_equal(%Result* %112, %Result* %113)
  %115 = xor i1 %114, true
  br i1 %115, label %then0__7, label %continue__7

then0__7:                                         ; preds = %else__6
  store i64 7, i64* %error_code
  br label %continue__7

continue__7:                                      ; preds = %then0__7, %else__6
  call void @__quantum__rt__qubit_release(%Qubit* %q4)
  call void @__quantum__rt__callable_unreference(%Callable* %77)
  call void @__quantum__rt__tuple_unreference(%Tuple* %78)
  call void @__quantum__rt__callable_unreference(%Callable* %81)
  call void @__quantum__rt__array_unreference(%Array* %82)
  call void @__quantum__rt__array_unreference(%Array* %85)
  call void @__quantum__rt__array_unreference(%Array* %88)
  %116 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %92, i64 0, i32 0
  %117 = load %Array*, %Array** %116
  call void @__quantum__rt__array_unreference(%Array* %117)
  call void @__quantum__rt__tuple_unreference(%Tuple* %91)
  %118 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %96, i64 0, i32 0
  %119 = load %Array*, %Array** %118
  call void @__quantum__rt__array_unreference(%Array* %119)
  %120 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %96, i64 0, i32 1
  %121 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %120
  %122 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %121, i64 0, i32 0
  %123 = load %Array*, %Array** %122
  call void @__quantum__rt__array_unreference(%Array* %123)
  %124 = bitcast { %Array*, %Qubit* }* %121 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %124)
  call void @__quantum__rt__tuple_unreference(%Tuple* %95)
  %125 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %102, i64 0, i32 0
  %126 = load %Array*, %Array** %125
  call void @__quantum__rt__array_unreference(%Array* %126)
  %127 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %102, i64 0, i32 1
  %128 = load { %Array*, { %Array*, %Qubit* }* }*, { %Array*, { %Array*, %Qubit* }* }** %127
  %129 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %128, i64 0, i32 0
  %130 = load %Array*, %Array** %129
  call void @__quantum__rt__array_unreference(%Array* %130)
  %131 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %128, i64 0, i32 1
  %132 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %131
  %133 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %132, i64 0, i32 0
  %134 = load %Array*, %Array** %133
  call void @__quantum__rt__array_unreference(%Array* %134)
  %135 = bitcast { %Array*, %Qubit* }* %132 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %135)
  %136 = bitcast { %Array*, { %Array*, %Qubit* }* }* %128 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %136)
  call void @__quantum__rt__tuple_unreference(%Tuple* %101)
  call void @__quantum__rt__result_unreference(%Result* %112)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  call void @__quantum__rt__callable_unreference(%Callable* %63)
  call void @__quantum__rt__array_unreference(%Array* %64)
  %137 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %70, i64 0, i32 0
  %138 = load %Array*, %Array** %137
  call void @__quantum__rt__array_unreference(%Array* %138)
  call void @__quantum__rt__tuple_unreference(%Tuple* %69)
  call void @__quantum__rt__result_unreference(%Result* %73)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_unreference(%Array* %43)
  call void @__quantum__rt__array_unreference(%Array* %46)
  %139 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %50, i64 0, i32 0
  %140 = load %Array*, %Array** %139
  call void @__quantum__rt__array_unreference(%Array* %140)
  call void @__quantum__rt__tuple_unreference(%Tuple* %49)
  %141 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 0
  %142 = load %Array*, %Array** %141
  call void @__quantum__rt__array_unreference(%Array* %142)
  %143 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 1
  %144 = load { %Array*, %Qubit* }*, { %Array*, %Qubit* }** %143
  %145 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %144, i64 0, i32 0
  %146 = load %Array*, %Array** %145
  call void @__quantum__rt__array_unreference(%Array* %146)
  %147 = bitcast { %Array*, %Qubit* }* %144 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %147)
  call void @__quantum__rt__tuple_unreference(%Tuple* %53)
  call void @__quantum__rt__result_unreference(%Result* %59)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_unreference(%Array* %32)
  %148 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %36, i64 0, i32 0
  %149 = load %Array*, %Array** %148
  call void @__quantum__rt__array_unreference(%Array* %149)
  call void @__quantum__rt__tuple_unreference(%Tuple* %35)
  call void @__quantum__rt__result_unreference(%Result* %39)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_unreference(%Array* %21)
  %150 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %25, i64 0, i32 0
  %151 = load %Array*, %Array** %150
  call void @__quantum__rt__array_unreference(%Array* %151)
  call void @__quantum__rt__tuple_unreference(%Tuple* %24)
  call void @__quantum__rt__result_unreference(%Result* %28)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  call void @__quantum__rt__tuple_unreference(%Tuple* %14)
  call void @__quantum__rt__result_unreference(%Result* %17)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  call void @__quantum__rt__tuple_unreference(%Tuple* %7)
  call void @__quantum__rt__result_unreference(%Result* %10)
  %152 = load i64, i64* %error_code
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %153 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %2, i64 0, i32 0
  %154 = load %Callable*, %Callable** %153
  call void @__quantum__rt__callable_unreference(%Callable* %154)
  call void @__quantum__rt__tuple_unreference(%Tuple* %1)
  call void @__quantum__rt__callable_unreference(%Callable* %qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_ctl_qop)
  ret i64 %152
}
