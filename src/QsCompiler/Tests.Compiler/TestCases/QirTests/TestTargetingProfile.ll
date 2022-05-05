define internal %Result* @Microsoft__Quantum__Testing__QIR__TestProfileTargeting__body(i1 %cond) {
entry:
  %0 = alloca [3 x i64], align 8
  store [3 x i64] [i64 1, i64 2, i64 3], [3 x i64]* %0, align 4
  %1 = bitcast [3 x i64]* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %1)
  %sum = alloca i64, align 8
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  %2 = alloca [3 x i64], align 8
  store [3 x i64] [i64 6, i64 6, i64 6], [3 x i64]* %2, align 4
  %3 = bitcast [3 x i64]* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %3)
  %4 = alloca i64, align 8
  store i64 1, i64* %4, align 4
  %5 = bitcast i64* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %5)
  %6 = alloca i64, align 8
  store i64 2, i64* %6, align 4
  %7 = bitcast i64* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %7)
  %8 = alloca i64, align 8
  store i64 3, i64* %8, align 4
  %9 = bitcast i64* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %9)
  %10 = alloca [6 x i64], align 8
  store [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6], [6 x i64]* %10, align 4
  %11 = bitcast [6 x i64]* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %11)
  %12 = call %Qubit* @__quantum__rt__qubit_allocate()
  %13 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %12, 0
  %14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qs = insertvalue [2 x %Qubit*] %13, %Qubit* %14, 1
  %qubit = extractvalue [2 x %Qubit*] %qs, 0
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  %control = extractvalue [2 x %Qubit*] %qs, 0
  %target = extractvalue [2 x %Qubit*] %qs, 1
  call void @__quantum__qis__cnot__body(%Qubit* %control, %Qubit* %target)
  %qubit__1 = extractvalue [2 x %Qubit*] %qs, 0
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__1)
  %qubit__2 = extractvalue [2 x %Qubit*] %qs, 1
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__2)
  %15 = load i2, i2* @PauliX, align 1
  %16 = insertvalue { i2, i64 } zeroinitializer, i2 %15, 0
  %17 = insertvalue { i2, i64 } %16, i64 0, 1
  %18 = load i2, i2* @PauliZ, align 1
  %19 = insertvalue { i2, i64 } zeroinitializer, i2 %18, 0
  %20 = insertvalue { i2, i64 } %19, i64 1, 1
  %21 = load i2, i2* @PauliY, align 1
  %22 = insertvalue { i2, i64 } zeroinitializer, i2 %21, 0
  %23 = insertvalue { i2, i64 } %22, i64 2, 1
  %24 = insertvalue [3 x { i2, i64 }] zeroinitializer, { i2, i64 } %17, 0
  %25 = insertvalue [3 x { i2, i64 }] %24, { i2, i64 } %20, 1
  %tupleArr = insertvalue [3 x { i2, i64 }] %25, { i2, i64 } %23, 2
  %26 = extractvalue [3 x { i2, i64 }] %tupleArr, 1
  %pauli = extractvalue { i2, i64 } %26, 0
  call void @__quantum__qis__logpauli__body(i2 %pauli)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %12)
  call void @__quantum__rt__qubit_release(%Qubit* %14)
  ret %Result* %m1
}
