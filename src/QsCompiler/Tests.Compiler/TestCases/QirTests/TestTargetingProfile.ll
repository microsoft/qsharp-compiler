%Result = type opaque
%Qubit = type opaque
%Array = type opaque

define void @Microsoft__Quantum__Testing__QIR__TestProfileTargeting() #0 {
entry:
  %bar__1 = alloca i64, align 8
  %bar = alloca i64, align 8
  %foo = alloca i64, align 8
  %0 = alloca { i1, i1 }, align 8
  %rand = alloca i64, align 8
  %1 = alloca { i64, %Result* }, align 8
  %2 = alloca { i64, %Result* }, align 8
  %3 = alloca { i64, %Result* }, align 8
  %4 = alloca { i64, %Result* }, align 8
  %5 = alloca { i64, %Result* }, align 8
  %6 = alloca { i64, %Result* }, align 8
  %7 = alloca { i64, %Result* }, align 8
  %8 = alloca { i64, %Result* }, align 8
  %9 = alloca { i64, %Result* }, align 8
  %10 = alloca { i64, %Result* }, align 8
  %11 = alloca { i64, %Result* }, align 8
  %12 = alloca { i64, %Result* }, align 8
  %13 = alloca { i64, %Result* }, align 8
  %14 = alloca { i64, %Result* }, align 8
  %15 = alloca { i64, %Result* }, align 8
  %16 = alloca { i64, %Result* }, align 8
  %17 = alloca { i64, %Result* }, align 8
  %18 = alloca { i64, %Result* }, align 8
  %19 = alloca { i64, %Result* }, align 8
  %20 = alloca { [4 x { [3 x %Qubit*], i64 }], i64 }, align 8
  %21 = alloca { [4 x { [2 x %Qubit*], i64 }], i64 }, align 8
  %22 = alloca { [3 x { [3 x i2], i64 }], i64 }, align 8
  %23 = alloca { [3 x { [2 x i2], i64 }], i64 }, align 8
  %24 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  %25 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %26 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %27 = alloca { { [2 x i64], i64 }, { [2 x i64], i64 } }, align 8
  %28 = alloca i64, align 8
  %29 = alloca { [2 x i64], i64 }, align 8
  %30 = alloca i64, align 8
  %31 = alloca i64, align 8
  %32 = alloca double, align 8
  %33 = alloca { { i64, double }, double }, align 8
  %34 = alloca { { i64, double }, double }, align 8
  %35 = alloca {}, align 8
  %36 = alloca { i64, double }, align 8
  %37 = alloca { { i64, double }, { i64, double } }, align 8
  %38 = alloca { [3 x i64], i64 }, align 8
  %39 = alloca { [3 x i64], i64 }, align 8
  %40 = alloca { [3 x i64], i64 }, align 8
  %41 = alloca { [6 x i64], i64 }, align 8
  %42 = alloca i64, align 8
  %43 = alloca i64, align 8
  %44 = alloca i64, align 8
  %45 = alloca { [3 x i64], i64 }, align 8
  %sum = alloca i64, align 8
  %46 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %46, align 4
  %47 = bitcast { [3 x i64], i64 }* %46 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %47)
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 6], i64 3 }, { [3 x i64], i64 }* %45, align 4
  %48 = bitcast { [3 x i64], i64 }* %45 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  store i64 1, i64* %44, align 4
  %49 = bitcast i64* %44 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %49)
  store i64 2, i64* %43, align 4
  %50 = bitcast i64* %43 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %50)
  store i64 3, i64* %42, align 4
  %51 = bitcast i64* %42 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %51)
  store { [6 x i64], i64 } { [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6], i64 6 }, { [6 x i64], i64 }* %41, align 4
  %52 = bitcast { [6 x i64], i64 }* %41 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %52)
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 2], i64 3 }, { [3 x i64], i64 }* %40, align 4
  %53 = bitcast { [3 x i64], i64 }* %40 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %53)
  store { [3 x i64], i64 } { [3 x i64] [i64 2, i64 3, i64 6], i64 3 }, { [3 x i64], i64 }* %39, align 4
  %54 = bitcast { [3 x i64], i64 }* %39 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %54)
  store { [3 x i64], i64 } { [3 x i64] [i64 4, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %38, align 4
  %55 = bitcast { [3 x i64], i64 }* %38 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %55)
  store { { i64, double }, { i64, double } } { { i64, double } { i64 1, double 1.000000e+00 }, { i64, double } { i64 5, double 1.000000e+00 } }, { { i64, double }, { i64, double } }* %37, align 8
  %56 = bitcast { { i64, double }, { i64, double } }* %37 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %56)
  store { i64, double } { i64 1, double 2.000000e+00 }, { i64, double }* %36, align 8
  %57 = bitcast { i64, double }* %36 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %57)
  store {} zeroinitializer, {}* %35, align 1
  %58 = bitcast {}* %35 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %58)
  store { { i64, double }, double } { { i64, double } { i64 1, double 1.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %34, align 8
  %59 = bitcast { { i64, double }, double }* %34 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %59)
  store { { i64, double }, double } { { i64, double } { i64 1, double 3.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %33, align 8
  %60 = bitcast { { i64, double }, double }* %33 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %60)
  store double 3.000000e+00, double* %32, align 8
  %61 = bitcast double* %32 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %61)
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  %62 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %63 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %62, 0
  %64 = insertvalue { [2 x %Qubit*], i64 } %63, i64 2, 1
  %65 = extractvalue { [2 x %Qubit*], i64 } %64, 0
  %66 = extractvalue { [2 x %Qubit*], i64 } %64, 1
  %67 = insertvalue [2 x %Qubit*] %65, %Qubit* %target, 1
  %68 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %67, 0
  %qs = insertvalue { [2 x %Qubit*], i64 } %68, i64 2, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  store i64 1, i64* %31, align 4
  %69 = bitcast i64* %31 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %69)
  call void @__quantum__qis__logpauli__body(i2 -2)
  store i64 2, i64* %30, align 4
  %70 = bitcast i64* %30 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %70)
  store { [2 x i64], i64 } { [2 x i64] [i64 1, i64 2], i64 2 }, { [2 x i64], i64 }* %29, align 4
  %71 = bitcast { [2 x i64], i64 }* %29 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %71)
  store i64 3, i64* %28, align 4
  %72 = bitcast i64* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %72)
  store { { [2 x i64], i64 }, { [2 x i64], i64 } } { { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 } }, { { [2 x i64], i64 }, { [2 x i64], i64 } }* %27, align 4
  %73 = bitcast { { [2 x i64], i64 }, { [2 x i64], i64 } }* %27 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %73)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } { [2 x i64] [i64 2, i64 1], i64 2 }, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %26, align 4
  %74 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %74)
  call void @__quantum__qis__logpauli__body(i2 0)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %25, align 4
  %75 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %25 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %75)
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %24, align 4
  %76 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %76)
  store { [3 x { [2 x i2], i64 }], i64 } { [3 x { [2 x i2], i64 }] [{ [2 x i2], i64 } zeroinitializer, { [2 x i2], i64 } { [2 x i2] [i2 -1, i2 0], i64 1 }, { [2 x i2], i64 } { [2 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [2 x i2], i64 }], i64 }* %23, align 4
  %77 = bitcast { [3 x { [2 x i2], i64 }], i64 }* %23 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %77)
  store { [3 x { [3 x i2], i64 }], i64 } { [3 x { [3 x i2], i64 }] [{ [3 x i2], i64 } { [3 x i2] [i2 1, i2 -2, i2 0], i64 2 }, { [3 x i2], i64 } { [3 x i2] [i2 1, i2 1, i2 1], i64 3 }, { [3 x i2], i64 } { [3 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [3 x i2], i64 }], i64 }* %22, align 4
  %78 = bitcast { [3 x { [3 x i2], i64 }], i64 }* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %78)
  %79 = call %Qubit* @__quantum__rt__qubit_allocate()
  %80 = call %Qubit* @__quantum__rt__qubit_allocate()
  %81 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %79, 0
  %82 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %81, 0
  %83 = insertvalue { [2 x %Qubit*], i64 } %82, i64 2, 1
  %84 = extractvalue { [2 x %Qubit*], i64 } %83, 0
  %85 = extractvalue { [2 x %Qubit*], i64 } %83, 1
  %86 = insertvalue [2 x %Qubit*] %84, %Qubit* %80, 1
  %87 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %86, 0
  %qs1 = insertvalue { [2 x %Qubit*], i64 } %87, i64 2, 1
  %88 = call %Qubit* @__quantum__rt__qubit_allocate()
  %89 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %88, 0
  %90 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %89, 0
  %qs2 = insertvalue { [1 x %Qubit*], i64 } %90, i64 1, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %91 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %92 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %91, 0
  %93 = insertvalue { [1 x %Qubit*], i64 } %92, i64 1, 1
  %94 = extractvalue { [2 x %Qubit*], i64 } %qs1, 0
  %95 = extractvalue { [2 x %Qubit*], i64 } %qs1, 1
  %96 = extractvalue { [1 x %Qubit*], i64 } %qs2, 0
  %97 = extractvalue { [1 x %Qubit*], i64 } %qs2, 1
  %98 = extractvalue { [1 x %Qubit*], i64 } %93, 0
  %99 = extractvalue { [1 x %Qubit*], i64 } %93, 1
  %100 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %79, 0
  %101 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %100, 0
  %102 = insertvalue { [2 x %Qubit*], i64 } %101, i64 2, 1
  %103 = extractvalue { [2 x %Qubit*], i64 } %102, 0
  %104 = extractvalue { [2 x %Qubit*], i64 } %102, 1
  %105 = insertvalue [2 x %Qubit*] %103, %Qubit* %80, 1
  %106 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %105, 0
  %107 = insertvalue { [2 x %Qubit*], i64 } %106, i64 2, 1
  %108 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %88, 0
  %109 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %108, 0
  %110 = insertvalue { [2 x %Qubit*], i64 } %109, i64 1, 1
  %111 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %112 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %111, 0
  %113 = insertvalue { [2 x %Qubit*], i64 } %112, i64 1, 1
  %114 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %107, 0
  %115 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %114, 0
  %116 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %115, i64 4, 1
  %117 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %116, 0
  %118 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %116, 1
  %119 = insertvalue [4 x { [2 x %Qubit*], i64 }] %117, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %120 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %119, 0
  %121 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %120, i64 4, 1
  %122 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %121, 0
  %123 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %121, 1
  %124 = insertvalue [4 x { [2 x %Qubit*], i64 }] %122, { [2 x %Qubit*], i64 } %110, 2
  %125 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %124, 0
  %126 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %125, i64 4, 1
  %127 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %126, 0
  %128 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %126, 1
  %129 = insertvalue [4 x { [2 x %Qubit*], i64 }] %127, { [2 x %Qubit*], i64 } %113, 3
  %130 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %129, 0
  %qubitArrArr = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %130, i64 4, 1
  %131 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %132 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %133 = extractvalue { [2 x %Qubit*], i64 } %107, 0
  %134 = extractvalue { [2 x %Qubit*], i64 } %107, 1
  %135 = extractvalue { [2 x %Qubit*], i64 } %110, 0
  %136 = extractvalue { [2 x %Qubit*], i64 } %110, 1
  %137 = extractvalue { [2 x %Qubit*], i64 } %113, 0
  %138 = extractvalue { [2 x %Qubit*], i64 } %113, 1
  %139 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %79, 0
  %140 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %139, 0
  %141 = insertvalue { [2 x %Qubit*], i64 } %140, i64 2, 1
  %142 = extractvalue { [2 x %Qubit*], i64 } %141, 0
  %143 = extractvalue { [2 x %Qubit*], i64 } %141, 1
  %144 = insertvalue [2 x %Qubit*] %142, %Qubit* %80, 1
  %145 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %144, 0
  %146 = insertvalue { [2 x %Qubit*], i64 } %145, i64 2, 1
  %147 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %88, 0
  %148 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %147, 0
  %149 = insertvalue { [2 x %Qubit*], i64 } %148, i64 1, 1
  %150 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %151 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %150, 0
  %152 = insertvalue { [2 x %Qubit*], i64 } %151, i64 1, 1
  %153 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %146, 0
  %154 = insertvalue [4 x { [2 x %Qubit*], i64 }] %153, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %155 = insertvalue [4 x { [2 x %Qubit*], i64 }] %154, { [2 x %Qubit*], i64 } %149, 2
  %156 = insertvalue [4 x { [2 x %Qubit*], i64 }] %155, { [2 x %Qubit*], i64 } %152, 3
  %157 = insertvalue [4 x { [2 x %Qubit*], i64 }] %156, { [2 x %Qubit*], i64 } zeroinitializer, 0
  %158 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %157, 0
  %159 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %158, i64 4, 1
  store { [4 x { [2 x %Qubit*], i64 }], i64 } %159, { [4 x { [2 x %Qubit*], i64 }], i64 }* %21, align 8
  %160 = bitcast { [4 x { [2 x %Qubit*], i64 }], i64 }* %21 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %160)
  %161 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %162 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %161, 0
  %163 = insertvalue { [3 x %Qubit*], i64 } %162, i64 3, 1
  %164 = extractvalue { [3 x %Qubit*], i64 } %163, 0
  %165 = extractvalue { [3 x %Qubit*], i64 } %163, 1
  %166 = insertvalue [3 x %Qubit*] %164, %Qubit* %q, 1
  %167 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %166, 0
  %168 = insertvalue { [3 x %Qubit*], i64 } %167, i64 3, 1
  %169 = extractvalue { [3 x %Qubit*], i64 } %168, 0
  %170 = extractvalue { [3 x %Qubit*], i64 } %168, 1
  %171 = insertvalue [3 x %Qubit*] %169, %Qubit* %q, 2
  %172 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %171, 0
  %173 = insertvalue { [3 x %Qubit*], i64 } %172, i64 3, 1
  %174 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %175 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %176 = extractvalue { [3 x %Qubit*], i64 } %173, 0
  %177 = extractvalue { [3 x %Qubit*], i64 } %173, 1
  %178 = extractvalue { [2 x %Qubit*], i64 } %107, 0
  %179 = extractvalue { [2 x %Qubit*], i64 } %107, 1
  %180 = extractvalue { [2 x %Qubit*], i64 } %110, 0
  %181 = extractvalue { [2 x %Qubit*], i64 } %110, 1
  %182 = extractvalue { [2 x %Qubit*], i64 } %113, 0
  %183 = extractvalue { [2 x %Qubit*], i64 } %113, 1
  %184 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %185 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %184, 0
  %186 = insertvalue { [3 x %Qubit*], i64 } %185, i64 3, 1
  %187 = extractvalue { [3 x %Qubit*], i64 } %186, 0
  %188 = extractvalue { [3 x %Qubit*], i64 } %186, 1
  %189 = insertvalue [3 x %Qubit*] %187, %Qubit* %q, 1
  %190 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %189, 0
  %191 = insertvalue { [3 x %Qubit*], i64 } %190, i64 3, 1
  %192 = extractvalue { [3 x %Qubit*], i64 } %191, 0
  %193 = extractvalue { [3 x %Qubit*], i64 } %191, 1
  %194 = insertvalue [3 x %Qubit*] %192, %Qubit* %q, 2
  %195 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %194, 0
  %196 = insertvalue { [3 x %Qubit*], i64 } %195, i64 3, 1
  %197 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %79, 0
  %198 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %197, 0
  %199 = insertvalue { [3 x %Qubit*], i64 } %198, i64 2, 1
  %200 = extractvalue { [3 x %Qubit*], i64 } %199, 0
  %201 = extractvalue { [3 x %Qubit*], i64 } %199, 1
  %202 = insertvalue [3 x %Qubit*] %200, %Qubit* %80, 1
  %203 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %202, 0
  %204 = insertvalue { [3 x %Qubit*], i64 } %203, i64 2, 1
  %205 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %88, 0
  %206 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %205, 0
  %207 = insertvalue { [3 x %Qubit*], i64 } %206, i64 1, 1
  %208 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %209 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %208, 0
  %210 = insertvalue { [3 x %Qubit*], i64 } %209, i64 1, 1
  %211 = insertvalue [4 x { [3 x %Qubit*], i64 }] zeroinitializer, { [3 x %Qubit*], i64 } %204, 0
  %212 = insertvalue [4 x { [3 x %Qubit*], i64 }] %211, { [3 x %Qubit*], i64 } zeroinitializer, 1
  %213 = insertvalue [4 x { [3 x %Qubit*], i64 }] %212, { [3 x %Qubit*], i64 } %207, 2
  %214 = insertvalue [4 x { [3 x %Qubit*], i64 }] %213, { [3 x %Qubit*], i64 } %210, 3
  %215 = insertvalue [4 x { [3 x %Qubit*], i64 }] %214, { [3 x %Qubit*], i64 } %196, 1
  %216 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [3 x %Qubit*], i64 }] %215, 0
  %217 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } %216, i64 4, 1
  store { [4 x { [3 x %Qubit*], i64 }], i64 } %217, { [4 x { [3 x %Qubit*], i64 }], i64 }* %20, align 8
  %218 = bitcast { [4 x { [3 x %Qubit*], i64 }], i64 }* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %218)
  %219 = call %Result* @__quantum__rt__result_get_zero()
  %220 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %219)
  br i1 %220, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %221 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %222 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %221, 1
  store { i64, %Result* } %222, { i64, %Result* }* %19, align 8
  %223 = bitcast { i64, %Result* }* %19 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %223)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %224 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %225 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %224, 1
  store { i64, %Result* } %225, { i64, %Result* }* %18, align 8
  %226 = bitcast { i64, %Result* }* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %226)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %227 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %228 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %227, 1
  store { i64, %Result* } %228, { i64, %Result* }* %17, align 8
  %229 = bitcast { i64, %Result* }* %17 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %229)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %230 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %231 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %230, 1
  store { i64, %Result* } %231, { i64, %Result* }* %16, align 8
  %232 = bitcast { i64, %Result* }* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %232)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %233 = call %Result* @__quantum__rt__result_get_zero()
  %234 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %233)
  br i1 %234, label %then0__1, label %else__1

then0__1:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %235 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %236 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %235, 1
  store { i64, %Result* } %236, { i64, %Result* }* %15, align 8
  %237 = bitcast { i64, %Result* }* %15 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %237)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %238 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %239 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %238, 1
  store { i64, %Result* } %239, { i64, %Result* }* %14, align 8
  %240 = bitcast { i64, %Result* }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %240)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__1
  %241 = call %Result* @__quantum__rt__result_get_zero()
  %242 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %241)
  br i1 %242, label %then0__2, label %test1__1

then0__2:                                         ; preds = %continue__4
  %q__7 = call %Qubit* @__quantum__rt__qubit_allocate()
  %243 = call %Result* @__quantum__qis__m__body(%Qubit* %q__7)
  %244 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %243, 1
  store { i64, %Result* } %244, { i64, %Result* }* %13, align 8
  %245 = bitcast { i64, %Result* }* %13 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %245)
  call void @__quantum__rt__qubit_release(%Qubit* %q__7)
  br label %continue__5

test1__1:                                         ; preds = %continue__4
  br label %continue__5

continue__5:                                      ; preds = %test1__1, %then0__2
  %246 = call %Result* @__quantum__rt__result_get_zero()
  %247 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %246)
  br i1 %247, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %continue__5
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %continue__5
  br label %continue__6

continue__6:                                      ; preds = %condContinue__1
  %248 = call %Result* @__quantum__rt__result_get_zero()
  %249 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %248)
  br i1 %249, label %condContinue__2, label %condFalse__1

condFalse__1:                                     ; preds = %continue__6
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__1, %continue__6
  br i1 %249, label %then0__3, label %continue__7

then0__3:                                         ; preds = %condContinue__2
  %q__8 = call %Qubit* @__quantum__rt__qubit_allocate()
  %250 = call %Result* @__quantum__qis__m__body(%Qubit* %q__8)
  %251 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %250, 1
  store { i64, %Result* } %251, { i64, %Result* }* %12, align 8
  %252 = bitcast { i64, %Result* }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %252)
  call void @__quantum__rt__qubit_release(%Qubit* %q__8)
  br label %continue__7

continue__7:                                      ; preds = %then0__3, %condContinue__2
  %253 = call %Result* @__quantum__rt__result_get_zero()
  %254 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %253)
  br i1 %254, label %then0__4, label %test1__2

then0__4:                                         ; preds = %continue__7
  %q__9 = call %Qubit* @__quantum__rt__qubit_allocate()
  %255 = call %Result* @__quantum__qis__m__body(%Qubit* %q__9)
  %256 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %255, 1
  store { i64, %Result* } %256, { i64, %Result* }* %11, align 8
  %257 = bitcast { i64, %Result* }* %11 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %257)
  call void @__quantum__rt__qubit_release(%Qubit* %q__9)
  br label %continue__8

test1__2:                                         ; preds = %continue__7
  %q__10 = call %Qubit* @__quantum__rt__qubit_allocate()
  %258 = call %Result* @__quantum__qis__m__body(%Qubit* %q__10)
  %259 = insertvalue { i64, %Result* } { i64 15, %Result* null }, %Result* %258, 1
  store { i64, %Result* } %259, { i64, %Result* }* %10, align 8
  %260 = bitcast { i64, %Result* }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %260)
  call void @__quantum__rt__qubit_release(%Qubit* %q__10)
  br label %continue__8

continue__8:                                      ; preds = %test1__2, %then0__4
  %261 = call %Result* @__quantum__rt__result_get_zero()
  %262 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %261)
  br i1 %262, label %condTrue__2, label %condContinue__3

condTrue__2:                                      ; preds = %continue__8
  br label %condContinue__3

condContinue__3:                                  ; preds = %condTrue__2, %continue__8
  br i1 %262, label %then0__5, label %continue__9

then0__5:                                         ; preds = %condContinue__3
  %q__11 = call %Qubit* @__quantum__rt__qubit_allocate()
  %263 = call %Result* @__quantum__qis__m__body(%Qubit* %q__11)
  %264 = insertvalue { i64, %Result* } { i64 16, %Result* null }, %Result* %263, 1
  store { i64, %Result* } %264, { i64, %Result* }* %9, align 8
  %265 = bitcast { i64, %Result* }* %9 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %265)
  call void @__quantum__rt__qubit_release(%Qubit* %q__11)
  br label %continue__9

continue__9:                                      ; preds = %then0__5, %condContinue__3
  %266 = call %Result* @__quantum__rt__result_get_zero()
  %267 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %266)
  br i1 %267, label %condContinue__4, label %condFalse__2

condFalse__2:                                     ; preds = %continue__9
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__2, %continue__9
  %q__12 = call %Qubit* @__quantum__rt__qubit_allocate()
  %268 = call %Result* @__quantum__qis__m__body(%Qubit* %q__12)
  %269 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %268, 1
  store { i64, %Result* } %269, { i64, %Result* }* %8, align 8
  %270 = bitcast { i64, %Result* }* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %270)
  call void @__quantum__rt__qubit_release(%Qubit* %q__12)
  br label %continue__10

continue__10:                                     ; preds = %condContinue__4
  %q__13 = call %Qubit* @__quantum__rt__qubit_allocate()
  %271 = call %Result* @__quantum__qis__m__body(%Qubit* %q__13)
  %272 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %271, 1
  store { i64, %Result* } %272, { i64, %Result* }* %7, align 8
  %273 = bitcast { i64, %Result* }* %7 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %273)
  call void @__quantum__rt__qubit_release(%Qubit* %q__13)
  br label %continue__11

continue__11:                                     ; preds = %continue__10
  br label %continue__12

continue__12:                                     ; preds = %continue__11
  %q__14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %274 = call %Result* @__quantum__qis__m__body(%Qubit* %q__14)
  %275 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %274, 1
  store { i64, %Result* } %275, { i64, %Result* }* %6, align 8
  %276 = bitcast { i64, %Result* }* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %276)
  call void @__quantum__rt__qubit_release(%Qubit* %q__14)
  br label %continue__13

continue__13:                                     ; preds = %continue__12
  %q__15 = call %Qubit* @__quantum__rt__qubit_allocate()
  %277 = call %Result* @__quantum__qis__m__body(%Qubit* %q__15)
  %278 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %277, 1
  store { i64, %Result* } %278, { i64, %Result* }* %5, align 8
  %279 = bitcast { i64, %Result* }* %5 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %279)
  call void @__quantum__rt__qubit_release(%Qubit* %q__15)
  br label %continue__14

continue__14:                                     ; preds = %continue__13
  %q__16 = call %Qubit* @__quantum__rt__qubit_allocate()
  %280 = call %Result* @__quantum__qis__m__body(%Qubit* %q__16)
  %281 = insertvalue { i64, %Result* } { i64 20, %Result* null }, %Result* %280, 1
  store { i64, %Result* } %281, { i64, %Result* }* %4, align 8
  %282 = bitcast { i64, %Result* }* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %282)
  call void @__quantum__rt__qubit_release(%Qubit* %q__16)
  br label %continue__15

continue__15:                                     ; preds = %continue__14
  %q__17 = call %Qubit* @__quantum__rt__qubit_allocate()
  %283 = call %Result* @__quantum__qis__m__body(%Qubit* %q__17)
  %284 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %283, 1
  store { i64, %Result* } %284, { i64, %Result* }* %3, align 8
  %285 = bitcast { i64, %Result* }* %3 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %285)
  call void @__quantum__rt__qubit_release(%Qubit* %q__17)
  br label %continue__16

continue__16:                                     ; preds = %continue__15
  br label %continue__17

continue__17:                                     ; preds = %continue__16
  br label %continue__18

continue__18:                                     ; preds = %continue__17
  br label %continue__19

continue__19:                                     ; preds = %continue__18
  %q__18 = call %Qubit* @__quantum__rt__qubit_allocate()
  %286 = call %Result* @__quantum__qis__m__body(%Qubit* %q__18)
  %287 = insertvalue { i64, %Result* } { i64 19, %Result* null }, %Result* %286, 1
  store { i64, %Result* } %287, { i64, %Result* }* %2, align 8
  %288 = bitcast { i64, %Result* }* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %288)
  call void @__quantum__rt__qubit_release(%Qubit* %q__18)
  br label %continue__20

continue__20:                                     ; preds = %continue__19
  br label %continue__21

continue__21:                                     ; preds = %continue__20
  %q__19 = call %Qubit* @__quantum__rt__qubit_allocate()
  %289 = call %Result* @__quantum__qis__m__body(%Qubit* %q__19)
  %290 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %289, 1
  store { i64, %Result* } %290, { i64, %Result* }* %1, align 8
  %291 = bitcast { i64, %Result* }* %1 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %291)
  call void @__quantum__rt__qubit_release(%Qubit* %q__19)
  br label %continue__22

continue__22:                                     ; preds = %continue__21
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %292 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %293 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %292, 0
  %qubits = insertvalue { [1 x %Qubit*], i64 } %293, i64 1, 1
  br label %continue__24

continue__24:                                     ; preds = %continue__22
  %294 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %294, i32 1)
  %295 = call %Result* @__quantum__rt__result_get_one()
  %296 = call i1 @__quantum__rt__result_equal(%Result* %294, %Result* %295)
  call void @__quantum__rt__result_update_reference_count(%Result* %294, i32 -1)
  br i1 %296, label %then0__6, label %continue__23

then0__6:                                         ; preds = %continue__24
  store i64 1, i64* %rand, align 4
  br label %continue__23

continue__23:                                     ; preds = %then0__6, %continue__24
  %297 = load i64, i64* %rand, align 4
  %298 = shl i64 %297, 1
  store i64 %298, i64* %rand, align 4
  %299 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %300 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %299, 0
  %qubits__1 = insertvalue { [1 x %Qubit*], i64 } %300, i64 1, 1
  br label %continue__26

continue__26:                                     ; preds = %continue__23
  %301 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %301, i32 1)
  %302 = call %Result* @__quantum__rt__result_get_one()
  %303 = call i1 @__quantum__rt__result_equal(%Result* %301, %Result* %302)
  call void @__quantum__rt__result_update_reference_count(%Result* %301, i32 -1)
  br i1 %303, label %then0__7, label %continue__25

then0__7:                                         ; preds = %continue__26
  %304 = add i64 %298, 1
  store i64 %304, i64* %rand, align 4
  br label %continue__25

continue__25:                                     ; preds = %then0__7, %continue__26
  %305 = call %Result* @__quantum__rt__result_get_zero()
  %a = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %305)
  %a__1 = call %Result* @__quantum__rt__result_get_zero()
  %306 = call %Result* @__quantum__rt__result_get_one()
  %307 = call i1 @__quantum__rt__result_equal(%Result* %a__1, %Result* %306)
  %c = or i1 %307, %a
  %308 = insertvalue { i1, i1 } zeroinitializer, i1 %a, 0
  %309 = insertvalue { i1, i1 } %308, i1 %c, 1
  store { i1, i1 } %309, { i1, i1 }* %0, align 1
  %310 = bitcast { i1, i1 }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %310)
  %311 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %m2)
  %312 = select i1 %311, i64 6, i64 0
  store i64 %312, i64* %foo, align 4
  %313 = call %Result* @__quantum__rt__result_get_zero()
  %314 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %313)
  br i1 %314, label %then0__8, label %else__2

then0__8:                                         ; preds = %continue__25
  store i64 0, i64* %bar, align 4
  %315 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %316 = call %Result* @__quantum__rt__result_get_one()
  %317 = call i1 @__quantum__rt__result_equal(%Result* %315, %Result* %316)
  %318 = select i1 %317, i64 1, i64 0
  %319 = add i64 0, %318
  store i64 %319, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %315, i32 -1)
  %320 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %321 = call %Result* @__quantum__rt__result_get_one()
  %322 = call i1 @__quantum__rt__result_equal(%Result* %320, %Result* %321)
  %323 = select i1 %322, i64 1, i64 0
  %324 = add i64 %319, %323
  store i64 %324, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %320, i32 -1)
  store i64 %324, i64* %foo, align 4
  br label %continue__27

else__2:                                          ; preds = %continue__25
  store i64 0, i64* %bar__1, align 4
  %325 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %326 = call %Result* @__quantum__rt__result_get_zero()
  %327 = call i1 @__quantum__rt__result_equal(%Result* %325, %Result* %326)
  %328 = select i1 %327, i64 1, i64 0
  %329 = add i64 0, %328
  store i64 %329, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %325, i32 -1)
  %330 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %331 = call %Result* @__quantum__rt__result_get_zero()
  %332 = call i1 @__quantum__rt__result_equal(%Result* %330, %Result* %331)
  %333 = select i1 %332, i64 1, i64 0
  %334 = add i64 %329, %333
  store i64 %334, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %330, i32 -1)
  store i64 %334, i64* %foo, align 4
  br label %continue__27

continue__27:                                     ; preds = %else__2, %then0__8
  %qubit__31 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__26 = call %Qubit* @__quantum__rt__qubit_allocate()
  %335 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__30 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__27 = call %Qubit* @__quantum__rt__qubit_allocate()
  %336 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__31, 0
  %337 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %336, 0
  %338 = insertvalue { [5 x %Qubit*], i64 } %337, i64 5, 1
  %339 = extractvalue { [5 x %Qubit*], i64 } %338, 0
  %340 = extractvalue { [5 x %Qubit*], i64 } %338, 1
  %341 = insertvalue [5 x %Qubit*] %339, %Qubit* %qubit__26, 1
  %342 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %341, 0
  %343 = insertvalue { [5 x %Qubit*], i64 } %342, i64 5, 1
  %344 = extractvalue { [5 x %Qubit*], i64 } %343, 0
  %345 = extractvalue { [5 x %Qubit*], i64 } %343, 1
  %346 = insertvalue [5 x %Qubit*] %344, %Qubit* %335, 2
  %347 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %346, 0
  %348 = insertvalue { [5 x %Qubit*], i64 } %347, i64 5, 1
  %349 = extractvalue { [5 x %Qubit*], i64 } %348, 0
  %350 = extractvalue { [5 x %Qubit*], i64 } %348, 1
  %351 = insertvalue [5 x %Qubit*] %349, %Qubit* %qubit__30, 3
  %352 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %351, 0
  %353 = insertvalue { [5 x %Qubit*], i64 } %352, i64 5, 1
  %354 = extractvalue { [5 x %Qubit*], i64 } %353, 0
  %355 = extractvalue { [5 x %Qubit*], i64 } %353, 1
  %356 = insertvalue [5 x %Qubit*] %354, %Qubit* %qubit__27, 4
  %357 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %356, 0
  %q__20 = insertvalue { [5 x %Qubit*], i64 } %357, i64 5, 1
  %r1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__26)
  %r2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__27)
  %r3 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %r4 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %358 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %359 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %358, 0
  %360 = insertvalue { [5 x %Qubit*], i64 } %359, i64 5, 1
  %361 = extractvalue { [5 x %Qubit*], i64 } %360, 0
  %362 = extractvalue { [5 x %Qubit*], i64 } %360, 1
  %363 = insertvalue [5 x %Qubit*] %361, %Qubit* %qubit__30, 1
  %364 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %363, 0
  %365 = insertvalue { [5 x %Qubit*], i64 } %364, i64 5, 1
  %366 = extractvalue { [5 x %Qubit*], i64 } %365, 0
  %367 = extractvalue { [5 x %Qubit*], i64 } %365, 1
  %368 = insertvalue [5 x %Qubit*] %366, %Qubit* %335, 2
  %369 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %368, 0
  %370 = insertvalue { [5 x %Qubit*], i64 } %369, i64 5, 1
  %371 = extractvalue { [5 x %Qubit*], i64 } %370, 0
  %372 = extractvalue { [5 x %Qubit*], i64 } %370, 1
  %373 = insertvalue [5 x %Qubit*] %371, %Qubit* %qubit__26, 3
  %374 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %373, 0
  %375 = insertvalue { [5 x %Qubit*], i64 } %374, i64 5, 1
  %376 = extractvalue { [5 x %Qubit*], i64 } %375, 0
  %377 = extractvalue { [5 x %Qubit*], i64 } %375, 1
  %378 = insertvalue [5 x %Qubit*] %376, %Qubit* %qubit__31, 4
  %379 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %378, 0
  %380 = insertvalue { [5 x %Qubit*], i64 } %379, i64 5, 1
  %r5 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__30)
  %381 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %382 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %381, 0
  %383 = insertvalue { [5 x %Qubit*], i64 } %382, i64 5, 1
  %384 = extractvalue { [5 x %Qubit*], i64 } %383, 0
  %385 = extractvalue { [5 x %Qubit*], i64 } %383, 1
  %386 = insertvalue [5 x %Qubit*] %384, %Qubit* %qubit__30, 1
  %387 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %386, 0
  %388 = insertvalue { [5 x %Qubit*], i64 } %387, i64 5, 1
  %389 = extractvalue { [5 x %Qubit*], i64 } %388, 0
  %390 = extractvalue { [5 x %Qubit*], i64 } %388, 1
  %391 = insertvalue [5 x %Qubit*] %389, %Qubit* %335, 2
  %392 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %391, 0
  %393 = insertvalue { [5 x %Qubit*], i64 } %392, i64 5, 1
  %394 = extractvalue { [5 x %Qubit*], i64 } %393, 0
  %395 = extractvalue { [5 x %Qubit*], i64 } %393, 1
  %396 = insertvalue [5 x %Qubit*] %394, %Qubit* %qubit__26, 3
  %397 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %396, 0
  %398 = insertvalue { [5 x %Qubit*], i64 } %397, i64 5, 1
  %399 = extractvalue { [5 x %Qubit*], i64 } %398, 0
  %400 = extractvalue { [5 x %Qubit*], i64 } %398, 1
  %401 = insertvalue [5 x %Qubit*] %399, %Qubit* %qubit__31, 4
  %402 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %401, 0
  %z2 = insertvalue { [5 x %Qubit*], i64 } %402, i64 5, 1
  %r6 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__31)
  call void @__quantum__rt__result_update_reference_count(%Result* %r1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r2, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r6, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__31)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__26)
  call void @__quantum__rt__qubit_release(%Qubit* %335)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__30)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__27)
  %__rtrnVal1__ = load i64, i64* %rand, align 4
  %403 = insertvalue { i64, i64 } { i64 6, i64 0 }, i64 %__rtrnVal1__, 1
  call void @__quantum__rt__qubit_release(%Qubit* %79)
  call void @__quantum__rt__qubit_release(%Qubit* %80)
  call void @__quantum__rt__qubit_release(%Qubit* %88)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %m2, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %target)
  call void @__quantum__rt__tuple_start_record_output()
  call void @__quantum__rt__int_record_output(i64 6)
  call void @__quantum__rt__int_record_output(i64 %__rtrnVal1__)
  call void @__quantum__rt__tuple_end_record_output()
  ret void
}

declare void @__quantum__qis__dumpmachine__body(i8*)

declare %Qubit* @__quantum__rt__qubit_allocate()

declare %Array* @__quantum__rt__qubit_allocate_array(i64)

declare void @__quantum__rt__qubit_release(%Qubit*)

declare void @__quantum__qis__h__body(%Qubit*)

declare void @__quantum__qis__cnot__body(%Qubit*, %Qubit*)

declare %Result* @__quantum__qis__m__body(%Qubit*)

declare void @__quantum__qis__logpauli__body(i2)

declare %Result* @__quantum__rt__result_get_zero()

declare i1 @__quantum__rt__result_equal(%Result*, %Result*)

declare %Result* @__quantum__rt__result_get_one()

declare void @__quantum__rt__result_update_reference_count(%Result*, i32)

declare void @__quantum__rt__tuple_start_record_output()

declare void @__quantum__rt__int_record_output(i64)

declare void @__quantum__rt__tuple_end_record_output()

attributes #0 = { "EntryPoint" }