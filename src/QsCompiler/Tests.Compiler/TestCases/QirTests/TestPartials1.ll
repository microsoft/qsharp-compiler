define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %2 = bitcast %Tuple* %1 to { %Callable*, double }*
  %3 = getelementptr inbounds { %Callable*, double }, { %Callable*, double }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { %Callable*, double }, { %Callable*, double }* %2, i32 0, i32 1
  store %Callable* %0, %Callable** %3, align 8
  store double 2.500000e-01, double* %4, align 8
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1, %Tuple* %1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %rotate, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %rotate, i32 1)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %unrotate, i32 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %unrotate, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %unrotate, i32 1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %59, %exiting__1 ]
  %5 = icmp sle i64 %i, 100
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { %Qubit* }*
  %8 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %7, i32 0, i32 0
  store %Qubit* %qb, %Qubit** %8, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %6, %Tuple* null)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Qubit* }*
  %11 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %10, i32 0, i32 0
  store %Qubit* %qb, %Qubit** %11, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %9, %Tuple* null)
  %12 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %13 = call %Result* @__quantum__rt__result_get_zero()
  %14 = call i1 @__quantum__rt__result_equal(%Result* %12, %Result* %13)
  %15 = xor i1 %14, true
  br i1 %15, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %16 to { i64, double }*
  %17 = getelementptr inbounds { i64, double }, { i64, double }* %tuple1, i32 0, i32 0
  %18 = getelementptr inbounds { i64, double }, { i64, double }* %tuple1, i32 0, i32 1
  store i64 %a, i64* %17, align 4
  store double %b, double* %18, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i32 1)
  %19 = call %String* @__quantum__rt__string_create(i8* null)
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %20 to { %String*, %Qubit* }*
  %21 = getelementptr inbounds { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i32 0, i32 0
  %22 = getelementptr inbounds { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i32 0, i32 1
  store %String* %19, %String** %21, align 8
  store %Qubit* %qb, %Qubit** %22, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %20, i32 1)
  %23 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %25 = bitcast %Tuple* %24 to { %Callable*, { i64, double }* }*
  %26 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %25, i32 0, i32 0
  %27 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %25, i32 0, i32 1
  store %Callable* %23, %Callable** %26, align 8
  store { i64, double }* %tuple1, { i64, double }** %27, align 8
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, [2 x void (%Tuple*, i32)*]* @MemoryManagement__2, %Tuple* %24)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial1, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial1, i32 1)
  %28 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %30 = bitcast %Tuple* %29 to { %Callable*, { %String*, %Qubit* }* }*
  %31 = getelementptr inbounds { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %30, i32 0, i32 0
  %32 = getelementptr inbounds { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %30, i32 0, i32 1
  store %Callable* %28, %Callable** %31, align 8
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %32, align 8
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, [2 x void (%Tuple*, i32)*]* @MemoryManagement__3, %Tuple* %29)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial2, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial2, i32 1)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %20, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %16, %Tuple* null)
  %33 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %35 = bitcast %Tuple* %34 to { %Callable*, { i64, double }* }*
  %36 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %35, i32 0, i32 0
  %37 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %35, i32 0, i32 1
  store %Callable* %33, %Callable** %36, align 8
  store { i64, double }* %tuple1, { i64, double }** %37, align 8
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, [2 x void (%Tuple*, i32)*]* @MemoryManagement__4, %Tuple* %34)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial3, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial3, i32 1)
  %38 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %39 = call %String* @__quantum__rt__string_create(i8* null)
  %40 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %41 = bitcast %Tuple* %40 to { %Callable*, %String*, %Qubit* }*
  %42 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %41, i32 0, i32 0
  %43 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %41, i32 0, i32 1
  %44 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %41, i32 0, i32 2
  store %Callable* %38, %Callable** %42, align 8
  store %String* %39, %String** %43, align 8
  store %Qubit* %qb, %Qubit** %44, align 8
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, [2 x void (%Tuple*, i32)*]* @MemoryManagement__5, %Tuple* %40)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial4, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial4, i32 1)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %20, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %16, %Tuple* null)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %20, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial4, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial4, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %20, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %23, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %28, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %29, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %33, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %34, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %38, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %39, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %40, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial4, i32 -1)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %45 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %46 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %46)
  %47 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %48 = bitcast %Tuple* %47 to { %Callable*, %Callable* }*
  %49 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %48, i32 0, i32 0
  %50 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %48, i32 0, i32 1
  store %Callable* %45, %Callable** %49, align 8
  store %Callable* %46, %Callable** %50, align 8
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, [2 x void (%Tuple*, i32)*]* @MemoryManagement__6, %Tuple* %47)
  %52 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %53 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %52, i64 0)
  %54 = bitcast i8* %53 to %Qubit**
  store %Qubit* %qb, %Qubit** %54, align 8
  %55 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %52)
  %56 = bitcast { %Array* }* %55 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %51, %Tuple* %56, %Tuple* null)
  %57 = getelementptr inbounds { %Array* }, { %Array* }* %55, i32 0, i32 0
  %58 = load %Array*, %Array** %57, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %45, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %46, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %47, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %51, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %52, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %58, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %56, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %59 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__capture_update_alias_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %unrotate, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %unrotate, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %unrotate, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %unrotate, i32 -1)
  ret i1 true
}
