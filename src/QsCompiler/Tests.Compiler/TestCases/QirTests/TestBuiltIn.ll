define { double, %BigInt*, i64 }* @Microsoft__Quantum__Testing__QIR__TestBuiltIn__body(i64 %arg) {
entry:
  %d = sitofp i64 %arg to double
  %bi = call %BigInt* @__quantum__rt__bigint_create_i64(i64 %arg)
  %i = fptrunc double %d to i64
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %BigInt*, i64 }* getelementptr ({ double, %BigInt*, i64 }, { double, %BigInt*, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, %BigInt*, i64 }*
  %2 = getelementptr inbounds { double, %BigInt*, i64 }, { double, %BigInt*, i64 }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { double, %BigInt*, i64 }, { double, %BigInt*, i64 }* %1, i32 0, i32 1
  %4 = getelementptr inbounds { double, %BigInt*, i64 }, { double, %BigInt*, i64 }* %1, i32 0, i32 2
  store double %d, double* %2
  store %BigInt* %bi, %BigInt** %3
  store i64 %i, i64* %4
  ret { double, %BigInt*, i64 }* %1
}
