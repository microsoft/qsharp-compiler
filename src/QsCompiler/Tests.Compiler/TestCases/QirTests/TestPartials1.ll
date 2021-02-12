define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr inbounds { %Callable*, double }, { %Callable*, double }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { %Callable*, double }, { %Callable*, double }* %1, i32 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %4, %Callable** %2
  store double 2.500000e-01, double* %3
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, [2 x void (%Tuple*, i64)*]* @MemoryManagement__1, %Tuple* %0)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %rotate, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %rotate, i64 1)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 false)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %unrotate, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %unrotate, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %unrotate, i64 1)
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
  store %Qubit* %qb, %Qubit** %8
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %6, %Tuple* null)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Qubit* }*
  %11 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %10, i32 0, i32 0
  store %Qubit* %qb, %Qubit** %11
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %9, %Tuple* null)
  %12 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %13 = load %Result*, %Result** @ResultZero
  %14 = call i1 @__quantum__rt__result_equal(%Result* %12, %Result* %13)
  %15 = xor i1 %14, true
  br i1 %15, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %16 to { i64, double }*
  %17 = getelementptr inbounds { i64, double }, { i64, double }* %tuple1, i32 0, i32 0
  %18 = getelementptr inbounds { i64, double }, { i64, double }* %tuple1, i32 0, i32 1
  store i64 %a, i64* %17
  store double %b, double* %18
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i64 1)
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %19 to { %String*, %Qubit* }*
  %20 = getelementptr inbounds { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i32 0, i32 0
  %21 = getelementptr inbounds { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i32 0, i32 1
  %22 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %String* %22, %String** %20
  store %Qubit* %qb, %Qubit** %21
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i64 1)
  %23 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %24 = bitcast %Tuple* %23 to { %Callable*, { i64, double }* }*
  %25 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %24, i32 0, i32 0
  %26 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %24, i32 0, i32 1
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %27, %Callable** %25
  store { i64, double }* %tuple1, { i64, double }** %26
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, [2 x void (%Tuple*, i64)*]* @MemoryManagement__2, %Tuple* %23)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial1, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial1, i64 1)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %29 = bitcast %Tuple* %28 to { %Callable*, { %String*, %Qubit* }* }*
  %30 = getelementptr inbounds { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %29, i32 0, i32 0
  %31 = getelementptr inbounds { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %29, i32 0, i32 1
  %32 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %32, %Callable** %30
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %31
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, [2 x void (%Tuple*, i64)*]* @MemoryManagement__3, %Tuple* %28)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial2, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial2, i64 1)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %19, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %16, %Tuple* null)
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %34 = bitcast %Tuple* %33 to { %Callable*, { i64, double }* }*
  %35 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %34, i32 0, i32 0
  %36 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %34, i32 0, i32 1
  %37 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %37, %Callable** %35
  store { i64, double }* %tuple1, { i64, double }** %36
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, [2 x void (%Tuple*, i64)*]* @MemoryManagement__4, %Tuple* %33)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial3, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial3, i64 1)
  %38 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %39 = bitcast %Tuple* %38 to { %Callable*, %String*, %Qubit* }*
  %40 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %39, i32 0, i32 0
  %41 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %39, i32 0, i32 1
  %42 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %39, i32 0, i32 2
  %43 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %44 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %Callable* %43, %Callable** %40
  store %String* %44, %String** %41
  store %Qubit* %qb, %Qubit** %42
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, [2 x void (%Tuple*, i64)*]* @MemoryManagement__5, %Tuple* %38)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial4, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial4, i64 1)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %19, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %16, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial1, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial1, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial2, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial2, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial3, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial3, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %partial4, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial4, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %partial1, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial1, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %partial2, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial2, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %partial3, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial3, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %partial4, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial4, i64 -1)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %45 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %46 = bitcast %Tuple* %45 to { %Callable*, %Callable* }*
  %47 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %46, i32 0, i32 0
  %48 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %46, i32 0, i32 1
  %49 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %50 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %50)
  store %Callable* %49, %Callable** %47
  store %Callable* %50, %Callable** %48
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, [2 x void (%Tuple*, i64)*]* @MemoryManagement__6, %Tuple* %45)
  %52 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %53 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %52, i64 0)
  %54 = bitcast i8* %53 to %Qubit**
  store %Qubit* %qb, %Qubit** %54
  %55 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %52)
  %56 = bitcast { %Array* }* %55 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %51, %Tuple* %56, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  %57 = getelementptr inbounds { %Array* }, { %Array* }* %55, i32 0, i32 0
  %58 = load %Array*, %Array** %57
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %51, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %51, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %52, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %58, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %56, i64 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %59 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %unrotate, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %unrotate, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %unrotate, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %unrotate, i64 -1)
  ret i1 true
}
