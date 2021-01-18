define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 0
  %3 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %4, %Callable** %2
  store double 2.500000e-01, double* %3
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, [2 x void (%Tuple*, i64)*]* @MemoryManagement__1, %Tuple* %0)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 true)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %unrotate, i64 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %61, %exiting__1 ]
  %5 = icmp sle i64 %i, 100
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { %Qubit* }*
  %8 = getelementptr { %Qubit* }, { %Qubit* }* %7, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %8
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %6, %Tuple* null)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Qubit* }*
  %11 = getelementptr { %Qubit* }, { %Qubit* }* %10, i64 0, i32 0
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
  %17 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 0
  %18 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 1
  store i64 %a, i64* %17
  store double %b, double* %18
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %16, i64 1)
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %19 to { %String*, %Qubit* }*
  %20 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %21 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  %22 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %String* %22, %String** %20
  store %Qubit* %qb, %Qubit** %21
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %19, i64 1)
  %23 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %24 = bitcast %Tuple* %23 to { %Callable*, { i64, double }* }*
  %25 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %24, i64 0, i32 0
  %26 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %24, i64 0, i32 1
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 1)
  store %Callable* %27, %Callable** %25
  store { i64, double }* %tuple1, { i64, double }** %26
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, [2 x void (%Tuple*, i64)*]* @MemoryManagement__2, %Tuple* %23)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %29 = bitcast %Tuple* %28 to { %Callable*, { %String*, %Qubit* }* }*
  %30 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %29, i64 0, i32 0
  %31 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %29, i64 0, i32 1
  %32 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %33 = load %String*, %String** %20
  call void @__quantum__rt__string_update_reference_count(%String* %33, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  store %Callable* %32, %Callable** %30
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %31
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, [2 x void (%Tuple*, i64)*]* @MemoryManagement__3, %Tuple* %28)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %19, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %16, %Tuple* null)
  %34 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %35 = bitcast %Tuple* %34 to { %Callable*, { i64, double }* }*
  %36 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %35, i64 0, i32 0
  %37 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %35, i64 0, i32 1
  %38 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 1)
  store %Callable* %38, %Callable** %36
  store { i64, double }* %tuple1, { i64, double }** %37
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, [2 x void (%Tuple*, i64)*]* @MemoryManagement__4, %Tuple* %34)
  %39 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %40 = bitcast %Tuple* %39 to { %Callable*, %String*, %Qubit* }*
  %41 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %40, i64 0, i32 0
  %42 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %40, i64 0, i32 1
  %43 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %40, i64 0, i32 2
  %44 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %45 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %Callable* %44, %Callable** %41
  store %String* %45, %String** %42
  store %Qubit* %qb, %Qubit** %43
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, [2 x void (%Tuple*, i64)*]* @MemoryManagement__5, %Tuple* %39)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %19, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %16, %Tuple* null)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %16, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 -1)
  %46 = load %String*, %String** %20
  call void @__quantum__rt__string_update_reference_count(%String* %46, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
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
  %47 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %48 = bitcast %Tuple* %47 to { %Callable*, %Callable* }*
  %49 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %48, i64 0, i32 0
  %50 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %48, i64 0, i32 1
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %52 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %52)
  store %Callable* %51, %Callable** %49
  store %Callable* %52, %Callable** %50
  %53 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, [2 x void (%Tuple*, i64)*]* @MemoryManagement__6, %Tuple* %47)
  %54 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %55 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %54, i64 0)
  %56 = bitcast i8* %55 to %Qubit**
  store %Qubit* %qb, %Qubit** %56
  %57 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %54)
  %58 = bitcast { %Array* }* %57 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %53, %Tuple* %58, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %53, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %53, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %54, i64 -1)
  %59 = getelementptr { %Array* }, { %Array* }* %57, i64 0, i32 0
  %60 = load %Array*, %Array** %59
  call void @__quantum__rt__array_update_reference_count(%Array* %60, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %58, i64 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %61 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %unrotate, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %unrotate, i64 -1)
  ret i1 true
}
