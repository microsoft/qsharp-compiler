define internal { double, i64, %BigInt*, i64 }* @Microsoft__Quantum__Testing__QIR__TestBuiltIn__body(i64 %arg) {
entry:
  %d = sitofp i64 %arg to double
  %i = fptosi double %d to i64
  %bi = call %BigInt* @__quantum__rt__bigint_create_i64(i64 %arg)
  %t = fptosi double %d to i64
  %0 = load %Range, %Range* @EmptyRange, align 4
  %1 = insertvalue %Range %0, i64 5, 0
  %2 = insertvalue %Range %1, i64 -2, 1
  %range = insertvalue %Range %2, i64 0, 2
  %3 = extractvalue %Range %range, 0
  %4 = extractvalue %Range %range, 1
  %5 = extractvalue %Range %range, 2
  %6 = sub i64 %5, %3
  %7 = sdiv i64 %6, %4
  %8 = mul i64 %4, %7
  %9 = add i64 %3, %8
  %10 = sub i64 0, %4
  %11 = load %Range, %Range* @EmptyRange, align 4
  %12 = insertvalue %Range %11, i64 %9, 0
  %13 = insertvalue %Range %12, i64 %10, 1
  %rev = insertvalue %Range %13, i64 %3, 2
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, i64, %BigInt*, i64 }* getelementptr ({ double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { double, i64, %BigInt*, i64 }*
  %16 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %15, i32 0, i32 0
  %17 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %15, i32 0, i32 1
  %18 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %15, i32 0, i32 2
  %19 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %15, i32 0, i32 3
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %bi, i32 1)
  store double %d, double* %16, align 8
  store i64 %i, i64* %17, align 4
  store %BigInt* %bi, %BigInt** %18, align 8
  store i64 %t, i64* %19, align 4
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %bi, i32 -1)
  ret { double, i64, %BigInt*, i64 }* %15
}
