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
  %31 = call %Qubit* @__quantum__rt__qubit_allocate()
  %32 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %30, 0
  %qs = insertvalue [2 x %Qubit*] %32, %Qubit* %31, 1
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
  %42 = insertvalue { i2, i64 } zeroinitializer, i2 %33, 0
  %43 = insertvalue { i2, i64 } %42, i64 0, 1
  %44 = insertvalue { i2, i64 } zeroinitializer, i2 %36, 0
  %45 = insertvalue { i2, i64 } %44, i64 1, 1
  %46 = insertvalue { i2, i64 } zeroinitializer, i2 %39, 0
  %47 = insertvalue { i2, i64 } %46, i64 2, 1
  %48 = insertvalue [3 x { i2, i64 }] zeroinitializer, { i2, i64 } %43, 0
  %49 = insertvalue [3 x { i2, i64 }] %48, { i2, i64 } %45, 1
  %tupleArr = insertvalue [3 x { i2, i64 }] %49, { i2, i64 } %47, 2
  %50 = extractvalue [3 x { i2, i64 }] %tupleArr, 1
  %pauli = extractvalue { i2, i64 } %50, 0
  call void @__quantum__qis__logpauli__body(i2 %pauli)
  %51 = alloca i64, align 8
  store i64 2, i64* %51, align 4
  %52 = bitcast i64* %51 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %52)
  %53 = alloca [2 x i64], align 8
  store [2 x i64] [i64 1, i64 2], [2 x i64]* %53, align 4
  %54 = bitcast [2 x i64]* %53 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %54)
  %55 = load i2, i2* @PauliX, align 1
  %56 = load i2, i2* @PauliZ, align 1
  %57 = insertvalue [2 x i2] zeroinitializer, i2 %55, 0
  %58 = insertvalue [2 x i2] %57, i2 %56, 1
  %59 = load i2, i2* @PauliY, align 1
  %60 = insertvalue [1 x i2] zeroinitializer, i2 %59, 0
  %61 = load i2, i2* @PauliI, align 1
  %62 = insertvalue [1 x i2] zeroinitializer, i2 %61, 0
  %63 = extractvalue [2 x i2] %58, 0
  %64 = extractvalue [1 x i2] %60, 0
  %65 = extractvalue [1 x i2] %62, 0
  %66 = extractvalue [2 x i2] %58, 1
  %67 = insertvalue [2 x i2] zeroinitializer, i2 %63, 0
  %68 = insertvalue [2 x i2] %67, i2 %66, 1
  %69 = insertvalue [2 x i2] zeroinitializer, i2 %64, 0
  %70 = insertvalue [2 x i2] zeroinitializer, i2 %65, 0
  %71 = insertvalue [3 x [2 x i2]] zeroinitializer, [2 x i2] %68, 0
  %72 = insertvalue [3 x [2 x i2]] %71, [2 x i2] %69, 1
  %arrArr = insertvalue [3 x [2 x i2]] %72, [2 x i2] %70, 2
  %73 = extractvalue [3 x [2 x i2]] %arrArr, 2
  %pauliI = extractvalue [2 x i2] %73, 0
  call void @__quantum__qis__logpauli__body(i2 %pauliI)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %30)
  call void @__quantum__rt__qubit_release(%Qubit* %31)
  ret %Result* %m1
}
