define i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) #0 {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 0
  %3 = getelementptr { %Callable*, double }, { %Callable*, double }* %1, i64 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz, %Tuple* null)
  store %Callable* %4, %Callable** %2
  store double 2.500000e-01, double* %3
  %rotate = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1, %Tuple* %0)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 true)
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %6 = bitcast %Tuple* %5 to { i64 }*
  %7 = getelementptr { i64 }, { i64 }* %6, i64 0, i32 0
  store i64 1, i64* %7
  call void @__quantum__rt__callable_memory_management(%Callable* %unrotate, %Tuple* %5, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %72, %exiting__1 ]
  %8 = icmp sge i64 %i, 100
  %9 = icmp sle i64 %i, 100
  %10 = select i1 true, i1 %9, i1 %8
  br i1 %10, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %12 = bitcast %Tuple* %11 to { %Qubit* }*
  %13 = getelementptr { %Qubit* }, { %Qubit* }* %12, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %13
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %11, %Tuple* null)
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { %Qubit* }*
  %16 = getelementptr { %Qubit* }, { %Qubit* }* %15, i64 0, i32 0
  store %Qubit* %qb, %Qubit** %16
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %14, %Tuple* null)
  %17 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %18 = load %Result*, %Result** @ResultZero
  %19 = call i1 @__quantum__rt__result_equal(%Result* %17, %Result* %18)
  %20 = xor i1 %19, true
  br i1 %20, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %21 to { i64, double }*
  %22 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 0
  %23 = getelementptr { i64, double }, { i64, double }* %tuple1, i64 0, i32 1
  store i64 %a, i64* %22
  store double %b, double* %23
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %21, i64 1)
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %tuple2 = bitcast %Tuple* %24 to { %String*, %Qubit* }*
  %25 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 0
  %26 = getelementptr { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i64 0, i32 1
  %27 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %String* %27, %String** %25
  store %Qubit* %qb, %Qubit** %26
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %24, i64 1)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %29 = bitcast %Tuple* %28 to { %Callable*, { i64, double }* }*
  %30 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 0
  %31 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %29, i64 0, i32 1
  %32 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %21, i64 1)
  store %Callable* %32, %Callable** %30
  store { i64, double }* %tuple1, { i64, double }** %31
  %partial1 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2, %Tuple* %28)
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %34 = bitcast %Tuple* %33 to { %Callable*, { %String*, %Qubit* }* }*
  %35 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %34, i64 0, i32 0
  %36 = getelementptr { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %34, i64 0, i32 1
  %37 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple, %Tuple* null)
  %38 = load %String*, %String** %25
  call void @__quantum__rt__string_update_reference_count(%String* %38, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 1)
  store %Callable* %37, %Callable** %35
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %36
  %partial2 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, %Tuple* %33)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %24, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %21, %Tuple* null)
  %39 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %40 = bitcast %Tuple* %39 to { %Callable*, { i64, double }* }*
  %41 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %40, i64 0, i32 0
  %42 = getelementptr { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %40, i64 0, i32 1
  %43 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %21, i64 1)
  store %Callable* %43, %Callable** %41
  store { i64, double }* %tuple1, { i64, double }** %42
  %partial3 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4, %Tuple* %39)
  %44 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 3))
  %45 = bitcast %Tuple* %44 to { %Callable*, %String*, %Qubit* }*
  %46 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %45, i64 0, i32 0
  %47 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %45, i64 0, i32 1
  %48 = getelementptr { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %45, i64 0, i32 2
  %49 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple, %Tuple* null)
  %50 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %Callable* %49, %Callable** %46
  store %String* %50, %String** %47
  store %Qubit* %qb, %Qubit** %48
  %partial4 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5, %Tuple* %44)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %24, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %21, %Tuple* null)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %21, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %21, i64 -1)
  %51 = load %String*, %String** %25
  call void @__quantum__rt__string_update_reference_count(%String* %51, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  %52 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %53 = bitcast %Tuple* %52 to { i64 }*
  %54 = getelementptr { i64 }, { i64 }* %53, i64 0, i32 0
  store i64 -1, i64* %54
  call void @__quantum__rt__callable_memory_management(%Callable* %partial1, %Tuple* %52, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial1, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %partial2, %Tuple* %52, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial2, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %partial3, %Tuple* %52, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial3, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %partial4, %Tuple* %52, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial4, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %52, i64 -1)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %55 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %56 = bitcast %Tuple* %55 to { %Callable*, %Callable* }*
  %57 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %56, i64 0, i32 0
  %58 = getelementptr { %Callable*, %Callable* }, { %Callable*, %Callable* }* %56, i64 0, i32 1
  %59 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian, %Tuple* null)
  %60 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %60)
  store %Callable* %59, %Callable** %57
  store %Callable* %60, %Callable** %58
  %61 = call %Callable* @__quantum__rt__callable_create([5 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6, %Tuple* %55)
  %62 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %63 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %62, i64 0)
  %64 = bitcast i8* %63 to %Qubit**
  store %Qubit* %qb, %Qubit** %64
  %65 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %62)
  %66 = bitcast { %Array* }* %65 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %61, %Tuple* %66, %Tuple* null)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %17, i64 -1)
  %67 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %68 = bitcast %Tuple* %67 to { i64 }*
  %69 = getelementptr { i64 }, { i64 }* %68, i64 0, i32 0
  store i64 -1, i64* %69
  call void @__quantum__rt__callable_memory_management(%Callable* %61, %Tuple* %67, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %61, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %62, i64 -1)
  %70 = getelementptr { %Array* }, { %Array* }* %65, i64 0, i32 0
  %71 = load %Array*, %Array** %70
  call void @__quantum__rt__array_update_reference_count(%Array* %71, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %66, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %67, i64 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %72 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %73 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %74 = bitcast %Tuple* %73 to { i64 }*
  %75 = getelementptr { i64 }, { i64 }* %74, i64 0, i32 0
  store i64 -1, i64* %75
  call void @__quantum__rt__callable_memory_management(%Callable* %rotate, %Tuple* %73, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %rotate, i64 -1)
  call void @__quantum__rt__callable_memory_management(%Callable* %unrotate, %Tuple* %73, %Tuple* null)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %unrotate, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %73, i64 -1)
  ret i1 true
}
