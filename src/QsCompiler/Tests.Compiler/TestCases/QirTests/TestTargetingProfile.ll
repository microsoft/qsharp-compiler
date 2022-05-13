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
  %49 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %49, align 4
  %50 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %49 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %50)
  %51 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %51, align 4
  %52 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %51 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %52)
  %53 = call %Qubit* @__quantum__rt__qubit_allocate()
  %54 = call %Qubit* @__quantum__rt__qubit_allocate()
  %55 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %53, 0
  %56 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %55, 0
  %57 = insertvalue { [2 x %Qubit*], i64 } %56, i64 2, 1
  %58 = extractvalue { [2 x %Qubit*], i64 } %57, 0
  %59 = extractvalue { [2 x %Qubit*], i64 } %57, 1
  %60 = insertvalue [2 x %Qubit*] %58, %Qubit* %54, 1
  %61 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %60, 0
  %qs1 = insertvalue { [2 x %Qubit*], i64 } %61, i64 2, 1
  %62 = call %Qubit* @__quantum__rt__qubit_allocate()
  %63 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %62, 0
  %64 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %63, 0
  %qs2 = insertvalue { [1 x %Qubit*], i64 } %64, i64 1, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %65 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %66 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %65, 0
  %67 = insertvalue { [1 x %Qubit*], i64 } %66, i64 1, 1
  %68 = extractvalue { [2 x %Qubit*], i64 } %qs1, 0
  %69 = extractvalue { [2 x %Qubit*], i64 } %qs1, 1
  %70 = extractvalue { [1 x %Qubit*], i64 } %qs2, 0
  %71 = extractvalue { [1 x %Qubit*], i64 } %qs2, 1
  %72 = extractvalue { [1 x %Qubit*], i64 } %67, 0
  %73 = extractvalue { [1 x %Qubit*], i64 } %67, 1
  %74 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %53, 0
  %75 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %74, 0
  %76 = insertvalue { [2 x %Qubit*], i64 } %75, i64 2, 1
  %77 = extractvalue { [2 x %Qubit*], i64 } %76, 0
  %78 = extractvalue { [2 x %Qubit*], i64 } %76, 1
  %79 = insertvalue [2 x %Qubit*] %77, %Qubit* %54, 1
  %80 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %79, 0
  %81 = insertvalue { [2 x %Qubit*], i64 } %80, i64 2, 1
  %82 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %62, 0
  %83 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %82, 0
  %84 = insertvalue { [2 x %Qubit*], i64 } %83, i64 1, 1
  %85 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %86 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %85, 0
  %87 = insertvalue { [2 x %Qubit*], i64 } %86, i64 1, 1
  %88 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %81, 0
  %89 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %88, 0
  %90 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %89, i64 4, 1
  %91 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %90, 0
  %92 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %90, 1
  %93 = insertvalue [4 x { [2 x %Qubit*], i64 }] %91, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %94 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %93, 0
  %95 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %94, i64 4, 1
  %96 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %95, 0
  %97 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %95, 1
  %98 = insertvalue [4 x { [2 x %Qubit*], i64 }] %96, { [2 x %Qubit*], i64 } %84, 2
  %99 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %98, 0
  %100 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %99, i64 4, 1
  %101 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %100, 0
  %102 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %100, 1
  %103 = insertvalue [4 x { [2 x %Qubit*], i64 }] %101, { [2 x %Qubit*], i64 } %87, 3
  %104 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %103, 0
  %qubitArrArr = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %104, i64 4, 1
  %105 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %106 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %107 = extractvalue { [2 x %Qubit*], i64 } %81, 0
  %108 = extractvalue { [2 x %Qubit*], i64 } %81, 1
  %109 = extractvalue { [2 x %Qubit*], i64 } %84, 0
  %110 = extractvalue { [2 x %Qubit*], i64 } %84, 1
  %111 = extractvalue { [2 x %Qubit*], i64 } %87, 0
  %112 = extractvalue { [2 x %Qubit*], i64 } %87, 1
  %113 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %53, 0
  %114 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %113, 0
  %115 = insertvalue { [2 x %Qubit*], i64 } %114, i64 2, 1
  %116 = extractvalue { [2 x %Qubit*], i64 } %115, 0
  %117 = extractvalue { [2 x %Qubit*], i64 } %115, 1
  %118 = insertvalue [2 x %Qubit*] %116, %Qubit* %54, 1
  %119 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %118, 0
  %120 = insertvalue { [2 x %Qubit*], i64 } %119, i64 2, 1
  %121 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %62, 0
  %122 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %121, 0
  %123 = insertvalue { [2 x %Qubit*], i64 } %122, i64 1, 1
  %124 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %125 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %124, 0
  %126 = insertvalue { [2 x %Qubit*], i64 } %125, i64 1, 1
  %127 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %120, 0
  %128 = insertvalue [4 x { [2 x %Qubit*], i64 }] %127, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %129 = insertvalue [4 x { [2 x %Qubit*], i64 }] %128, { [2 x %Qubit*], i64 } %123, 2
  %130 = insertvalue [4 x { [2 x %Qubit*], i64 }] %129, { [2 x %Qubit*], i64 } %126, 3
  %131 = insertvalue [4 x { [2 x %Qubit*], i64 }] %130, { [2 x %Qubit*], i64 } zeroinitializer, 0
  %132 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %131, 0
  %133 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %132, i64 4, 1
  %134 = alloca { [4 x { [2 x %Qubit*], i64 }], i64 }, align 8
  store { [4 x { [2 x %Qubit*], i64 }], i64 } %133, { [4 x { [2 x %Qubit*], i64 }], i64 }* %134, align 8
  %135 = bitcast { [4 x { [2 x %Qubit*], i64 }], i64 }* %134 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %135)
  %136 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %137 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %136, 0
  %138 = insertvalue { [3 x %Qubit*], i64 } %137, i64 3, 1
  %139 = extractvalue { [3 x %Qubit*], i64 } %138, 0
  %140 = extractvalue { [3 x %Qubit*], i64 } %138, 1
  %141 = insertvalue [3 x %Qubit*] %139, %Qubit* %q, 1
  %142 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %141, 0
  %143 = insertvalue { [3 x %Qubit*], i64 } %142, i64 3, 1
  %144 = extractvalue { [3 x %Qubit*], i64 } %143, 0
  %145 = extractvalue { [3 x %Qubit*], i64 } %143, 1
  %146 = insertvalue [3 x %Qubit*] %144, %Qubit* %q, 2
  %147 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %146, 0
  %148 = insertvalue { [3 x %Qubit*], i64 } %147, i64 3, 1
  %149 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %150 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %151 = extractvalue { [3 x %Qubit*], i64 } %148, 0
  %152 = extractvalue { [3 x %Qubit*], i64 } %148, 1
  %153 = extractvalue { [2 x %Qubit*], i64 } %81, 0
  %154 = extractvalue { [2 x %Qubit*], i64 } %81, 1
  %155 = extractvalue { [2 x %Qubit*], i64 } %84, 0
  %156 = extractvalue { [2 x %Qubit*], i64 } %84, 1
  %157 = extractvalue { [2 x %Qubit*], i64 } %87, 0
  %158 = extractvalue { [2 x %Qubit*], i64 } %87, 1
  %159 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %160 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %159, 0
  %161 = insertvalue { [3 x %Qubit*], i64 } %160, i64 3, 1
  %162 = extractvalue { [3 x %Qubit*], i64 } %161, 0
  %163 = extractvalue { [3 x %Qubit*], i64 } %161, 1
  %164 = insertvalue [3 x %Qubit*] %162, %Qubit* %q, 1
  %165 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %164, 0
  %166 = insertvalue { [3 x %Qubit*], i64 } %165, i64 3, 1
  %167 = extractvalue { [3 x %Qubit*], i64 } %166, 0
  %168 = extractvalue { [3 x %Qubit*], i64 } %166, 1
  %169 = insertvalue [3 x %Qubit*] %167, %Qubit* %q, 2
  %170 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %169, 0
  %171 = insertvalue { [3 x %Qubit*], i64 } %170, i64 3, 1
  %172 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %53, 0
  %173 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %172, 0
  %174 = insertvalue { [3 x %Qubit*], i64 } %173, i64 2, 1
  %175 = extractvalue { [3 x %Qubit*], i64 } %174, 0
  %176 = extractvalue { [3 x %Qubit*], i64 } %174, 1
  %177 = insertvalue [3 x %Qubit*] %175, %Qubit* %54, 1
  %178 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %177, 0
  %179 = insertvalue { [3 x %Qubit*], i64 } %178, i64 2, 1
  %180 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %62, 0
  %181 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %180, 0
  %182 = insertvalue { [3 x %Qubit*], i64 } %181, i64 1, 1
  %183 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %184 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %183, 0
  %185 = insertvalue { [3 x %Qubit*], i64 } %184, i64 1, 1
  %186 = insertvalue [4 x { [3 x %Qubit*], i64 }] zeroinitializer, { [3 x %Qubit*], i64 } %179, 0
  %187 = insertvalue [4 x { [3 x %Qubit*], i64 }] %186, { [3 x %Qubit*], i64 } zeroinitializer, 1
  %188 = insertvalue [4 x { [3 x %Qubit*], i64 }] %187, { [3 x %Qubit*], i64 } %182, 2
  %189 = insertvalue [4 x { [3 x %Qubit*], i64 }] %188, { [3 x %Qubit*], i64 } %185, 3
  %190 = insertvalue [4 x { [3 x %Qubit*], i64 }] %189, { [3 x %Qubit*], i64 } %171, 1
  %191 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [3 x %Qubit*], i64 }] %190, 0
  %192 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } %191, i64 4, 1
  %193 = alloca { [4 x { [3 x %Qubit*], i64 }], i64 }, align 8
  store { [4 x { [3 x %Qubit*], i64 }], i64 } %192, { [4 x { [3 x %Qubit*], i64 }], i64 }* %193, align 8
  %194 = bitcast { [4 x { [3 x %Qubit*], i64 }], i64 }* %193 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %194)
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %53)
  call void @__quantum__rt__qubit_release(%Qubit* %54)
  call void @__quantum__rt__qubit_release(%Qubit* %62)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %target)
  ret i64 6
}
