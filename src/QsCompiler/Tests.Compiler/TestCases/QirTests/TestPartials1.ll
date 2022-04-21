define internal i1 @Microsoft__Quantum__Testing__QIR__TestPartials__body(i64 %a, double %b) {
entry:
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, double }* getelementptr ({ %Callable*, double }, { %Callable*, double }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable*, double }*
  %2 = getelementptr inbounds { %Callable*, double }, { %Callable*, double }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { %Callable*, double }, { %Callable*, double }* %1, i32 0, i32 1
  %4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Rz__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %4, %Callable** %2, align 8
  store double 2.500000e-01, double* %3, align 8
  %rotate = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %0)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %rotate, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %rotate, i32 1)
  %unrotate = call %Callable* @__quantum__rt__callable_copy(%Callable* %rotate, i1 false)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %unrotate, i32 1)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %unrotate)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %unrotate, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %unrotate, i32 1)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %67, %exiting__1 ]
  %5 = icmp sle i64 %i, 100
  br i1 %5, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %qb = call %Qubit* @__quantum__rt__qubit_allocate()
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit* }* getelementptr ({ %Qubit* }, { %Qubit* }* null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { %Qubit* }*
  %8 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %7, i32 0, i32 0
  store %Qubit* %qb, %Qubit** %8, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %rotate, %Tuple* %6, %Tuple* null)
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Qubit* }* getelementptr ({ %Qubit* }, { %Qubit* }* null, i32 1) to i64))
  %10 = bitcast %Tuple* %9 to { %Qubit* }*
  %11 = getelementptr inbounds { %Qubit* }, { %Qubit* }* %10, i32 0, i32 0
  store %Qubit* %qb, %Qubit** %11, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %unrotate, %Tuple* %9, %Tuple* null)
  %12 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  %13 = call %Result* @__quantum__rt__result_get_zero()
  %14 = call i1 @__quantum__rt__result_equal(%Result* %12, %Result* %13)
  %15 = xor i1 %14, true
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i32 -1)
  br i1 %15, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, double }* getelementptr ({ i64, double }, { i64, double }* null, i32 1) to i64))
  %tuple1 = bitcast %Tuple* %16 to { i64, double }*
  %17 = getelementptr inbounds { i64, double }, { i64, double }* %tuple1, i32 0, i32 0
  %18 = getelementptr inbounds { i64, double }, { i64, double }* %tuple1, i32 0, i32 1
  store i64 %a, i64* %17, align 4
  store double %b, double* %18, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i32 1)
  %19 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String*, %Qubit* }* getelementptr ({ %String*, %Qubit* }, { %String*, %Qubit* }* null, i32 1) to i64))
  %tuple2 = bitcast %Tuple* %19 to { %String*, %Qubit* }*
  %20 = getelementptr inbounds { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i32 0, i32 0
  %21 = getelementptr inbounds { %String*, %Qubit* }, { %String*, %Qubit* }* %tuple2, i32 0, i32 1
  %22 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @0, i32 0, i32 0))
  store %String* %22, %String** %20, align 8
  store %Qubit* %qb, %Qubit** %21, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i32 1)
  %23 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, { i64, double }* }* getelementptr ({ %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* null, i32 1) to i64))
  %24 = bitcast %Tuple* %23 to { %Callable*, { i64, double }* }*
  %25 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %24, i32 0, i32 0
  %26 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %24, i32 0, i32 1
  %27 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 1)
  store %Callable* %27, %Callable** %25, align 8
  store { i64, double }* %tuple1, { i64, double }** %26, align 8
  %partial1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__2__FunctionTable, %Tuple* %23)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial1, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial1, i32 1)
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, { %String*, %Qubit* }* }* getelementptr ({ %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* null, i32 1) to i64))
  %29 = bitcast %Tuple* %28 to { %Callable*, { %String*, %Qubit* }* }*
  %30 = getelementptr inbounds { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %29, i32 0, i32 0
  %31 = getelementptr inbounds { %Callable*, { %String*, %Qubit* }* }, { %Callable*, { %String*, %Qubit* }* }* %29, i32 0, i32 1
  %32 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__InnerNestedTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i32 1)
  store %Callable* %32, %Callable** %30, align 8
  store { %String*, %Qubit* }* %tuple2, { %String*, %Qubit* }** %31, align 8
  %partial2 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__3__FunctionTable, %Tuple* %28)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial2, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial2, i32 1)
  call void @__quantum__rt__callable_invoke(%Callable* %partial1, %Tuple* %19, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial2, %Tuple* %16, %Tuple* null)
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, { i64, double }* }* getelementptr ({ %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* null, i32 1) to i64))
  %34 = bitcast %Tuple* %33 to { %Callable*, { i64, double }* }*
  %35 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %34, i32 0, i32 0
  %36 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %34, i32 0, i32 1
  %37 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 1)
  store %Callable* %37, %Callable** %35, align 8
  store { i64, double }* %tuple1, { i64, double }** %36, align 8
  %partial3 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__4__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__4__FunctionTable, %Tuple* %33)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial3, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial3, i32 1)
  %38 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %String*, %Qubit* }* getelementptr ({ %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* null, i32 1) to i64))
  %39 = bitcast %Tuple* %38 to { %Callable*, %String*, %Qubit* }*
  %40 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %39, i32 0, i32 0
  %41 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %39, i32 0, i32 1
  %42 = getelementptr inbounds { %Callable*, %String*, %Qubit* }, { %Callable*, %String*, %Qubit* }* %39, i32 0, i32 2
  %43 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__TakesNestedTuple__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %44 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @0, i32 0, i32 0))
  store %Callable* %43, %Callable** %40, align 8
  store %String* %44, %String** %41, align 8
  store %Qubit* %qb, %Qubit** %42, align 8
  %partial4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__5__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__5__FunctionTable, %Tuple* %38)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial4, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial4, i32 1)
  call void @__quantum__rt__callable_invoke(%Callable* %partial3, %Tuple* %19, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %partial4, %Tuple* %16, %Tuple* null)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %partial4, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %partial4, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial3, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %partial4, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %partial4, i32 -1)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  %45 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Callable* }* getelementptr ({ %Callable*, %Callable* }, { %Callable*, %Callable* }* null, i32 1) to i64))
  %46 = bitcast %Tuple* %45 to { %Callable*, %Callable* }*
  %47 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %46, i32 0, i32 0
  %48 = getelementptr inbounds { %Callable*, %Callable* }, { %Callable*, %Callable* }* %46, i32 0, i32 1
  %49 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__ApplyToLittleEndian__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %50 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Dummy__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %50)
  store %Callable* %49, %Callable** %47, align 8
  store %Callable* %50, %Callable** %48, align 8
  %51 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__6__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__6__FunctionTable, %Tuple* %45)
  %52 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %53 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %52, i64 0)
  %54 = bitcast i8* %53 to %Qubit**
  store %Qubit* %qb, %Qubit** %54, align 8
  %55 = call { %Array* }* @Microsoft__Quantum__Testing__QIR__LittleEndian__body(%Array* %52)
  %56 = bitcast { %Array* }* %55 to %Tuple*
  call void @__quantum__rt__callable_invoke(%Callable* %51, %Tuple* %56, %Tuple* null)
  %57 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, %Callable*, %Qubit* }* getelementptr ({ %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* null, i32 1) to i64))
  %58 = bitcast %Tuple* %57 to { %Callable*, %Callable*, %Qubit* }*
  %59 = getelementptr inbounds { %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* %58, i32 0, i32 0
  %60 = getelementptr inbounds { %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* %58, i32 0, i32 1
  %61 = getelementptr inbounds { %Callable*, %Callable*, %Qubit* }, { %Callable*, %Callable*, %Qubit* }* %58, i32 0, i32 2
  %62 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR______GUID____Delay__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  %63 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__H__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %62, %Callable** %59, align 8
  store %Callable* %63, %Callable** %60, align 8
  store %Qubit* %qb, %Qubit** %61, align 8
  %64 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__7__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__7__FunctionTable, %Tuple* %57)
  call void @__quantum__rt__callable_invoke(%Callable* %64, %Tuple* null, %Tuple* null)
  %65 = getelementptr inbounds { %Array* }, { %Array* }* %55, i32 0, i32 0
  %66 = load %Array*, %Array** %65, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %51, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %51, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %52, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %66, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %56, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %64, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %64, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qb)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %67 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__capture_update_alias_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %unrotate, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %unrotate, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %rotate, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %unrotate, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %unrotate, i32 -1)
  ret i1 true
}
