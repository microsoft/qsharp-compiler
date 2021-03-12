define { double, i64, %BigInt*, i64 }* @Microsoft__Quantum__Testing__QIR__TestBuiltIn__body(i64 %arg) {
entry:
  %d = sitofp i64 %arg to double
  %i = fptosi double %d to i64
  %bi = call %BigInt* @__quantum__rt__bigint_create_i64(i64 %arg)
  %t = fptosi double %d to i64
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, i64, %BigInt*, i64 }* getelementptr ({ double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { double, i64, %BigInt*, i64 }*
  %2 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %1, i32 0, i32 1
  %4 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %1, i32 0, i32 2
  %5 = getelementptr inbounds { double, i64, %BigInt*, i64 }, { double, i64, %BigInt*, i64 }* %1, i32 0, i32 3
  store double %d, double* %2, align 8
  store i64 %i, i64* %3, align 4
  store %BigInt* %bi, %BigInt** %4, align 8
  store i64 %t, i64* %5, align 4
  ret { double, i64, %BigInt*, i64 }* %1
}
