define void @MemoryManagement__2__RefCount(%Tuple* %capture-tuple, i32 %count-change) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i32 0, i32 0
  %2 = load %Callable*, %Callable** %1, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %2, i32 %count-change)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %2, i32 %count-change)
  %3 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i32 0, i32 1
  %4 = load { i64, double }*, { i64, double }** %3, align 8
  %5 = bitcast { i64, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 %count-change)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %capture-tuple, i32 %count-change)
  ret void
}

define void @MemoryManagement__2__AliasCount(%Tuple* %capture-tuple, i32 %count-change) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, { i64, double }* }*
  %1 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i32 0, i32 0
  %2 = load %Callable*, %Callable** %1, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %2, i32 %count-change)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %2, i32 %count-change)
  %3 = getelementptr inbounds { %Callable*, { i64, double }* }, { %Callable*, { i64, double }* }* %0, i32 0, i32 1
  %4 = load { i64, double }*, { i64, double }** %3, align 8
  %5 = bitcast { i64, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 %count-change)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %capture-tuple, i32 %count-change)
  ret void
}
