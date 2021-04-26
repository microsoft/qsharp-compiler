define void @Microsoft__Quantum__Testing__QIR__TestBuiltInIntrinsics__body() #0 {
entry:
  %0 = call { %Callable* }* @Microsoft__Quantum__Testing__QIR__DefaultOptions__body()
  %1 = bitcast { %Callable* }* %0 to %Tuple*
  %2 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %1, i1 false)
  %3 = icmp ne %Tuple* %1, %2
  %options = bitcast %Tuple* %2 to { %Callable* }*
  %4 = getelementptr inbounds { %Callable* }, { %Callable* }* %options, i32 0, i32 0
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Intrinsic_____GUID___Message, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br i1 %3, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %entry
  %6 = load %Callable*, %Callable** %4, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %5, i32 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %5, i32 1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %6, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %6, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %entry
  store %Callable* %5, %Callable** %4, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %5, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %5, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 1)
  %7 = getelementptr inbounds { %Callable* }, { %Callable* }* %0, i32 0, i32 0
  %8 = load %Callable*, %Callable** %7, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %8, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %1, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  ret void
}
