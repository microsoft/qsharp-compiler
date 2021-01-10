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
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %22, %Tuple* null)
  %26 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %27 = load %Result*, %Result** @ResultOne
  %28 = call i1 @__quantum__rt__result_equal(%Result* %26, %Result* %27)
  %29 = xor i1 %28, true
  br i1 %29, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code
  br label %continue__3

else__3:                                          ; preds = %else__2
  %30 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %30, i64 0)
  %32 = bitcast i8* %31 to %Qubit**
  store %Qubit* %q2, %Qubit** %32
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %34 = bitcast %Tuple* %33 to { %Array*, %Qubit* }*
  %35 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %34, i64 0, i32 0
  %36 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %34, i64 0, i32 1
  store %Array* %30, %Array** %35
  call void @__quantum__rt__array_reference(%Array* %30)
  store %Qubit* %q3, %Qubit** %36
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %33, %Tuple* null)
  %37 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %38 = load %Result*, %Result** @ResultZero
  %39 = call i1 @__quantum__rt__result_equal(%Result* %37, %Result* %38)
  %40 = xor i1 %39, true
  br i1 %40, label %then0__4, label %else__4

then0__4:                                         ; preds = %else__3
  store i64 4, i64* %error_code
  br label %continue__4

else__4:                                          ; preds = %else__3
  %41 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %42 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %41, i64 0)
  %43 = bitcast i8* %42 to %Qubit**
  store %Qubit* %q1, %Qubit** %43
  %44 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %45 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %44, i64 0)
  %46 = bitcast i8* %45 to %Qubit**
  store %Qubit* %q2, %Qubit** %46
  %47 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %48 = bitcast %Tuple* %47 to { %Array*, %Qubit* }*
  %49 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %48, i64 0, i32 0
  %50 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %48, i64 0, i32 1
  store %Array* %44, %Array** %49
  call void @__quantum__rt__array_reference(%Array* %44)
  store %Qubit* %q3, %Qubit** %50
  %51 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %52 = bitcast %Tuple* %51 to { %Array*, { %Array*, %Qubit* }* }*
  %53 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %52, i64 0, i32 0
  %54 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %52, i64 0, i32 1
  store %Array* %41, %Array** %53
  call void @__quantum__rt__array_reference(%Array* %41)
  store { %Array*, %Qubit* }* %48, { %Array*, %Qubit* }** %54
  call void @__quantum__rt__array_reference(%Array* %44)
  call void @__quantum__rt__tuple_reference(%Tuple* %47)
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %51, %Tuple* null)
  %55 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %56 = load %Result*, %Result** @ResultOne
  %57 = call i1 @__quantum__rt__result_equal(%Result* %55, %Result* %56)
  %58 = xor i1 %57, true
  br i1 %58, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code
  br label %continue__5

else__5:                                          ; preds = %else__4
  %59 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %59)
  %60 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %61 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %60, i64 0)
  %62 = bitcast i8* %61 to %Qubit**
  %63 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %60, i64 1)
  %64 = bitcast i8* %63 to %Qubit**
  store %Qubit* %q1, %Qubit** %62
  store %Qubit* %q2, %Qubit** %64
  %65 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %66 = bitcast %Tuple* %65 to { %Array*, %Qubit* }*
  %67 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %66, i64 0, i32 0
  %68 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %66, i64 0, i32 1
  store %Array* %60, %Array** %67
  call void @__quantum__rt__array_reference(%Array* %60)
  store %Qubit* %q3, %Qubit** %68
  call void @__quantum__rt__callable_invoke(%Callable* %59, %Tuple* %65, %Tuple* null)
  %69 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %70 = load %Result*, %Result** @ResultZero
  %71 = call i1 @__quantum__rt__result_equal(%Result* %69, %Result* %70)
  %72 = xor i1 %71, true
  br i1 %72, label %then0__6, label %else__6

then0__6:                                         ; preds = %else__5
  store i64 6, i64* %error_code
  br label %continue__6

else__6:                                          ; preds = %else__5
  %q4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %73 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %73)
  %74 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %75 = bitcast %Tuple* %74 to { %Qubit* }*
  %76 = getelementptr { %Qubit* }, { %Qubit* }* %75, i64 0, i32 0
  store %Qubit* %q3, %Qubit** %76
  call void @__quantum__rt__callable_invoke(%Callable* %73, %Tuple* %74, %Tuple* null)
  %77 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop, i1 true)
  call void @__quantum__rt__callable_make_controlled(%Callable* %77)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %77)
  %78 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %79 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %78, i64 0)
  %80 = bitcast i8* %79 to %Qubit**
  store %Qubit* %q1, %Qubit** %80
  %81 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %82 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %81, i64 0)
  %83 = bitcast i8* %82 to %Qubit**
  store %Qubit* %q2, %Qubit** %83
  %84 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %85 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %84, i64 0)
  %86 = bitcast i8* %85 to %Qubit**
  store %Qubit* %q3, %Qubit** %86
  %87 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %88 = bitcast %Tuple* %87 to { %Array*, %Qubit* }*
  %89 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %88, i64 0, i32 0
  %90 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %88, i64 0, i32 1
  store %Array* %84, %Array** %89
  call void @__quantum__rt__array_reference(%Array* %84)
  store %Qubit* %q4, %Qubit** %90
  %91 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %92 = bitcast %Tuple* %91 to { %Array*, { %Array*, %Qubit* }* }*
  %93 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %92, i64 0, i32 0
  %94 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %92, i64 0, i32 1
  store %Array* %81, %Array** %93
  call void @__quantum__rt__array_reference(%Array* %81)
  store { %Array*, %Qubit* }* %88, { %Array*, %Qubit* }** %94
  call void @__quantum__rt__array_reference(%Array* %84)
  call void @__quantum__rt__tuple_reference(%Tuple* %87)
  %95 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %96 = bitcast %Tuple* %95 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %97 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %96, i64 0, i32 0
  %98 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %96, i64 0, i32 1
  store %Array* %78, %Array** %97
  call void @__quantum__rt__array_reference(%Array* %78)
  store { %Array*, { %Array*, %Qubit* }* }* %92, { %Array*, { %Array*, %Qubit* }* }** %98
  call void @__quantum__rt__array_reference(%Array* %81)
  call void @__quantum__rt__array_reference(%Array* %84)
  call void @__quantum__rt__tuple_reference(%Tuple* %87)
  call void @__quantum__rt__tuple_reference(%Tuple* %91)
  call void @__quantum__rt__callable_invoke(%Callable* %77, %Tuple* %95, %Tuple* null)
  %99 = call %Result* @__quantum__qis__mz(%Qubit* %q4)
  %100 = load %Result*, %Result** @ResultOne
  %101 = call i1 @__quantum__rt__result_equal(%Result* %99, %Result* %100)
  %102 = xor i1 %101, true
  br i1 %102, label %then0__7, label %continue__7

then0__7:                                         ; preds = %else__6
  store i64 7, i64* %error_code
  br label %continue__7

continue__7:                                      ; preds = %then0__7, %else__6
  call void @__quantum__rt__qubit_release(%Qubit* %q4)
  call void @__quantum__rt__callable_unreference(%Callable* %73)
  call void @__quantum__rt__tuple_unreference(%Tuple* %74)
  call void @__quantum__rt__callable_unreference(%Callable* %77)
  call void @__quantum__rt__array_unreference(%Array* %78)
  call void @__quantum__rt__array_unreference(%Array* %81)
  call void @__quantum__rt__array_unreference(%Array* %84)
  call void @__quantum__rt__array_unreference(%Array* %84)
  call void @__quantum__rt__tuple_unreference(%Tuple* %87)
  call void @__quantum__rt__array_unreference(%Array* %81)
  call void @__quantum__rt__array_unreference(%Array* %84)
  call void @__quantum__rt__tuple_unreference(%Tuple* %87)
  call void @__quantum__rt__tuple_unreference(%Tuple* %91)
  call void @__quantum__rt__array_unreference(%Array* %78)
  call void @__quantum__rt__array_unreference(%Array* %81)
  call void @__quantum__rt__array_unreference(%Array* %84)
  call void @__quantum__rt__tuple_unreference(%Tuple* %87)
  call void @__quantum__rt__tuple_unreference(%Tuple* %91)
  call void @__quantum__rt__tuple_unreference(%Tuple* %95)
  call void @__quantum__rt__result_unreference(%Result* %99)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  call void @__quantum__rt__callable_unreference(%Callable* %59)
  call void @__quantum__rt__array_unreference(%Array* %60)
  call void @__quantum__rt__array_unreference(%Array* %60)
  call void @__quantum__rt__tuple_unreference(%Tuple* %65)
  call void @__quantum__rt__result_unreference(%Result* %69)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_unreference(%Array* %41)
  call void @__quantum__rt__array_unreference(%Array* %44)
  call void @__quantum__rt__array_unreference(%Array* %44)
  call void @__quantum__rt__tuple_unreference(%Tuple* %47)
  call void @__quantum__rt__array_unreference(%Array* %41)
  call void @__quantum__rt__array_unreference(%Array* %44)
  call void @__quantum__rt__tuple_unreference(%Tuple* %47)
  call void @__quantum__rt__tuple_unreference(%Tuple* %51)
  call void @__quantum__rt__result_unreference(%Result* %55)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_unreference(%Array* %30)
  call void @__quantum__rt__array_unreference(%Array* %30)
  call void @__quantum__rt__tuple_unreference(%Tuple* %33)
  call void @__quantum__rt__result_unreference(%Result* %37)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_unreference(%Array* %19)
  call void @__quantum__rt__array_unreference(%Array* %19)
  call void @__quantum__rt__tuple_unreference(%Tuple* %22)
  call void @__quantum__rt__result_unreference(%Result* %26)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  call void @__quantum__rt__tuple_unreference(%Tuple* %12)
  call void @__quantum__rt__result_unreference(%Result* %15)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  call void @__quantum__rt__tuple_unreference(%Tuple* %5)
  call void @__quantum__rt__result_unreference(%Result* %8)
  %103 = load i64, i64* %error_code
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  call void @__quantum__rt__callable_unreference(%Callable* %qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_unreference(%Callable* %ctl_ctl_qop)
  ret i64 %103
}
