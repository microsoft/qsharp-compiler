define internal void @Microsoft__Quantum__Testing__QIR__TestBuiltInIntrinsics__body() {
entry:
  %0 = call { %Callable*, %Callable*, %Callable* }* @Microsoft__Quantum__Testing__QIR__DefaultOptions__body()
  %1 = bitcast { %Callable*, %Callable*, %Callable* }* %0 to %Tuple*
  %2 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %1, i1 false)
  %3 = bitcast %Tuple* %2 to { %Callable*, %Callable*, %Callable* }*
  %4 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %3, i32 0, i32 0
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Message, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %5, %Callable** %4, align 8
  %6 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %7 = bitcast %Tuple* %6 to { %Callable*, %Callable*, %Callable* }*
  %8 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %7, i32 0, i32 1
  %9 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Diagnostics__DumpMachine, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %9, %Callable** %8, align 8
  %10 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %6, i1 false)
  %options = bitcast %Tuple* %10 to { %Callable*, %Callable*, %Callable* }*
  %11 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 2
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Diagnostics__DumpMachine, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %12, %Callable** %11, align 8
  %13 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 0
  %14 = load %Callable*, %Callable** %13, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %14, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %14, i32 1)
  %15 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 1
  %16 = load %Callable*, %Callable** %15, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %16, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %16, i32 1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %12, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %12, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %10, i32 1)
  %17 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %18 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %19 = bitcast %Tuple* %18 to { %String* }*
  %20 = getelementptr inbounds { %String* }, { %String* }* %19, i32 0, i32 0
  store %String* %17, %String** %20, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %14, %Tuple* %18, %Tuple* null)
  %21 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @1, i32 0, i32 0))
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %23 = bitcast %Tuple* %22 to { %String* }*
  %24 = getelementptr inbounds { %String* }, { %String* }* %23, i32 0, i32 0
  store %String* %21, %String** %24, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %16, %Tuple* %22, %Tuple* null)
  call void @__quantum__rt__callable_invoke(%Callable* %12, %Tuple* null, %Tuple* null)
  %25 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %0, i32 0, i32 0
  %26 = load %Callable*, %Callable** %25, align 8
  %27 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %0, i32 0, i32 1
  %28 = load %Callable*, %Callable** %27, align 8
  %29 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %0, i32 0, i32 2
  %30 = load %Callable*, %Callable** %29, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %14, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %14, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %16, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %16, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %12, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %12, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %10, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %26, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %26, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %28, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %28, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %30, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %30, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %12, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %12, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %10, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i32 -1)
  ret void
}
