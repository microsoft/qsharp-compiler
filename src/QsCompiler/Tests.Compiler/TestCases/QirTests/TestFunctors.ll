define i64 @Microsoft__Quantum__Testing__QIR__TestControlled__body() #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, i64 }*
  %2 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %1, i32 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Qop, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %4, %Callable** %2
  store i64 1, i64* %3
  %qop = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, [2 x void (%Tuple*, i64)*]* @MemoryManagement__1, %Tuple* %0)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %qop, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %qop, i64 1)
  %adj_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %adj_qop, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_qop)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %adj_qop, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_qop, i64 1)
  %ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %ctl_qop, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_qop)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %ctl_qop, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_qop, i64 1)
  %adj_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %adj_ctl_qop, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %adj_ctl_qop, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_ctl_qop, i64 1)
  %ctl_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %ctl_ctl_qop, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_ctl_qop)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %ctl_ctl_qop, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_ctl_qop, i64 1)
  %error_code = alloca i64
  store i64 0, i64* %error_code
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { %Qubit* }*
  %7 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %6, i32 0, i32 0
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
  %14 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %13, i32 0, i32 0
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
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %20 = bitcast %Tuple* %19 to { %Array*, %Qubit* }*
  %21 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %20, i32 0, i32 0
  %22 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %20, i32 0, i32 1
  %23 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %23, i64 0)
  %25 = bitcast i8* %24 to %Qubit**
  store %Qubit* %q1, %Qubit** %25
  store %Array* %23, %Array** %21
  store %Qubit* %q3, %Qubit** %22
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %19, %Tuple* null)
  %26 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %27 = load %Result*, %Result** @ResultOne
  %28 = call i1 @__quantum__rt__result_equal(%Result* %26, %Result* %27)
  %29 = xor i1 %28, true
  br i1 %29, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code
  br label %continue__3

else__3:                                          ; preds = %else__2
  %30 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %31 = bitcast %Tuple* %30 to { %Array*, %Qubit* }*
  %32 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %31, i32 0, i32 0
  %33 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %31, i32 0, i32 1
  %34 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %35 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %34, i64 0)
  %36 = bitcast i8* %35 to %Qubit**
  store %Qubit* %q2, %Qubit** %36
  store %Array* %34, %Array** %32
  store %Qubit* %q3, %Qubit** %33
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %30, %Tuple* null)
  %37 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %38 = load %Result*, %Result** @ResultZero
  %39 = call i1 @__quantum__rt__result_equal(%Result* %37, %Result* %38)
  %40 = xor i1 %39, true
  br i1 %40, label %then0__4, label %else__4

then0__4:                                         ; preds = %else__3
  store i64 4, i64* %error_code
  br label %continue__4

else__4:                                          ; preds = %else__3
  %41 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %42 = bitcast %Tuple* %41 to { %Array*, { %Array*, %Qubit* }* }*
  %43 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %42, i32 0, i32 0
  %44 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %42, i32 0, i32 1
  %45 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %46 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %45, i64 0)
  %47 = bitcast i8* %46 to %Qubit**
  store %Qubit* %q1, %Qubit** %47
  %48 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %49 = bitcast %Tuple* %48 to { %Array*, %Qubit* }*
  %50 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %49, i32 0, i32 0
  %51 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %49, i32 0, i32 1
  %52 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %53 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %52, i64 0)
  %54 = bitcast i8* %53 to %Qubit**
  store %Qubit* %q2, %Qubit** %54
  store %Array* %52, %Array** %50
  store %Qubit* %q3, %Qubit** %51
  store %Array* %45, %Array** %43
  store { %Array*, %Qubit* }* %49, { %Array*, %Qubit* }** %44
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %41, %Tuple* null)
  %55 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %56 = load %Result*, %Result** @ResultOne
  %57 = call i1 @__quantum__rt__result_equal(%Result* %55, %Result* %56)
  %58 = xor i1 %57, true
  br i1 %58, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code
  br label %continue__5

else__5:                                          ; preds = %else__4
  %59 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %59, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %59)
  %60 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %61 = bitcast %Tuple* %60 to { %Array*, %Qubit* }*
  %62 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %61, i32 0, i32 0
  %63 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %61, i32 0, i32 1
  %64 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %65 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %64, i64 0)
  %66 = bitcast i8* %65 to %Qubit**
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %64, i64 1)
  %68 = bitcast i8* %67 to %Qubit**
  store %Qubit* %q1, %Qubit** %66
  store %Qubit* %q2, %Qubit** %68
  store %Array* %64, %Array** %62
  store %Qubit* %q3, %Qubit** %63
  call void @__quantum__rt__callable_invoke(%Callable* %59, %Tuple* %60, %Tuple* null)
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
  %73 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %73, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %73)
  %74 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %75 = bitcast %Tuple* %74 to { %Qubit* }*
  %76 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %75, i32 0, i32 0
  store %Qubit* %q3, %Qubit** %76
  call void @__quantum__rt__callable_invoke(%Callable* %73, %Tuple* %74, %Tuple* null)
  %77 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %77, i64 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %77)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %77)
  %78 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %79 = bitcast %Tuple* %78 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %80 = getelementptr inbounds { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %79, i32 0, i32 0
  %81 = getelementptr inbounds { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %79, i32 0, i32 1
  %82 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %83 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %82, i64 0)
  %84 = bitcast i8* %83 to %Qubit**
  store %Qubit* %q1, %Qubit** %84
  %85 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %86 = bitcast %Tuple* %85 to { %Array*, { %Array*, %Qubit* }* }*
  %87 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %86, i32 0, i32 0
  %88 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %86, i32 0, i32 1
  %89 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %90 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %89, i64 0)
  %91 = bitcast i8* %90 to %Qubit**
  store %Qubit* %q2, %Qubit** %91
  %92 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %93 = bitcast %Tuple* %92 to { %Array*, %Qubit* }*
  %94 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %93, i32 0, i32 0
  %95 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %93, i32 0, i32 1
  %96 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %97 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %96, i64 0)
  %98 = bitcast i8* %97 to %Qubit**
  store %Qubit* %q3, %Qubit** %98
  store %Array* %96, %Array** %94
  store %Qubit* %q4, %Qubit** %95
  store %Array* %89, %Array** %87
  store { %Array*, %Qubit* }* %93, { %Array*, %Qubit* }** %88
  store %Array* %82, %Array** %80
  store { %Array*, { %Array*, %Qubit* }* }* %86, { %Array*, { %Array*, %Qubit* }* }** %81
  call void @__quantum__rt__callable_invoke(%Callable* %77, %Tuple* %78, %Tuple* null)
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
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %73, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %73, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %74, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %77, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %77, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %82, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %89, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %96, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %92, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %85, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %78, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %99, i64 -1)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %59, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %59, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %64, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %60, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %69, i64 -1)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_update_reference_count(%Array* %45, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %52, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %48, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %41, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %55, i64 -1)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_update_reference_count(%Array* %34, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %30, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %37, i64 -1)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_update_reference_count(%Array* %23, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %26, i64 -1)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %15, i64 -1)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %8, i64 -1)
  %103 = load i64, i64* %error_code
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %qop, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %adj_qop, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %ctl_qop, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %adj_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %ctl_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %qop, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %adj_qop, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %adj_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %ctl_qop, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %adj_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %adj_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %ctl_ctl_qop, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %ctl_ctl_qop, i64 -1)
  ret i64 %103
}
