define internal i64 @Microsoft__Quantum__Testing__QIR__TestDeconstruct__body(i64 %0, { i64, i64 }* %1) {
entry:
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i64, i64 }* }* getelementptr ({ i64, { i64, i64 }* }, { i64, { i64, i64 }* }* null, i32 1) to i64))
  %a = bitcast %Tuple* %2 to { i64, { i64, i64 }* }*
  %3 = getelementptr inbounds { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i32 0, i32 0
  %4 = getelementptr inbounds { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i32 0, i32 1
  store i64 %0, i64* %3, align 4
  store { i64, i64 }* %1, { i64, i64 }** %4, align 8
  %5 = bitcast { i64, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 1)
  %b = alloca i64, align 8
  store i64 3, i64* %b, align 4
  %c = alloca i64, align 8
  store i64 5, i64* %c, align 4
  %6 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %1, i32 0, i32 0
  %7 = load i64, i64* %6, align 4
  store i64 %7, i64* %b, align 4
  %8 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %1, i32 0, i32 1
  %9 = load i64, i64* %8, align 4
  store i64 %9, i64* %c, align 4
  %10 = mul i64 %7, %9
  %11 = add i64 %0, %10
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  ret i64 %11
}
