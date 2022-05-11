define internal %Result* @Microsoft__Quantum__Testing__QIR__TestProfileTargeting__body(i1 %cond) {
entry:
  %0 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %0, align 4
  %1 = bitcast { [3 x i64], i64 }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %1)
  %sum = alloca i64, align 8
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  %2 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 6], i64 3 }, { [3 x i64], i64 }* %2, align 4
  %3 = bitcast { [3 x i64], i64 }* %2 to i8*
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
  %10 = alloca { [6 x i64], i64 }, align 8
  store { [6 x i64], i64 } { [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6], i64 6 }, { [6 x i64], i64 }* %10, align 4
  %11 = bitcast { [6 x i64], i64 }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %11)
  %12 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 2], i64 3 }, { [3 x i64], i64 }* %12, align 4
  %13 = bitcast { [3 x i64], i64 }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %13)
  %14 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 2, i64 3, i64 6], i64 3 }, { [3 x i64], i64 }* %14, align 4
  %15 = bitcast { [3 x i64], i64 }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %15)
  %16 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 4, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %16, align 4
  %17 = bitcast { [3 x i64], i64 }* %16 to i8*
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
  %33 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %32, 0
  %34 = insertvalue { [2 x %Qubit*], i64 } %33, i64 2, 1
  %35 = extractvalue { [2 x %Qubit*], i64 } %34, 0
  %36 = extractvalue { [2 x %Qubit*], i64 } %34, 1
  %37 = insertvalue [2 x %Qubit*] %35, %Qubit* %31, 1
  %38 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %37, 0
  %qs = insertvalue { [2 x %Qubit*], i64 } %38, i64 2, 1
  %39 = extractvalue { [2 x %Qubit*], i64 } %qs, 0
  %40 = extractvalue { [2 x %Qubit*], i64 } %qs, 1
  %qubit = extractvalue [2 x %Qubit*] %39, 0
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  %41 = extractvalue { [2 x %Qubit*], i64 } %qs, 0
  %42 = extractvalue { [2 x %Qubit*], i64 } %qs, 1
  %control = extractvalue [2 x %Qubit*] %41, 0
  %43 = extractvalue { [2 x %Qubit*], i64 } %qs, 0
  %44 = extractvalue { [2 x %Qubit*], i64 } %qs, 1
  %target = extractvalue [2 x %Qubit*] %43, 1
  call void @__quantum__qis__cnot__body(%Qubit* %control, %Qubit* %target)
  %45 = extractvalue { [2 x %Qubit*], i64 } %qs, 0
  %46 = extractvalue { [2 x %Qubit*], i64 } %qs, 1
  %qubit__1 = extractvalue [2 x %Qubit*] %45, 0
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__1)
  %47 = extractvalue { [2 x %Qubit*], i64 } %qs, 0
  %48 = extractvalue { [2 x %Qubit*], i64 } %qs, 1
  %qubit__2 = extractvalue [2 x %Qubit*] %47, 1
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__2)
  %49 = alloca i64, align 8
  store i64 1, i64* %49, align 4
  %50 = bitcast i64* %49 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %50)
  %51 = alloca i64, align 8
  store i64 2, i64* %51, align 4
  %52 = bitcast i64* %51 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %52)
  %53 = alloca { [2 x i64], i64 }, align 8
  store { [2 x i64], i64 } { [2 x i64] [i64 1, i64 2], i64 2 }, { [2 x i64], i64 }* %53, align 4
  %54 = bitcast { [2 x i64], i64 }* %53 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %54)
  %55 = alloca i64, align 8
  store i64 3, i64* %55, align 4
  %56 = bitcast i64* %55 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %56)
  %57 = alloca { { [2 x i64], i64 }, { [2 x i64], i64 } }, align 8
  store { { [2 x i64], i64 }, { [2 x i64], i64 } } { { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 } }, { { [2 x i64], i64 }, { [2 x i64], i64 } }* %57, align 4
  %58 = bitcast { { [2 x i64], i64 }, { [2 x i64], i64 } }* %57 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %58)
  %59 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } { [2 x i64] [i64 2, i64 1], i64 2 }, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %59, align 4
  %60 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %59 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %60)
  %61 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %61, align 4
  %62 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %61 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %62)
  %63 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %63, align 4
  %64 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %63 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %64)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %30)
  call void @__quantum__rt__qubit_release(%Qubit* %31)
  ret %Result* %m1
}
