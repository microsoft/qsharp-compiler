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
  %10 = call %Qubit* @__quantum__rt__qubit_allocate()
  %11 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %10, 0
  %12 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qs = insertvalue [2 x %Qubit*] %11, %Qubit* %12, 1
  %qubit = extractvalue [2 x %Qubit*] %qs, 0
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  %control = extractvalue [2 x %Qubit*] %qs, 0
  %target = extractvalue [2 x %Qubit*] %qs, 1
  call void @__quantum__qis__cnot__body(%Qubit* %control, %Qubit* %target)
  %qubit__1 = extractvalue [2 x %Qubit*] %qs, 0
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__1)
  %qubit__2 = extractvalue [2 x %Qubit*] %qs, 1
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__2)
  %13 = load i2, i2* @PauliX, align 1
  %14 = insertvalue { i2, i64 } zeroinitializer, i2 %13, 0
  %15 = insertvalue { i2, i64 } %14, i64 0, 1
  %16 = load i2, i2* @PauliZ, align 1
  %17 = insertvalue { i2, i64 } zeroinitializer, i2 %16, 0
  %18 = insertvalue { i2, i64 } %17, i64 1, 1
  %19 = load i2, i2* @PauliY, align 1
  %20 = insertvalue { i2, i64 } zeroinitializer, i2 %19, 0
  %21 = insertvalue { i2, i64 } %20, i64 2, 1
  %22 = insertvalue [3 x { i2, i64 }*] zeroinitializer, { i2, i64 } %15, 0
  %23 = insertvalue [3 x { i2, i64 }*] %22, { i2, i64 } %18, 1
  %tupleArr = insertvalue [3 x { i2, i64 }*] %23, { i2, i64 } %21, 2
  %24 = extractvalue [3 x { i2, i64 }*] %tupleArr, 1
  %25 = getelementptr inbounds { i2, i64 }, { i2, i64 }* %24, i32 0, i32 0
  %pauli = load i2, i2* %25, align 1
  call void @__quantum__qis__logpauli__body(i2 %pauli)
  %26 = load i2, i2* @PauliX, align 1
  %27 = load i2, i2* @PauliZ, align 1
  %28 = insertvalue [2 x i2] zeroinitializer, i2 %26, 0
  %29 = insertvalue [2 x i2] %28, i2 %27, 1
  %30 = load i2, i2* @PauliY, align 1
  %31 = insertvalue [1 x i2] zeroinitializer, i2 %30, 0
  %32 = load i2, i2* @PauliI, align 1
  %33 = insertvalue [1 x i2] zeroinitializer, i2 %32, 0
  %34 = insertvalue [3 x %Array*] zeroinitializer, [2 x i2] %29, 0
  %35 = insertvalue [3 x %Array*] %34, [1 x i2] %31, 1
  %arrArr = insertvalue [3 x %Array*] %35, [1 x i2] %33, 2
  %36 = extractvalue [3 x %Array*] %arrArr, 2
  %37 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %36, i64 0)
  %38 = bitcast i8* %37 to i2*
  %pauliI = load i2, i2* %38, align 1
  call void @__quantum__qis__logpauli__body(i2 %pauliI)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %10)
  call void @__quantum__rt__qubit_release(%Qubit* %12)
  ret %Result* %m1
}
