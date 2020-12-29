define i64 @Microsoft__Quantum__Testing__QIR__TestDeconstruct__body(i64 %0, { i64, i64 }* %1) {
entry:
  %2 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, { i64, i64 }* }* getelementptr ({ i64, { i64, i64 }* }, { i64, { i64, i64 }* }* null, i32 1) to i64))
  %a = bitcast %Tuple* %2 to { i64, { i64, i64 }* }*
  %3 = getelementptr { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i64 0, i32 0
  %4 = getelementptr { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i64 0, i32 1
  store i64 %0, i64* %3
  store { i64, i64 }* %1, { i64, i64 }** %4
  %5 = bitcast { i64, i64 }* %1 to %Tuple*
  call void @__quantum__rt__tuple_reference(%Tuple* %5)
  %6 = getelementptr { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i64 0, i32 0
  %7 = getelementptr { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i64 0, i32 1
  %x = load i64, i64* %6
  %y = load { i64, i64 }*, { i64, i64 }** %7
  %b = alloca i64
  store i64 3, i64* %b
  %c = alloca i64
  store i64 5, i64* %c
  %8 = getelementptr { i64, i64 }, { i64, i64 }* %y, i64 0, i32 0
  %9 = getelementptr { i64, i64 }, { i64, i64 }* %y, i64 0, i32 1
  %10 = load i64, i64* %8
  %11 = load i64, i64* %9
  store i64 %10, i64* %b
  store i64 %11, i64* %c
  %12 = load i64, i64* %b
  %13 = load i64, i64* %c
  %14 = mul i64 %12, %13
  %15 = add i64 %x, %14
  %16 = getelementptr { i64, { i64, i64 }* }, { i64, { i64, i64 }* }* %a, i64 0, i32 1
  %17 = load { i64, i64 }*, { i64, i64 }** %16
  %18 = bitcast { i64, i64 }* %17 to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %18)
  %19 = bitcast { i64, { i64, i64 }* }* %a to %Tuple*
  call void @__quantum__rt__tuple_unreference(%Tuple* %19)
  ret i64 %15
}
