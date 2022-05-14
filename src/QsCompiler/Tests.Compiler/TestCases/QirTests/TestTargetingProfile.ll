define internal i64 @Microsoft__Quantum__Testing__QIR__TestProfileTargeting__body() {
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
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  %30 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %31 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %30, 0
  %32 = insertvalue { [2 x %Qubit*], i64 } %31, i64 2, 1
  %33 = extractvalue { [2 x %Qubit*], i64 } %32, 0
  %34 = extractvalue { [2 x %Qubit*], i64 } %32, 1
  %35 = insertvalue [2 x %Qubit*] %33, %Qubit* %target, 1
  %36 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %35, 0
  %qs = insertvalue { [2 x %Qubit*], i64 } %36, i64 2, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %37 = alloca i64, align 8
  store i64 1, i64* %37, align 4
  %38 = bitcast i64* %37 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %38)
  call void @__quantum__qis__logpauli__body(i2 -2)
  %39 = alloca i64, align 8
  store i64 2, i64* %39, align 4
  %40 = bitcast i64* %39 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %40)
  %41 = alloca { [2 x i64], i64 }, align 8
  store { [2 x i64], i64 } { [2 x i64] [i64 1, i64 2], i64 2 }, { [2 x i64], i64 }* %41, align 4
  %42 = bitcast { [2 x i64], i64 }* %41 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %42)
  %43 = alloca i64, align 8
  store i64 3, i64* %43, align 4
  %44 = bitcast i64* %43 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %44)
  %45 = alloca { { [2 x i64], i64 }, { [2 x i64], i64 } }, align 8
  store { { [2 x i64], i64 }, { [2 x i64], i64 } } { { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 } }, { { [2 x i64], i64 }, { [2 x i64], i64 } }* %45, align 4
  %46 = bitcast { { [2 x i64], i64 }, { [2 x i64], i64 } }* %45 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %46)
  %47 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } { [2 x i64] [i64 2, i64 1], i64 2 }, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %47, align 4
  %48 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %47 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  call void @__quantum__qis__logpauli__body(i2 0)
  %49 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %49, align 4
  %50 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %49 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %50)
  %51 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %51, align 4
  %52 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %51 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %52)
  %53 = alloca { [3 x { [2 x i2], i64 }], i64 }, align 8
  store { [3 x { [2 x i2], i64 }], i64 } { [3 x { [2 x i2], i64 }] [{ [2 x i2], i64 } zeroinitializer, { [2 x i2], i64 } { [2 x i2] [i2 -1, i2 0], i64 1 }, { [2 x i2], i64 } { [2 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [2 x i2], i64 }], i64 }* %53, align 4
  %54 = bitcast { [3 x { [2 x i2], i64 }], i64 }* %53 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %54)
  %55 = alloca { [3 x { [3 x i2], i64 }], i64 }, align 8
  store { [3 x { [3 x i2], i64 }], i64 } { [3 x { [3 x i2], i64 }] [{ [3 x i2], i64 } { [3 x i2] [i2 1, i2 -2, i2 0], i64 2 }, { [3 x i2], i64 } { [3 x i2] [i2 1, i2 1, i2 1], i64 3 }, { [3 x i2], i64 } { [3 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [3 x i2], i64 }], i64 }* %55, align 4
  %56 = bitcast { [3 x { [3 x i2], i64 }], i64 }* %55 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %56)
  %57 = call %Qubit* @__quantum__rt__qubit_allocate()
  %58 = call %Qubit* @__quantum__rt__qubit_allocate()
  %59 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %57, 0
  %60 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %59, 0
  %61 = insertvalue { [2 x %Qubit*], i64 } %60, i64 2, 1
  %62 = extractvalue { [2 x %Qubit*], i64 } %61, 0
  %63 = extractvalue { [2 x %Qubit*], i64 } %61, 1
  %64 = insertvalue [2 x %Qubit*] %62, %Qubit* %58, 1
  %65 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %64, 0
  %qs1 = insertvalue { [2 x %Qubit*], i64 } %65, i64 2, 1
  %66 = call %Qubit* @__quantum__rt__qubit_allocate()
  %67 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %68 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %67, 0
  %qs2 = insertvalue { [1 x %Qubit*], i64 } %68, i64 1, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %69 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %70 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %69, 0
  %71 = insertvalue { [1 x %Qubit*], i64 } %70, i64 1, 1
  %72 = extractvalue { [2 x %Qubit*], i64 } %qs1, 0
  %73 = extractvalue { [2 x %Qubit*], i64 } %qs1, 1
  %74 = extractvalue { [1 x %Qubit*], i64 } %qs2, 0
  %75 = extractvalue { [1 x %Qubit*], i64 } %qs2, 1
  %76 = extractvalue { [1 x %Qubit*], i64 } %71, 0
  %77 = extractvalue { [1 x %Qubit*], i64 } %71, 1
  %78 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %57, 0
  %79 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %78, 0
  %80 = insertvalue { [2 x %Qubit*], i64 } %79, i64 2, 1
  %81 = extractvalue { [2 x %Qubit*], i64 } %80, 0
  %82 = extractvalue { [2 x %Qubit*], i64 } %80, 1
  %83 = insertvalue [2 x %Qubit*] %81, %Qubit* %58, 1
  %84 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %83, 0
  %85 = insertvalue { [2 x %Qubit*], i64 } %84, i64 2, 1
  %86 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %87 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %86, 0
  %88 = insertvalue { [2 x %Qubit*], i64 } %87, i64 1, 1
  %89 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %90 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %89, 0
  %91 = insertvalue { [2 x %Qubit*], i64 } %90, i64 1, 1
  %92 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %85, 0
  %93 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %92, 0
  %94 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %93, i64 4, 1
  %95 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %94, 0
  %96 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %94, 1
  %97 = insertvalue [4 x { [2 x %Qubit*], i64 }] %95, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %98 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %97, 0
  %99 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %98, i64 4, 1
  %100 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %99, 0
  %101 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %99, 1
  %102 = insertvalue [4 x { [2 x %Qubit*], i64 }] %100, { [2 x %Qubit*], i64 } %88, 2
  %103 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %102, 0
  %104 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %103, i64 4, 1
  %105 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %104, 0
  %106 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %104, 1
  %107 = insertvalue [4 x { [2 x %Qubit*], i64 }] %105, { [2 x %Qubit*], i64 } %91, 3
  %108 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %107, 0
  %qubitArrArr = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %108, i64 4, 1
  %109 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %110 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %111 = extractvalue { [2 x %Qubit*], i64 } %85, 0
  %112 = extractvalue { [2 x %Qubit*], i64 } %85, 1
  %113 = extractvalue { [2 x %Qubit*], i64 } %88, 0
  %114 = extractvalue { [2 x %Qubit*], i64 } %88, 1
  %115 = extractvalue { [2 x %Qubit*], i64 } %91, 0
  %116 = extractvalue { [2 x %Qubit*], i64 } %91, 1
  %117 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %57, 0
  %118 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %117, 0
  %119 = insertvalue { [2 x %Qubit*], i64 } %118, i64 2, 1
  %120 = extractvalue { [2 x %Qubit*], i64 } %119, 0
  %121 = extractvalue { [2 x %Qubit*], i64 } %119, 1
  %122 = insertvalue [2 x %Qubit*] %120, %Qubit* %58, 1
  %123 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %122, 0
  %124 = insertvalue { [2 x %Qubit*], i64 } %123, i64 2, 1
  %125 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %126 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %125, 0
  %127 = insertvalue { [2 x %Qubit*], i64 } %126, i64 1, 1
  %128 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %129 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %128, 0
  %130 = insertvalue { [2 x %Qubit*], i64 } %129, i64 1, 1
  %131 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %124, 0
  %132 = insertvalue [4 x { [2 x %Qubit*], i64 }] %131, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %133 = insertvalue [4 x { [2 x %Qubit*], i64 }] %132, { [2 x %Qubit*], i64 } %127, 2
  %134 = insertvalue [4 x { [2 x %Qubit*], i64 }] %133, { [2 x %Qubit*], i64 } %130, 3
  %135 = insertvalue [4 x { [2 x %Qubit*], i64 }] %134, { [2 x %Qubit*], i64 } zeroinitializer, 0
  %136 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %135, 0
  %137 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %136, i64 4, 1
  %138 = alloca { [4 x { [2 x %Qubit*], i64 }], i64 }, align 8
  store { [4 x { [2 x %Qubit*], i64 }], i64 } %137, { [4 x { [2 x %Qubit*], i64 }], i64 }* %138, align 8
  %139 = bitcast { [4 x { [2 x %Qubit*], i64 }], i64 }* %138 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %139)
  %140 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %141 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %140, 0
  %142 = insertvalue { [3 x %Qubit*], i64 } %141, i64 3, 1
  %143 = extractvalue { [3 x %Qubit*], i64 } %142, 0
  %144 = extractvalue { [3 x %Qubit*], i64 } %142, 1
  %145 = insertvalue [3 x %Qubit*] %143, %Qubit* %q, 1
  %146 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %145, 0
  %147 = insertvalue { [3 x %Qubit*], i64 } %146, i64 3, 1
  %148 = extractvalue { [3 x %Qubit*], i64 } %147, 0
  %149 = extractvalue { [3 x %Qubit*], i64 } %147, 1
  %150 = insertvalue [3 x %Qubit*] %148, %Qubit* %q, 2
  %151 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %150, 0
  %152 = insertvalue { [3 x %Qubit*], i64 } %151, i64 3, 1
  %153 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %154 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %155 = extractvalue { [3 x %Qubit*], i64 } %152, 0
  %156 = extractvalue { [3 x %Qubit*], i64 } %152, 1
  %157 = extractvalue { [2 x %Qubit*], i64 } %85, 0
  %158 = extractvalue { [2 x %Qubit*], i64 } %85, 1
  %159 = extractvalue { [2 x %Qubit*], i64 } %88, 0
  %160 = extractvalue { [2 x %Qubit*], i64 } %88, 1
  %161 = extractvalue { [2 x %Qubit*], i64 } %91, 0
  %162 = extractvalue { [2 x %Qubit*], i64 } %91, 1
  %163 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %164 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %163, 0
  %165 = insertvalue { [3 x %Qubit*], i64 } %164, i64 3, 1
  %166 = extractvalue { [3 x %Qubit*], i64 } %165, 0
  %167 = extractvalue { [3 x %Qubit*], i64 } %165, 1
  %168 = insertvalue [3 x %Qubit*] %166, %Qubit* %q, 1
  %169 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %168, 0
  %170 = insertvalue { [3 x %Qubit*], i64 } %169, i64 3, 1
  %171 = extractvalue { [3 x %Qubit*], i64 } %170, 0
  %172 = extractvalue { [3 x %Qubit*], i64 } %170, 1
  %173 = insertvalue [3 x %Qubit*] %171, %Qubit* %q, 2
  %174 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %173, 0
  %175 = insertvalue { [3 x %Qubit*], i64 } %174, i64 3, 1
  %176 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %57, 0
  %177 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %176, 0
  %178 = insertvalue { [3 x %Qubit*], i64 } %177, i64 2, 1
  %179 = extractvalue { [3 x %Qubit*], i64 } %178, 0
  %180 = extractvalue { [3 x %Qubit*], i64 } %178, 1
  %181 = insertvalue [3 x %Qubit*] %179, %Qubit* %58, 1
  %182 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %181, 0
  %183 = insertvalue { [3 x %Qubit*], i64 } %182, i64 2, 1
  %184 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %185 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %184, 0
  %186 = insertvalue { [3 x %Qubit*], i64 } %185, i64 1, 1
  %187 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %188 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %187, 0
  %189 = insertvalue { [3 x %Qubit*], i64 } %188, i64 1, 1
  %190 = insertvalue [4 x { [3 x %Qubit*], i64 }] zeroinitializer, { [3 x %Qubit*], i64 } %183, 0
  %191 = insertvalue [4 x { [3 x %Qubit*], i64 }] %190, { [3 x %Qubit*], i64 } zeroinitializer, 1
  %192 = insertvalue [4 x { [3 x %Qubit*], i64 }] %191, { [3 x %Qubit*], i64 } %186, 2
  %193 = insertvalue [4 x { [3 x %Qubit*], i64 }] %192, { [3 x %Qubit*], i64 } %189, 3
  %194 = insertvalue [4 x { [3 x %Qubit*], i64 }] %193, { [3 x %Qubit*], i64 } %175, 1
  %195 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [3 x %Qubit*], i64 }] %194, 0
  %196 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } %195, i64 4, 1
  %197 = alloca { [4 x { [3 x %Qubit*], i64 }], i64 }, align 8
  store { [4 x { [3 x %Qubit*], i64 }], i64 } %196, { [4 x { [3 x %Qubit*], i64 }], i64 }* %197, align 8
  %198 = bitcast { [4 x { [3 x %Qubit*], i64 }], i64 }* %197 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %198)
  %199 = call %Result* @__quantum__rt__result_get_zero()
  %200 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %199)
  br i1 %200, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %201 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %202 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %201, 1
  %203 = alloca { i64, %Result* }, align 8
  store { i64, %Result* } %202, { i64, %Result* }* %203, align 8
  %204 = bitcast { i64, %Result* }* %203 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %204)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %205 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %206 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %205, 1
  %207 = alloca { i64, %Result* }, align 8
  store { i64, %Result* } %206, { i64, %Result* }* %207, align 8
  %208 = bitcast { i64, %Result* }* %207 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %208)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  br i1 true, label %then1__2, label %continue__2

then1__2:                                         ; preds = %continue__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %209 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %210 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %209, 1
  %211 = alloca { i64, %Result* }, align 8
  store { i64, %Result* } %210, { i64, %Result* }* %211, align 8
  %212 = bitcast { i64, %Result* }* %211 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %212)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %then1__2, %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  br i1 true, label %then0__1, label %continue__3

then0__1:                                         ; preds = %continue__2
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %213 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %214 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %213, 1
  %215 = alloca { i64, %Result* }, align 8
  store { i64, %Result* } %214, { i64, %Result* }* %215, align 8
  %216 = bitcast { i64, %Result* }* %215 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %216)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %then0__1, %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %217 = call %Result* @__quantum__rt__result_get_zero()
  %218 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %217)
  br i1 %218, label %then0__2, label %else__1

then0__2:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %219 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %220 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %219, 1
  %221 = alloca { i64, %Result* }, align 8
  store { i64, %Result* } %220, { i64, %Result* }* %221, align 8
  %222 = bitcast { i64, %Result* }* %221 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %222)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %223 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %224 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %223, 1
  %225 = alloca { i64, %Result* }, align 8
  store { i64, %Result* } %224, { i64, %Result* }* %225, align 8
  %226 = bitcast { i64, %Result* }* %225 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %226)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__2
  %rand = alloca i64, align 8
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %227 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %228 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %227, 0
  %qubits = insertvalue { [1 x %Qubit*], i64 } %228, i64 1, 1
  %229 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %229, i32 1)
  %230 = call %Result* @__quantum__rt__result_get_one()
  %231 = call i1 @__quantum__rt__result_equal(%Result* %229, %Result* %230)
  call void @__quantum__rt__result_update_reference_count(%Result* %229, i32 -1)
  br i1 %231, label %then0__3, label %continue__5

continue__6:                                      ; No predecessors!

then0__3:                                         ; preds = %continue__4
  store i64 1, i64* %rand, align 4
  br label %continue__5

continue__5:                                      ; preds = %then0__3, %continue__4
  %232 = load i64, i64* %rand, align 4
  %233 = shl i64 %232, 1
  store i64 %233, i64* %rand, align 4
  %234 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %235 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %234, 0
  %qubits__1 = insertvalue { [1 x %Qubit*], i64 } %235, i64 1, 1
  %236 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %236, i32 1)
  %237 = call %Result* @__quantum__rt__result_get_one()
  %238 = call i1 @__quantum__rt__result_equal(%Result* %236, %Result* %237)
  call void @__quantum__rt__result_update_reference_count(%Result* %236, i32 -1)
  br i1 %238, label %then0__4, label %continue__7

continue__8:                                      ; No predecessors!

then0__4:                                         ; preds = %continue__5
  %239 = add i64 %233, 1
  store i64 %239, i64* %rand, align 4
  br label %continue__7

continue__7:                                      ; preds = %then0__4, %continue__5
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %57)
  call void @__quantum__rt__qubit_release(%Qubit* %58)
  call void @__quantum__rt__qubit_release(%Qubit* %66)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %target)
  ret i64 6
}
