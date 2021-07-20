define internal void @Microsoft__Quantum__Testing__QIR__TestBuiltInIntrinsics__body() {
entry:
  %0 = call { %Callable*, %Callable*, %Callable* }* @Microsoft__Quantum__Testing__QIR__DefaultOptions__body()
  %1 = bitcast { %Callable*, %Callable*, %Callable* }* %0 to %Tuple*
  %2 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %1, i1 false)
  %3 = icmp ne %Tuple* %1, %2
  %4 = bitcast %Tuple* %2 to { %Callable*, %Callable*, %Callable* }*
  %5 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %4, i32 0, i32 0
  %6 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Message, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br i1 %3, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %entry
  %7 = load %Callable*, %Callable** %5, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %6, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %6, i32 1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %7, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %7, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %entry
  store %Callable* %6, %Callable** %5, align 8
  %8 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %9 = icmp ne %Tuple* %2, %8
  %10 = bitcast %Tuple* %8 to { %Callable*, %Callable*, %Callable* }*
  %11 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %10, i32 0, i32 1
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Diagnostics__DumpMachine, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br i1 %9, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %condContinue__1
  %13 = load %Callable*, %Callable** %11, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %12, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %12, i32 1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %13, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %13, i32 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condContinue__1
  store %Callable* %12, %Callable** %11, align 8
  %14 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %8, i1 false)
  %15 = icmp ne %Tuple* %8, %14
  %options = bitcast %Tuple* %14 to { %Callable*, %Callable*, %Callable* }*
  %16 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 2
  %17 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Diagnostics__DumpMachine, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br i1 %15, label %condContinue__3, label %condFalse__3

condFalse__3:                                     ; preds = %condContinue__2
  %18 = load %Callable*, %Callable** %16, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %17, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %17, i32 1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %18, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %18, i32 -1)
  br label %condContinue__3

condContinue__3:                                  ; preds = %condFalse__3, %condContinue__2
  store %Callable* %17, %Callable** %16, align 8
  %19 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 0
  %20 = load %Callable*, %Callable** %19, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %20, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %20, i32 1)
  %21 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 1
  %22 = load %Callable*, %Callable** %21, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %22, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %22, i32 1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %17, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %17, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 1)
  %23 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %24 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %25 = bitcast %Tuple* %24 to { %String* }*
  %26 = getelementptr inbounds { %String* }, { %String* }* %25, i32 0, i32 0
  store %String* %23, %String** %26, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %20, %Tuple* %24, %Tuple* null)
  %27 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @1, i32 0, i32 0))
  %28 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %29 = bitcast %Tuple* %28 to { %String* }*
  %30 = getelementptr inbounds { %String* }, { %String* }* %29, i32 0, i32 0
  store %String* %27, %String** %30, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %22, %Tuple* %28, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %17, %Tuple* null, %Tuple* null)
  %31 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %0, i32 0, i32 0
  %32 = load %Callable*, %Callable** %31, align 8
  %33 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %0, i32 0, i32 1
  %34 = load %Callable*, %Callable** %33, align 8
  %35 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %0, i32 0, i32 2
  %36 = load %Callable*, %Callable** %35, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %20, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %20, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %22, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %22, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %32, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %32, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %34, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %34, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %36, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %36, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %6, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %6, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %12, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %12, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %23, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %28, i32 -1)
  ret void
}
