define internal i64 @Microsoft__Quantum__Testing__QIR__TestBuiltInIntrinsics__body() {
entry:
  %0 = call { %Callable*, %Callable*, %Callable* }* @Microsoft__Quantum__Testing__QIR__DefaultOptions__body()
  %1 = bitcast { %Callable*, %Callable*, %Callable* }* %0 to %Tuple*
  %2 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %1, i1 false)
  %3 = bitcast %Tuple* %2 to { %Callable*, %Callable*, %Callable* }*
  %4 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %3, i32 0, i32 0
  %5 = load %Callable*, %Callable** %4, align 8
  %6 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic__Message__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %6, %Callable** %4, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  %7 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %8 = bitcast %Tuple* %7 to { %Callable*, %Callable*, %Callable* }*
  %9 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %8, i32 0, i32 1
  %10 = load %Callable*, %Callable** %9, align 8
  %11 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Diagnostics__DumpMachine__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %11, %Callable** %9, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  %12 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %7, i1 false)
  %options = bitcast %Tuple* %12 to { %Callable*, %Callable*, %Callable* }*
  %13 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 2
  %14 = load %Callable*, %Callable** %13, align 8
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Diagnostics__DumpMachine__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  store %Callable* %15, %Callable** %13, align 8
  %16 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 0
  %17 = load %Callable*, %Callable** %16, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %17, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %17, i32 1)
  %18 = getelementptr inbounds { %Callable*, %Callable*, %Callable* }, { %Callable*, %Callable*, %Callable* }* %options, i32 0, i32 1
  %19 = load %Callable*, %Callable** %18, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %19, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %19, i32 1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %15, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %15, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %12, i32 1)
  %20 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %21 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String* }* getelementptr ({ %String* }, { %String* }* null, i32 1) to i64))
  %22 = bitcast %Tuple* %21 to { %String* }*
  %23 = getelementptr inbounds { %String* }, { %String* }* %22, i32 0, i32 0
  store %String* %20, %String** %23, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %17, %Tuple* %21, %Tuple* null)
  %24 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @1, i32 0, i32 0))
  %25 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %String* }* getelementptr ({ %String* }, { %String* }* null, i32 1) to i64))
  %26 = bitcast %Tuple* %25 to { %String* }*
  %27 = getelementptr inbounds { %String* }, { %String* }* %26, i32 0, i32 0
  store %String* %24, %String** %27, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %19, %Tuple* %25, %Tuple* null)
  call void @__quantum__qis__dumpmachine__body(i8* null)
  %28 = call i64 @llvm.readcyclecounter()
  call void @__quantum__rt__capture_update_alias_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %19, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %19, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %15, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %15, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %12, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %14, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %14, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %17, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %19, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %19, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %15, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %15, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %12, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %21, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %24, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %25, i32 -1)
  ret i64 %28
}
