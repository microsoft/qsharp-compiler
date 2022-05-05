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
  %12 = alloca [3 x i64], align 8
  store [3 x i64] [i64 6, i64 6, i64 2], [3 x i64]* %12, align 4
  %13 = bitcast [3 x i64]* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %13)
  %14 = alloca [3 x i64], align 8
  store [3 x i64] [i64 2, i64 3, i64 6], [3 x i64]* %14, align 4
  %15 = bitcast [3 x i64]* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %15)
  %16 = call %Qubit* @__quantum__rt__qubit_allocate()
  %17 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %16, 0
  %18 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qs = insertvalue [2 x %Qubit*] %17, %Qubit* %18, 1
  %qubit = extractvalue [2 x %Qubit*] %qs, 0
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  %control = extractvalue [2 x %Qubit*] %qs, 0
  %target = extractvalue [2 x %Qubit*] %qs, 1
  call void @__quantum__qis__cnot__body(%Qubit* %control, %Qubit* %target)
  %qubit__1 = extractvalue [2 x %Qubit*] %qs, 0
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__1)
  %qubit__2 = extractvalue [2 x %Qubit*] %qs, 1
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__2)
  %19 = load i2, i2* @PauliX, align 1
  %20 = insertvalue { i2, i64 } zeroinitializer, i2 %19, 0
  %21 = insertvalue { i2, i64 } %20, i64 0, 1
  %22 = load i2, i2* @PauliZ, align 1
  %23 = insertvalue { i2, i64 } zeroinitializer, i2 %22, 0
  %24 = insertvalue { i2, i64 } %23, i64 1, 1
  %25 = load i2, i2* @PauliY, align 1
  %26 = insertvalue { i2, i64 } zeroinitializer, i2 %25, 0
  %27 = insertvalue { i2, i64 } %26, i64 2, 1
  %28 = insertvalue [3 x { i2, i64 }] zeroinitializer, { i2, i64 } %21, 0
  %29 = insertvalue [3 x { i2, i64 }] %28, { i2, i64 } %24, 1
  %tupleArr = insertvalue [3 x { i2, i64 }] %29, { i2, i64 } %27, 2
  %30 = extractvalue [3 x { i2, i64 }] %tupleArr, 1
  %pauli = extractvalue { i2, i64 } %30, 0
  call void @__quantum__qis__logpauli__body(i2 %pauli)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %16)
  call void @__quantum__rt__qubit_release(%Qubit* %18)
  ret %Result* %m1
}
