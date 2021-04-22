define i64 @Microsoft__Quantum__Testing__QIR__TestControlled__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Qop, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { %Callable*, i64 }*
  %3 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %2, i32 0, i32 1
  store %Callable* %0, %Callable** %3, align 8
  store i64 1, i64* %4, align 4
  %qop = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1, %Tuple* %1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %qop, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %qop, i32 1)
  %adj_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %adj_qop, i32 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_qop)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %adj_qop, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_qop, i32 1)
  %ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %ctl_qop, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_qop)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %ctl_qop, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_qop, i32 1)
  %adj_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %adj_ctl_qop, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %adj_ctl_qop)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %adj_ctl_qop, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_ctl_qop, i32 1)
  %ctl_ctl_qop = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %ctl_ctl_qop, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %ctl_ctl_qop)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %ctl_ctl_qop, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_ctl_qop, i32 1)
  %error_code = alloca i64, align 8
  store i64 0, i64* %error_code, align 4
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { %Qubit* }*
  %7 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %6, i32 0, i32 0
  store %Qubit* %q1, %Qubit** %7, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %qop, %Tuple* %5, %Tuple* null)
  %8 = call %Result* @__quantum__qis__mz(%Qubit* %q1)
  %9 = call %Result* @__quantum__rt__result_get_one()
  %10 = call i1 @__quantum__rt__result_equal(%Result* %8, %Result* %9)
  %11 = xor i1 %10, true
  br i1 %11, label %then0__1, label %else__1

then0__1:                                         ; preds = %entry
  store i64 1, i64* %error_code, align 4
  br label %continue__1

else__1:                                          ; preds = %entry
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %13 = bitcast %Tuple* %12 to { %Qubit* }*
  %14 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %13, i32 0, i32 0
  store %Qubit* %q2, %Qubit** %14, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %adj_qop, %Tuple* %12, %Tuple* null)
  %15 = call %Result* @__quantum__qis__mz(%Qubit* %q2)
  %16 = call %Result* @__quantum__rt__result_get_one()
  %17 = call i1 @__quantum__rt__result_equal(%Result* %15, %Result* %16)
  %18 = xor i1 %17, true
  br i1 %18, label %then0__2, label %else__2

then0__2:                                         ; preds = %else__1
  store i64 2, i64* %error_code, align 4
  br label %continue__2

else__2:                                          ; preds = %else__1
  %19 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %20 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 0)
  %21 = bitcast i8* %20 to %Qubit**
  store %Qubit* %q1, %Qubit** %21, align 8
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %23 = bitcast %Tuple* %22 to { %Array*, %Qubit* }*
  %24 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %23, i32 0, i32 0
  %25 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %23, i32 0, i32 1
  store %Array* %19, %Array** %24, align 8
  store %Qubit* %q3, %Qubit** %25, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_qop, %Tuple* %22, %Tuple* null)
  %26 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %27 = call %Result* @__quantum__rt__result_get_one()
  %28 = call i1 @__quantum__rt__result_equal(%Result* %26, %Result* %27)
  %29 = xor i1 %28, true
  br i1 %29, label %then0__3, label %else__3

then0__3:                                         ; preds = %else__2
  store i64 3, i64* %error_code, align 4
  br label %continue__3

else__3:                                          ; preds = %else__2
  %30 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %30, i64 0)
  %32 = bitcast i8* %31 to %Qubit**
  store %Qubit* %q2, %Qubit** %32, align 8
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %34 = bitcast %Tuple* %33 to { %Array*, %Qubit* }*
  %35 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %34, i32 0, i32 0
  %36 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %34, i32 0, i32 1
  store %Array* %30, %Array** %35, align 8
  store %Qubit* %q3, %Qubit** %36, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %adj_ctl_qop, %Tuple* %33, %Tuple* null)
  %37 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %38 = call %Result* @__quantum__rt__result_get_zero()
  %39 = call i1 @__quantum__rt__result_equal(%Result* %37, %Result* %38)
  %40 = xor i1 %39, true
  br i1 %40, label %then0__4, label %else__4

then0__4:                                         ; preds = %else__3
  store i64 4, i64* %error_code, align 4
  br label %continue__4

else__4:                                          ; preds = %else__3
  %41 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %42 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %41, i64 0)
  %43 = bitcast i8* %42 to %Qubit**
  store %Qubit* %q1, %Qubit** %43, align 8
  %44 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %45 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %44, i64 0)
  %46 = bitcast i8* %45 to %Qubit**
  store %Qubit* %q2, %Qubit** %46, align 8
  %47 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %48 = bitcast %Tuple* %47 to { %Array*, %Qubit* }*
  %49 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %48, i32 0, i32 0
  %50 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %48, i32 0, i32 1
  store %Array* %44, %Array** %49, align 8
  store %Qubit* %q3, %Qubit** %50, align 8
  %51 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %52 = bitcast %Tuple* %51 to { %Array*, { %Array*, %Qubit* }* }*
  %53 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %52, i32 0, i32 0
  %54 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %52, i32 0, i32 1
  store %Array* %41, %Array** %53, align 8
  store { %Array*, %Qubit* }* %48, { %Array*, %Qubit* }** %54, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %ctl_ctl_qop, %Tuple* %51, %Tuple* null)
  %55 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %56 = call %Result* @__quantum__rt__result_get_one()
  %57 = call i1 @__quantum__rt__result_equal(%Result* %55, %Result* %56)
  %58 = xor i1 %57, true
  br i1 %58, label %then0__5, label %else__5

then0__5:                                         ; preds = %else__4
  store i64 5, i64* %error_code, align 4
  br label %continue__5

else__5:                                          ; preds = %else__4
  %59 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %59, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %59)
  %60 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %61 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %60, i64 0)
  %62 = bitcast i8* %61 to %Qubit**
  %63 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %60, i64 1)
  %64 = bitcast i8* %63 to %Qubit**
  store %Qubit* %q1, %Qubit** %62, align 8
  store %Qubit* %q2, %Qubit** %64, align 8
  %65 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %66 = bitcast %Tuple* %65 to { %Array*, %Qubit* }*
  %67 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %66, i32 0, i32 0
  %68 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %66, i32 0, i32 1
  store %Array* %60, %Array** %67, align 8
  store %Qubit* %q3, %Qubit** %68, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %59, %Tuple* %65, %Tuple* null)
  %69 = call %Result* @__quantum__qis__mz(%Qubit* %q3)
  %70 = call %Result* @__quantum__rt__result_get_zero()
  %71 = call i1 @__quantum__rt__result_equal(%Result* %69, %Result* %70)
  %72 = xor i1 %71, true
  br i1 %72, label %then0__6, label %else__6

then0__6:                                         ; preds = %else__5
  store i64 6, i64* %error_code, align 4
  br label %continue__6

else__6:                                          ; preds = %else__5
  %q4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %73 = call %Callable* @__quantum__rt__callable_copy(%Callable* %qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %73, i32 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %73)
  %74 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %75 = bitcast %Tuple* %74 to { %Qubit* }*
  %76 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %75, i32 0, i32 0
  store %Qubit* %q3, %Qubit** %76, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %73, %Tuple* %74, %Tuple* null)
  %77 = call %Callable* @__quantum__rt__callable_copy(%Callable* %ctl_ctl_qop, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %77, i32 1)
  call void @__quantum__rt__callable_make_controlled(%Callable* %77)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %77)
  %78 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %79 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %78, i64 0)
  %80 = bitcast i8* %79 to %Qubit**
  store %Qubit* %q1, %Qubit** %80, align 8
  %81 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %82 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %81, i64 0)
  %83 = bitcast i8* %82 to %Qubit**
  store %Qubit* %q2, %Qubit** %83, align 8
  %84 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %85 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %84, i64 0)
  %86 = bitcast i8* %85 to %Qubit**
  store %Qubit* %q3, %Qubit** %86, align 8
  %87 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %88 = bitcast %Tuple* %87 to { %Array*, %Qubit* }*
  %89 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %88, i32 0, i32 0
  %90 = getelementptr inbounds { %Array*, %Qubit* }, { %Array*, %Qubit* }* %88, i32 0, i32 1
  store %Array* %84, %Array** %89, align 8
  store %Qubit* %q4, %Qubit** %90, align 8
  %91 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %92 = bitcast %Tuple* %91 to { %Array*, { %Array*, %Qubit* }* }*
  %93 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %92, i32 0, i32 0
  %94 = getelementptr inbounds { %Array*, { %Array*, %Qubit* }* }, { %Array*, { %Array*, %Qubit* }* }* %92, i32 0, i32 1
  store %Array* %81, %Array** %93, align 8
  store { %Array*, %Qubit* }* %88, { %Array*, %Qubit* }** %94, align 8
  %95 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %96 = bitcast %Tuple* %95 to { %Array*, { %Array*, { %Array*, %Qubit* }* }* }*
  %97 = getelementptr inbounds { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %96, i32 0, i32 0
  %98 = getelementptr inbounds { %Array*, { %Array*, { %Array*, %Qubit* }* }* }, { %Array*, { %Array*, { %Array*, %Qubit* }* }* }* %96, i32 0, i32 1
  store %Array* %78, %Array** %97, align 8
  store { %Array*, { %Array*, %Qubit* }* }* %92, { %Array*, { %Array*, %Qubit* }* }** %98, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %77, %Tuple* %95, %Tuple* null)
  %99 = call %Result* @__quantum__qis__mz(%Qubit* %q4)
  %100 = call %Result* @__quantum__rt__result_get_one()
  %101 = call i1 @__quantum__rt__result_equal(%Result* %99, %Result* %100)
  %102 = xor i1 %101, true
  br i1 %102, label %then0__7, label %continue__7

then0__7:                                         ; preds = %else__6
  store i64 7, i64* %error_code, align 4
  br label %continue__7

continue__7:                                      ; preds = %then0__7, %else__6
  call void @__quantum__rt__capture_update_reference_count(%Callable* %73, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %73, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %74, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %77, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %77, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %78, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %81, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %84, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %87, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %91, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %95, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %99, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q4)
  br label %continue__6

continue__6:                                      ; preds = %continue__7, %then0__6
  call void @__quantum__rt__capture_update_reference_count(%Callable* %59, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %59, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %60, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %65, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %69, i32 -1)
  br label %continue__5

continue__5:                                      ; preds = %continue__6, %then0__5
  call void @__quantum__rt__array_update_reference_count(%Array* %41, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %44, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %47, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %51, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %55, i32 -1)
  br label %continue__4

continue__4:                                      ; preds = %continue__5, %then0__4
  call void @__quantum__rt__array_update_reference_count(%Array* %30, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %33, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %37, i32 -1)
  br label %continue__3

continue__3:                                      ; preds = %continue__4, %then0__3
  call void @__quantum__rt__array_update_reference_count(%Array* %19, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %26, i32 -1)
  br label %continue__2

continue__2:                                      ; preds = %continue__3, %then0__2
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %15, i32 -1)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %then0__1
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %8, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  call void @__quantum__rt__qubit_release(%Qubit* %q3)
  %103 = load i64, i64* %error_code, align 4
  call void @__quantum__rt__capture_update_alias_count(%Callable* %qop, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %qop, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %adj_qop, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_qop, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_qop, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %adj_ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %adj_ctl_qop, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %ctl_ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %ctl_ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %qop, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %adj_qop, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %adj_qop, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %ctl_qop, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %adj_ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %adj_ctl_qop, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %ctl_ctl_qop, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %ctl_ctl_qop, i32 -1)
  ret i64 %103
}
