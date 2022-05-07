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
  %16 = alloca [3 x i64], align 8
  store [3 x i64] [i64 4, i64 2, i64 3], [3 x i64]* %16, align 4
  %17 = bitcast [3 x i64]* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %17)
  %18 = alloca { { i64, double }, { i64, double } }, align 8
  store { { i64, double }, { i64, double } } { { i64, double } { i64 1, double 1.000000e+00 }, { i64, double } { i64 5, double 1.000000e+00 } }, { { i64, double }, { i64, double } }* %18, align 8
  %19 = bitcast { { i64, double }, { i64, double } }* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %19)
  %20 = alloca { i64, double }, align 8
  store { i64, double } { i64 1, double 2.000000e+00 }, { i64, double }* %20, align 8
  %21 = bitcast { i64, double }* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %21)
  %22 = alloca {}, align 8
  store {} zeroinitializer, {}* %22, align 1
  %23 = bitcast {}* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %23)
  %24 = alloca { { i64, double }, double }, align 8
  store { { i64, double }, double } { { i64, double } { i64 1, double 1.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %24, align 8
  %25 = bitcast { { i64, double }, double }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %25)
  %26 = alloca { { i64, double }, double }, align 8
  store { { i64, double }, double } { { i64, double } { i64 1, double 3.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %26, align 8
  %27 = bitcast { { i64, double }, double }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %27)
  %28 = alloca double, align 8
  store double 3.000000e+00, double* %28, align 8
  %29 = bitcast double* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %29)
  %30 = call %Qubit* @__quantum__rt__qubit_allocate()
  %31 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %30, 0
  %32 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qs = insertvalue [2 x %Qubit*] %31, %Qubit* %32, 1
  %qubit = extractvalue [2 x %Qubit*] %qs, 0
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  %control = extractvalue [2 x %Qubit*] %qs, 0
  %target = extractvalue [2 x %Qubit*] %qs, 1
  call void @__quantum__qis__cnot__body(%Qubit* %control, %Qubit* %target)
  %qubit__1 = extractvalue [2 x %Qubit*] %qs, 0
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__1)
  %qubit__2 = extractvalue [2 x %Qubit*] %qs, 1
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__2)
  %33 = load i2, i2* @PauliX, align 1
  %34 = insertvalue { i2, i64 } zeroinitializer, i2 %33, 0
  %35 = insertvalue { i2, i64 } %34, i64 0, 1
  %36 = load i2, i2* @PauliZ, align 1
  %37 = insertvalue { i2, i64 } zeroinitializer, i2 %36, 0
  %38 = insertvalue { i2, i64 } %37, i64 1, 1
  %39 = load i2, i2* @PauliY, align 1
  %40 = insertvalue { i2, i64 } zeroinitializer, i2 %39, 0
  %41 = insertvalue { i2, i64 } %40, i64 2, 1
  %42 = insertvalue [3 x { i2, i64 }] zeroinitializer, { i2, i64 } %35, 0
  %43 = insertvalue [3 x { i2, i64 }] %42, { i2, i64 } %38, 1
  %tupleArr = insertvalue [3 x { i2, i64 }] %43, { i2, i64 } %41, 2
  %44 = extractvalue [3 x { i2, i64 }] %tupleArr, 1
  %pauli = extractvalue { i2, i64 } %44, 0
  call void @__quantum__qis__logpauli__body(i2 %pauli)
  %45 = alloca i64, align 8
  store i64 2, i64* %45, align 4
  %46 = bitcast i64* %45 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %46)
  %47 = alloca [2 x i64], align 8
  store [2 x i64] [i64 1, i64 2], [2 x i64]* %47, align 4
  %48 = bitcast [2 x i64]* %47 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %30)
  call void @__quantum__rt__qubit_release(%Qubit* %32)
  ret %Result* %m1
}
