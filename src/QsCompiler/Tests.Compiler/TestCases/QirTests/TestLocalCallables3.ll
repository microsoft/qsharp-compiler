define internal %Array* @Microsoft__Quantum__Testing__QIR__LazyConstruction__body(i1 %cond) {
entry:
  br i1 %cond, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Foo__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %1 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Bar__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %op = phi %Callable* [ %0, %condTrue__1 ], [ %1, %condFalse__1 ]
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op, i32 1)
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64 }* getelementptr ({ i64 }, { i64 }* null, i32 1) to i64))
  %3 = bitcast %Tuple* %2 to { i64 }*
  %4 = getelementptr inbounds { i64 }, { i64 }* %3, i32 0, i32 0
  store i64 5, i64* %4, align 4
  call void @__quantum__rt__callable_invoke(%Callable* %op, %Tuple* %2, %Tuple* null)
  br i1 %cond, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condContinue__1
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Foo__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__2

condFalse__2:                                     ; preds = %condContinue__1
  %6 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Bar__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condTrue__2
  %7 = phi %Callable* [ %5, %condTrue__2 ], [ %6, %condFalse__2 ]
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64 }* getelementptr ({ i64 }, { i64 }* null, i32 1) to i64))
  %9 = bitcast %Tuple* %8 to { i64 }*
  %10 = getelementptr inbounds { i64 }, { i64 }* %9, i32 0, i32 0
  store i64 4, i64* %10, align 4
  call void @__quantum__rt__callable_invoke(%Callable* %7, %Tuple* %8, %Tuple* null)
  br i1 %cond, label %condTrue__3, label %condFalse__3

condTrue__3:                                      ; preds = %condContinue__2
  %11 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Foo__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__3

condFalse__3:                                     ; preds = %condContinue__2
  %12 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Bar__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__3

condContinue__3:                                  ; preds = %condFalse__3, %condTrue__3
  %op2 = phi %Callable* [ %11, %condTrue__3 ], [ %12, %condFalse__3 ]
  %13 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %tuple = bitcast %Tuple* %13 to { %Callable*, i64 }*
  %14 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %tuple, i32 0, i32 0
  %15 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %tuple, i32 0, i32 1
  store %Callable* %op2, %Callable** %14, align 8
  store i64 3, i64* %15, align 4
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op2, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op2, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %13, i32 1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op2, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op2, i32 1)
  %16 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64 }* getelementptr ({ i64 }, { i64 }* null, i32 1) to i64))
  %17 = bitcast %Tuple* %16 to { i64 }*
  %18 = getelementptr inbounds { i64 }, { i64 }* %17, i32 0, i32 0
  store i64 3, i64* %18, align 4
  call void @__quantum__rt__callable_invoke(%Callable* %op2, %Tuple* %16, %Tuple* null)
  %19 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Foo__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br i1 %cond, label %condTrue__4, label %condFalse__4

condTrue__4:                                      ; preds = %condContinue__3
  %20 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Foo__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__4

condFalse__4:                                     ; preds = %condContinue__3
  %21 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Bar__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null)
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__4, %condTrue__4
  %22 = phi %Callable* [ %20, %condTrue__4 ], [ %21, %condFalse__4 ]
  %23 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %23, i64 0)
  %25 = bitcast i8* %24 to %Callable**
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %23, i64 1)
  %27 = bitcast i8* %26 to %Callable**
  store %Callable* %19, %Callable** %25, align 8
  store %Callable* %22, %Callable** %27, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op2, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op2, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %13, i32 -1)
  call void @__quantum__rt__capture_update_alias_count(%Callable* %op2, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %op2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %op, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %7, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %7, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1)
  call void @__quantum__rt__capture_update_reference_count(%Callable* %op2, i32 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %op2, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %13, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i32 -1)
  ret %Array* %23
}
