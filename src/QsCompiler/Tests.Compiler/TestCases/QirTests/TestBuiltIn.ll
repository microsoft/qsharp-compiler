define { double, %BigInt* }* @Microsoft__Quantum__Testing__QIR__TestBuiltIn__body(i64 %arg) {
entry:
  %d = sitofp i64 %arg to double
  %bi = call %BigInt* @__quantum__rt__bigint_create_i64(i64 %arg)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %BigInt* }* getelementptr ({ double, %BigInt* }, { double, %BigInt* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %BigInt* }*
  %2 = getelementptr { double, %BigInt* }, { double, %BigInt* }* %1, i64 0, i32 0
  %3 = getelementptr { double, %BigInt* }, { double, %BigInt* }* %1, i64 0, i32 1
  store double %d, double* %2
  store %BigInt* %bi, %BigInt** %3
  ret { double, %BigInt* }* %1
}
