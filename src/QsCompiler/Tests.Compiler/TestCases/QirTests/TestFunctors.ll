define i64 @Microsoft__Quantum__Testing__QIR__TestControlled__body() #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, i64 }*
  %2 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %1, i64 0, i32 0
  %3 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %1, i64 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Qop, %Tuple* null)
  store %Callable* %4, %Callable** %2
  store i64 1, i64* %3
  %qop = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
  %adj_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { i64 }*
  %7 = getelementptr { i64 }, { i64 }* %6, i64 0, i32 0
  store i64 1, i64* %7
  call void @__quantum__rt__callable_memory_management(%Callable* %adj_qop, %Tuple* %5, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_qop)
  %ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { i64 }*
  %10 = getelementptr { i64 }, { i64 }* %9, i64 0, i32 0
  store i64 1, i64* %10
  call void @__quantum__rt__callable_memory_management(%Callable* %ctl_qop, %Tuple* %8, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i64 -1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_qop)
  %adj_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %12 = bitcast %Tuple* %11 to { i64 }*
  %13 = getelementptr { i64 }, { i64 }* %12, i64 0, i32 0
  store i64 1, i64* %13
  call void @__quantum__rt__callable_memory_management(%Callable* %adj_ctl_qop, %Tuple* %11, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_ctl_qop)
  %ctl_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_qop, i1 true)
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { i64 }*
  %16 = getelementptr { i64 }, { i64 }* %15, i64 0, i32 0
  store i64 1, i64* %16
  call void @__quantum__rt__callable_memory_management(%Callable* %ctl_ctl_qop, %Tuple* %14, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i64 -1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_ctl_qop)
  %error_code = alloca i64
  store i64 0, i64* %error_code
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %17 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %18 = bitcast %Tuple* %17 to { %Qubit* }*
  %19 = getelementptr { %Qubit* }, { %Qubit* }* %18, i64 0, i32 0
  store %Qubit* %q1, %Qubit** %19
  call void @__quantum__rt__callable_invoke(%Callable* %qop, %Tuple* %17, %Tuple* null)
  %20 = call %Result* @__quantum__qis__mz(%Qubit* %q1)
  %21 = load %Result*, %Result** @ResultOne
  %22 = call i1 @__quantum__rt__result_equal(%Result* %20, %Result* %21)
  %23 = xor i1 %22, true
  br i1 %23, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  store i64 1, i64* %error_code
  br label %continue__1

else__1:                                          ; preds = %entry
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %25 = bitcast %Tuple* %24 to { %Qubit* }*
  %26 = getelementptr { %Qubit* }, { %Qubit* }* %25, i64 0, i32 0
  store %Qubit* %q2, %Qubit** %26
  call void @__quantum__rt__callable_invoke(%Callable* %adj_qop, %Tuple* %24, %Tuple* null)
  %27 = call %Result* @__quantum__qis__mz(%Qubit* %q2)
  %28 = load %Result*, %Result** @ResultOne
  %29 = call i1 @__quantum__rt__result_equal(%Result* %27, %Result* %28)
  %30 = xor i1 %29, true
  br i1 %30, label %then0__2, label %else__2

then0__2:                                         ; preds = %else__1
  store i64 2, i64* %error_code
  br label %continue__2

else__2:                                          ; preds = %else__1
  %31 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %32 = bitcast %Tuple* %31 to { %Array*, %Qubit* }*
  %33 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %32, i64 0, i32 0
  %34 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %32, i64 0, i32 1
  %35 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %36 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %35, i64 0)
  %37 = bitcast i8* %36 to %Qubit**
  store %Qubit* %q1, %Qubit** %37
  store %Array* %35, %Array** %33
  store %Qubit* %q3, %Qubit** %34
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %31, %Tuple* null)
  %38 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %39 = load %Result*, %Result** @ResultOne
  %40 = call i1 @__quantum__rt__result_equal(%Result* %38, %Result* %39)
  %41 = xor i1 %40, true
  br i1 %41, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code
  br label %continue__3

else__3:                                          ; preds = %else__2
  %42 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %43 = bitcast %Tuple* %42 to { %Array*, %Qubit* }*
  %44 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %43, i64 0, i32 0
  %45 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %43, i64 0, i32 1
  %46 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %47 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %46, i64 0)
  %48 = bitcast i8* %47 to %Qubit**
  store %Qubit* %q2, %Qubit** %48
  store %Array* %46, %Array** %44
  store %Qubit* %q3, %Qubit** %45
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %42, %Tuple* null)
  %49 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %50 = load %Result*, %Result** @ResultZero
  %51 = call i1 @__quantum__rt__result_equal(%Result* %49, %Result* %50)
  %52 = xor i1 %51, true
  br i1 %52, label %then0__4, label %else__4

then0__4:                                         ; preds = %else__3
  store i64 4, i64* %error_code
  br label %continue__4

else__4:                                          ; preds = %else__3
  %53 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %54 = bitcast %Tuple* %53 to { %Array*, { %Array*, %Qubit* }* }*
  %55 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 0
  %56 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %54, i64 0, i32 1
  %57 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %58 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %57, i64 0)
  %59 = bitcast i8* %58 to %Qubit**
  store %Qubit* %q1, %Qubit** %59
  %60 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %61 = bitcast %Tuple* %60 to { %Array*, %Qubit* }*
  %62 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %61, i64 0, i32 0
  %63 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %61, i64 0, i32 1
  %64 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %65 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %64, i64 0)
  %66 = bitcast i8* %65 to %Qubit**
  store %Qubit* %q2, %Qubit** %66
  store %Array* %64, %Array** %62
  store %Qubit* %q3, %Qubit** %63
  store %Array* %57, %Array** %55
  store { %Array*, %Qubit* }* %61, { %Array*, %Qubit* }** %56
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %53, %Tuple* null)
  %67 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %68 = load %Result*, %Result** @ResultOne
  %69 = call i1 @__quantum__rt__result_equal(%Result* %67, %Result* %68)
  %70 = xor i1 %69, true
  br i1 %70, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code
  br label %continue__5

else__5:                                          ; preds = %else__4
  %71 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  %72 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %73 = bitcast %Tuple* %72 to { i64 }*
  %74 = getelementptr { i64 }, { i64 }* %73, i64 0, i32 0
  store i64 1, i64* %74
  call void @__quantum__rt__callable_memory_management(%Callable* %71, %Tuple* %72, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %72, i64 -1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %71)
  %75 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %76 = bitcast %Tuple* %75 to { %Array*, %Qubit* }*
  %77 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %76, i64 0, i32 0
  %78 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %76, i64 0, i32 1
  %79 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %80 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %79, i64 0)
  %81 = bitcast i8* %80 to %Qubit**
  %82 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %79, i64 1)
  %83 = bitcast i8* %82 to %Qubit**
  store %Qubit* %q1, %Qubit** %81
  store %Qubit* %q2, %Qubit** %83
  store %Array* %79, %Array** %77
  store %Qubit* %q3, %Qubit** %78
  call void @__quantum__rt__callable_invoke(%Callable* %71, %Tuple* %75, %Tuple* null)
  %84 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %85 = load %Result*, %Result** @ResultZero
  %86 = call i1 @__quantum__rt__result_equal(%Result* %84, %Result* %85)
  %87 = xor i1 %86, true
  br i1 %87, label %then0__6, label %else__6

then0__6:                                         ; preds = %else__5
  store i64 6, i64* %error_code
  br label %continue__6

else__6:                                          ; preds = %else__5
  %q4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %88 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 true)
  %89 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %90 = bitcast %Tuple* %89 to { i64 }*
  %91 = getelementptr { i64 }, { i64 }* %90, i64 0, i32 0
  store i64 1, i64* %91
  call void @__quantum__rt__callable_memory_management(%Callable* %88, %Tuple* %89, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %89, i64 -1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %88)
  %92 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %93 = bitcast %Tuple* %92 to { %Qubit* }*
  %94 = getelementptr { %Qubit* }, { %Qubit* }* %93, i64 0, i32 0
  store %Qubit* %q3, %Qubit** %94
  call void @__quantum__rt__callable_invoke(%Callable* %88, %Tuple* %92, %Tuple* null)
  %95 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop, i1 true)
  %96 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %97 = bitcast %Tuple* %96 to { i64 }*
  %98 = getelementptr { i64 }, { i64 }* %97, i64 0, i32 0
  store i64 1, i64* %98
  call void @__quantum__rt__callable_memory_management(%Callable* %95, %Tuple* %96, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %96, i64 -1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %95)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %95)
  %99 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %100 = bitcast %Tuple* %99 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %101 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %100, i64 0, i32 0
  %102 = getelementptr { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %100, i64 0, i32 1
  %103 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %104 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %103, i64 0)
  %105 = bitcast i8* %104 to %Qubit**
  store %Qubit* %q1, %Qubit** %105
  %106 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %107 = bitcast %Tuple* %106 to { %Array*, { %Array*, %Qubit* }* }*
  %108 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %107, i64 0, i32 0
  %109 = getelementptr { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %107, i64 0, i32 1
  %110 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %111 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %110, i64 0)
  %112 = bitcast i8* %111 to %Qubit**
  store %Qubit* %q2, %Qubit** %112
  %113 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %114 = bitcast %Tuple* %113 to { %Array*, %Qubit* }*
  %115 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %114, i64 0, i32 0
  %116 = getelementptr { %Array*, %Qubit* }, { %Array*, %Qubit* }* %114, i64 0, i32 1
  %117 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %118 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %117, i64 0)
  %119 = bitcast i8* %118 to %Qubit**
  store %Qubit* %q3, %Qubit** %119
  store %Array* %117, %Array** %115
  store %Qubit* %q4, %Qubit** %116
  store %Array* %110, %Array** %108
  store { %Array*, %Qubit* }* %114, { %Array*, %Qubit* }** %109
  store %Array* %103, %Array** %101
  store { %Array*, { %Array*, %Qubit* }* }* %107, { %Array*, { %Array*, %Qubit* }* }** %102
  call void @__quantum__rt__callable_invoke(%Callable* %95, %Tuple* %99, %Tuple* null)
  %120 = call %Result* @__quantum__qis__mz(%Qubit* %q4)
  %121 = load %Result*, %Result** @ResultOne
  %122 = call i1 @__quantum__rt__result_equal(%Result* %120, %Result* %121)
  %123 = xor i1 %122, true
  br i1 %123, label %then0__7, label %continue__7

then0__7:                                         ; preds = %else__6
  store i64 7, i64* %error_code
  br label %continue__7

continue__7:                                      ; preds = %then0__7, %else__6
  call void @__quantum__rt__qubit_release(%Qubit* %q4)
  %124 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %125 = bitcast %Tuple* %124 to { i64 }*
  %126 = getelementptr { i64 }, { i64 }* %125, i64 0, i32 0
  store i64 -1, i64* %126
  call void @__quantum__rt__callable_memory_management(%Callable* %88, %Tuple* %124, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %88, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %92, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %95, %Tuple* %124, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %95, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %103, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %110, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %117, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %113, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %106, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %99, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %120, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %124, i64 -1)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  %127 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %128 = bitcast %Tuple* %127 to { i64 }*
  %129 = getelementptr { i64 }, { i64 }* %128, i64 0, i32 0
  store i64 -1, i64* %129
  call void @__quantum__rt__callable_memory_management(%Callable* %71, %Tuple* %127, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %71, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %79, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %75, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %84, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %127, i64 -1)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_update_reference_count(%Array* %57, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %64, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %60, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %53, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %67, i64 -1)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_update_reference_count(%Array* %46, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %42, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %49, i64 -1)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %31, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %38, i64 -1)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %27, i64 -1)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %20, i64 -1)
  %130 = load i64, i64* %error_code
  %131 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %132 = bitcast %Tuple* %131 to { i64 }*
  %133 = getelementptr { i64 }, { i64 }* %132, i64 0, i32 0
  store i64 -1, i64* %133
  call void @__quantum__rt__callable_memory_management(%Callable* %qop, %Tuple* %131, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %adj_qop, %Tuple* %131, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %adj_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %ctl_qop, %Tuple* %131, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %adj_ctl_qop, %Tuple* %131, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %adj_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %ctl_ctl_qop, %Tuple* %131, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %ctl_ctl_qop, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %131, i64 -1)
  ret i64 %130
}
