
%Result = type opaque
%Qubit = type opaque
%Array = type opaque

define void @Microsoft__Quantum__Testing__QIR__TestProfileTargeting() #0 {
entry:
  %0 = alloca { [1 x %Result*], i64 }, align 8
  %bar__1 = alloca i64, align 8
  %bar = alloca i64, align 8
  %foo = alloca i64, align 8
  %1 = alloca { i1, i1 }, align 8
  %rand = alloca i64, align 8
  %2 = alloca { i64, %Result* }, align 8
  %3 = alloca { i64, %Result* }, align 8
  %4 = alloca { i64, %Result* }, align 8
  %5 = alloca { i64, %Result* }, align 8
  %6 = alloca { i64, %Result* }, align 8
  %7 = alloca { i64, %Result* }, align 8
  %8 = alloca { [4 x { [3 x %Qubit*], i64 }], i64 }, align 8
  %9 = alloca { [4 x { [2 x %Qubit*], i64 }], i64 }, align 8
  %10 = alloca { [3 x { [3 x i2], i64 }], i64 }, align 8
  %11 = alloca { [3 x { [2 x i2], i64 }], i64 }, align 8
  %12 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  %13 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %14 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %15 = alloca { { [2 x i64], i64 }, { [2 x i64], i64 } }, align 8
  %16 = alloca i64, align 8
  %17 = alloca { [2 x i64], i64 }, align 8
  %18 = alloca i64, align 8
  %19 = alloca i64, align 8
  %20 = alloca double, align 8
  %21 = alloca { { i64, double }, double }, align 8
  %22 = alloca { { i64, double }, double }, align 8
  %23 = alloca {}, align 8
  %24 = alloca { i64, double }, align 8
  %25 = alloca { { i64, double }, { i64, double } }, align 8
  %26 = alloca { [3 x i64], i64 }, align 8
  %27 = alloca { [3 x i64], i64 }, align 8
  %28 = alloca { [3 x i64], i64 }, align 8
  %29 = alloca { [6 x i64], i64 }, align 8
  %30 = alloca i64, align 8
  %31 = alloca i64, align 8
  %32 = alloca i64, align 8
  %33 = alloca { [3 x i64], i64 }, align 8
  %sum = alloca i64, align 8
  %34 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %34, align 4
  %35 = bitcast { [3 x i64], i64 }* %34 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %35)
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 6], i64 3 }, { [3 x i64], i64 }* %33, align 4
  %36 = bitcast { [3 x i64], i64 }* %33 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %36)
  store i64 1, i64* %32, align 4
  %37 = bitcast i64* %32 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %37)
  store i64 2, i64* %31, align 4
  %38 = bitcast i64* %31 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %38)
  store i64 3, i64* %30, align 4
  %39 = bitcast i64* %30 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %39)
  store { [6 x i64], i64 } { [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6], i64 6 }, { [6 x i64], i64 }* %29, align 4
  %40 = bitcast { [6 x i64], i64 }* %29 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %40)
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 2], i64 3 }, { [3 x i64], i64 }* %28, align 4
  %41 = bitcast { [3 x i64], i64 }* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %41)
  store { [3 x i64], i64 } { [3 x i64] [i64 2, i64 3, i64 6], i64 3 }, { [3 x i64], i64 }* %27, align 4
  %42 = bitcast { [3 x i64], i64 }* %27 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %42)
  store { [3 x i64], i64 } { [3 x i64] [i64 4, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %26, align 4
  %43 = bitcast { [3 x i64], i64 }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %43)
  store { { i64, double }, { i64, double } } { { i64, double } { i64 1, double 1.000000e+00 }, { i64, double } { i64 5, double 1.000000e+00 } }, { { i64, double }, { i64, double } }* %25, align 8
  %44 = bitcast { { i64, double }, { i64, double } }* %25 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %44)
  store { i64, double } { i64 1, double 2.000000e+00 }, { i64, double }* %24, align 8
  %45 = bitcast { i64, double }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %45)
  store {} zeroinitializer, {}* %23, align 1
  %46 = bitcast {}* %23 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %46)
  store { { i64, double }, double } { { i64, double } { i64 1, double 1.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %22, align 8
  %47 = bitcast { { i64, double }, double }* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %47)
  store { { i64, double }, double } { { i64, double } { i64 1, double 3.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %21, align 8
  %48 = bitcast { { i64, double }, double }* %21 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  store double 3.000000e+00, double* %20, align 8
  %49 = bitcast double* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %49)
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  %50 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %51 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %50, 0
  %52 = insertvalue { [2 x %Qubit*], i64 } %51, i64 2, 1
  %53 = extractvalue { [2 x %Qubit*], i64 } %52, 0
  %54 = extractvalue { [2 x %Qubit*], i64 } %52, 1
  %55 = insertvalue [2 x %Qubit*] %53, %Qubit* %target, 1
  %56 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %55, 0
  %qs = insertvalue { [2 x %Qubit*], i64 } %56, i64 2, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  store i64 1, i64* %19, align 4
  %57 = bitcast i64* %19 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %57)
  call void @__quantum__qis__logpauli__body(i2 -2)
  store i64 2, i64* %18, align 4
  %58 = bitcast i64* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %58)
  store { [2 x i64], i64 } { [2 x i64] [i64 1, i64 2], i64 2 }, { [2 x i64], i64 }* %17, align 4
  %59 = bitcast { [2 x i64], i64 }* %17 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %59)
  store i64 3, i64* %16, align 4
  %60 = bitcast i64* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %60)
  store { { [2 x i64], i64 }, { [2 x i64], i64 } } { { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 } }, { { [2 x i64], i64 }, { [2 x i64], i64 } }* %15, align 4
  %61 = bitcast { { [2 x i64], i64 }, { [2 x i64], i64 } }* %15 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %61)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } { [2 x i64] [i64 2, i64 1], i64 2 }, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %14, align 4
  %62 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %62)
  call void @__quantum__qis__logpauli__body(i2 0)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %13, align 4
  %63 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %13 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %63)
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %12, align 4
  %64 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %64)
  store { [3 x { [2 x i2], i64 }], i64 } { [3 x { [2 x i2], i64 }] [{ [2 x i2], i64 } zeroinitializer, { [2 x i2], i64 } { [2 x i2] [i2 -1, i2 0], i64 1 }, { [2 x i2], i64 } { [2 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [2 x i2], i64 }], i64 }* %11, align 4
  %65 = bitcast { [3 x { [2 x i2], i64 }], i64 }* %11 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %65)
  store { [3 x { [3 x i2], i64 }], i64 } { [3 x { [3 x i2], i64 }] [{ [3 x i2], i64 } { [3 x i2] [i2 1, i2 -2, i2 0], i64 2 }, { [3 x i2], i64 } { [3 x i2] [i2 1, i2 1, i2 1], i64 3 }, { [3 x i2], i64 } { [3 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [3 x i2], i64 }], i64 }* %10, align 4
  %66 = bitcast { [3 x { [3 x i2], i64 }], i64 }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %66)
  %67 = call %Qubit* @__quantum__rt__qubit_allocate()
  %68 = call %Qubit* @__quantum__rt__qubit_allocate()
  %69 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %67, 0
  %70 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %69, 0
  %71 = insertvalue { [2 x %Qubit*], i64 } %70, i64 2, 1
  %72 = extractvalue { [2 x %Qubit*], i64 } %71, 0
  %73 = extractvalue { [2 x %Qubit*], i64 } %71, 1
  %74 = insertvalue [2 x %Qubit*] %72, %Qubit* %68, 1
  %75 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %74, 0
  %qs1 = insertvalue { [2 x %Qubit*], i64 } %75, i64 2, 1
  %76 = call %Qubit* @__quantum__rt__qubit_allocate()
  %77 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %76, 0
  %78 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %77, 0
  %qs2 = insertvalue { [1 x %Qubit*], i64 } %78, i64 1, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %79 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %80 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %79, 0
  %81 = insertvalue { [1 x %Qubit*], i64 } %80, i64 1, 1
  %82 = extractvalue { [2 x %Qubit*], i64 } %qs1, 0
  %83 = extractvalue { [2 x %Qubit*], i64 } %qs1, 1
  %84 = extractvalue { [1 x %Qubit*], i64 } %qs2, 0
  %85 = extractvalue { [1 x %Qubit*], i64 } %qs2, 1
  %86 = extractvalue { [1 x %Qubit*], i64 } %81, 0
  %87 = extractvalue { [1 x %Qubit*], i64 } %81, 1
  %88 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %67, 0
  %89 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %88, 0
  %90 = insertvalue { [2 x %Qubit*], i64 } %89, i64 2, 1
  %91 = extractvalue { [2 x %Qubit*], i64 } %90, 0
  %92 = extractvalue { [2 x %Qubit*], i64 } %90, 1
  %93 = insertvalue [2 x %Qubit*] %91, %Qubit* %68, 1
  %94 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %93, 0
  %95 = insertvalue { [2 x %Qubit*], i64 } %94, i64 2, 1
  %96 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %76, 0
  %97 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %96, 0
  %98 = insertvalue { [2 x %Qubit*], i64 } %97, i64 1, 1
  %99 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %100 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %99, 0
  %101 = insertvalue { [2 x %Qubit*], i64 } %100, i64 1, 1
  %102 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %95, 0
  %103 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %102, 0
  %104 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %103, i64 4, 1
  %105 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %104, 0
  %106 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %104, 1
  %107 = insertvalue [4 x { [2 x %Qubit*], i64 }] %105, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %108 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %107, 0
  %109 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %108, i64 4, 1
  %110 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %109, 0
  %111 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %109, 1
  %112 = insertvalue [4 x { [2 x %Qubit*], i64 }] %110, { [2 x %Qubit*], i64 } %98, 2
  %113 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %112, 0
  %114 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %113, i64 4, 1
  %115 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %114, 0
  %116 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %114, 1
  %117 = insertvalue [4 x { [2 x %Qubit*], i64 }] %115, { [2 x %Qubit*], i64 } %101, 3
  %118 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %117, 0
  %qubitArrArr = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %118, i64 4, 1
  %119 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %120 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %121 = extractvalue { [2 x %Qubit*], i64 } %95, 0
  %122 = extractvalue { [2 x %Qubit*], i64 } %95, 1
  %123 = extractvalue { [2 x %Qubit*], i64 } %98, 0
  %124 = extractvalue { [2 x %Qubit*], i64 } %98, 1
  %125 = extractvalue { [2 x %Qubit*], i64 } %101, 0
  %126 = extractvalue { [2 x %Qubit*], i64 } %101, 1
  %127 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %67, 0
  %128 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %127, 0
  %129 = insertvalue { [2 x %Qubit*], i64 } %128, i64 2, 1
  %130 = extractvalue { [2 x %Qubit*], i64 } %129, 0
  %131 = extractvalue { [2 x %Qubit*], i64 } %129, 1
  %132 = insertvalue [2 x %Qubit*] %130, %Qubit* %68, 1
  %133 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %132, 0
  %134 = insertvalue { [2 x %Qubit*], i64 } %133, i64 2, 1
  %135 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %76, 0
  %136 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %135, 0
  %137 = insertvalue { [2 x %Qubit*], i64 } %136, i64 1, 1
  %138 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %139 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %138, 0
  %140 = insertvalue { [2 x %Qubit*], i64 } %139, i64 1, 1
  %141 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %134, 0
  %142 = insertvalue [4 x { [2 x %Qubit*], i64 }] %141, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %143 = insertvalue [4 x { [2 x %Qubit*], i64 }] %142, { [2 x %Qubit*], i64 } %137, 2
  %144 = insertvalue [4 x { [2 x %Qubit*], i64 }] %143, { [2 x %Qubit*], i64 } %140, 3
  %145 = insertvalue [4 x { [2 x %Qubit*], i64 }] %144, { [2 x %Qubit*], i64 } zeroinitializer, 0
  %146 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %145, 0
  %147 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %146, i64 4, 1
  store { [4 x { [2 x %Qubit*], i64 }], i64 } %147, { [4 x { [2 x %Qubit*], i64 }], i64 }* %9, align 8
  %148 = bitcast { [4 x { [2 x %Qubit*], i64 }], i64 }* %9 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %148)
  %149 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %150 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %149, 0
  %151 = insertvalue { [3 x %Qubit*], i64 } %150, i64 3, 1
  %152 = extractvalue { [3 x %Qubit*], i64 } %151, 0
  %153 = extractvalue { [3 x %Qubit*], i64 } %151, 1
  %154 = insertvalue [3 x %Qubit*] %152, %Qubit* %q, 1
  %155 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %154, 0
  %156 = insertvalue { [3 x %Qubit*], i64 } %155, i64 3, 1
  %157 = extractvalue { [3 x %Qubit*], i64 } %156, 0
  %158 = extractvalue { [3 x %Qubit*], i64 } %156, 1
  %159 = insertvalue [3 x %Qubit*] %157, %Qubit* %q, 2
  %160 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %159, 0
  %161 = insertvalue { [3 x %Qubit*], i64 } %160, i64 3, 1
  %162 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %163 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %164 = extractvalue { [3 x %Qubit*], i64 } %161, 0
  %165 = extractvalue { [3 x %Qubit*], i64 } %161, 1
  %166 = extractvalue { [2 x %Qubit*], i64 } %95, 0
  %167 = extractvalue { [2 x %Qubit*], i64 } %95, 1
  %168 = extractvalue { [2 x %Qubit*], i64 } %98, 0
  %169 = extractvalue { [2 x %Qubit*], i64 } %98, 1
  %170 = extractvalue { [2 x %Qubit*], i64 } %101, 0
  %171 = extractvalue { [2 x %Qubit*], i64 } %101, 1
  %172 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %173 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %172, 0
  %174 = insertvalue { [3 x %Qubit*], i64 } %173, i64 3, 1
  %175 = extractvalue { [3 x %Qubit*], i64 } %174, 0
  %176 = extractvalue { [3 x %Qubit*], i64 } %174, 1
  %177 = insertvalue [3 x %Qubit*] %175, %Qubit* %q, 1
  %178 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %177, 0
  %179 = insertvalue { [3 x %Qubit*], i64 } %178, i64 3, 1
  %180 = extractvalue { [3 x %Qubit*], i64 } %179, 0
  %181 = extractvalue { [3 x %Qubit*], i64 } %179, 1
  %182 = insertvalue [3 x %Qubit*] %180, %Qubit* %q, 2
  %183 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %182, 0
  %184 = insertvalue { [3 x %Qubit*], i64 } %183, i64 3, 1
  %185 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %67, 0
  %186 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %185, 0
  %187 = insertvalue { [3 x %Qubit*], i64 } %186, i64 2, 1
  %188 = extractvalue { [3 x %Qubit*], i64 } %187, 0
  %189 = extractvalue { [3 x %Qubit*], i64 } %187, 1
  %190 = insertvalue [3 x %Qubit*] %188, %Qubit* %68, 1
  %191 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %190, 0
  %192 = insertvalue { [3 x %Qubit*], i64 } %191, i64 2, 1
  %193 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %76, 0
  %194 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %193, 0
  %195 = insertvalue { [3 x %Qubit*], i64 } %194, i64 1, 1
  %196 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %197 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %196, 0
  %198 = insertvalue { [3 x %Qubit*], i64 } %197, i64 1, 1
  %199 = insertvalue [4 x { [3 x %Qubit*], i64 }] zeroinitializer, { [3 x %Qubit*], i64 } %192, 0
  %200 = insertvalue [4 x { [3 x %Qubit*], i64 }] %199, { [3 x %Qubit*], i64 } zeroinitializer, 1
  %201 = insertvalue [4 x { [3 x %Qubit*], i64 }] %200, { [3 x %Qubit*], i64 } %195, 2
  %202 = insertvalue [4 x { [3 x %Qubit*], i64 }] %201, { [3 x %Qubit*], i64 } %198, 3
  %203 = insertvalue [4 x { [3 x %Qubit*], i64 }] %202, { [3 x %Qubit*], i64 } %184, 1
  %204 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [3 x %Qubit*], i64 }] %203, 0
  %205 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } %204, i64 4, 1
  store { [4 x { [3 x %Qubit*], i64 }], i64 } %205, { [4 x { [3 x %Qubit*], i64 }], i64 }* %8, align 8
  %206 = bitcast { [4 x { [3 x %Qubit*], i64 }], i64 }* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %206)
  %207 = call %Result* @__quantum__rt__result_get_zero()
  %208 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %207)
  br i1 %208, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %209 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %210 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %209, 1
  store { i64, %Result* } %210, { i64, %Result* }* %7, align 8
  %211 = bitcast { i64, %Result* }* %7 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %211)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %212 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %213 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %212, 1
  store { i64, %Result* } %213, { i64, %Result* }* %6, align 8
  %214 = bitcast { i64, %Result* }* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %214)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  br i1 true, label %then1__2, label %continue__2

then1__2:                                         ; preds = %continue__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %215 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %216 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %215, 1
  store { i64, %Result* } %216, { i64, %Result* }* %5, align 8
  %217 = bitcast { i64, %Result* }* %5 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %217)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %then1__2, %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  br i1 true, label %then0__1, label %continue__3

then0__1:                                         ; preds = %continue__2
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %218 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %219 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %218, 1
  store { i64, %Result* } %219, { i64, %Result* }* %4, align 8
  %220 = bitcast { i64, %Result* }* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %220)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %then0__1, %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %221 = call %Result* @__quantum__rt__result_get_zero()
  %222 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %221)
  br i1 %222, label %then0__2, label %else__1

then0__2:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %223 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %224 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %223, 1
  store { i64, %Result* } %224, { i64, %Result* }* %3, align 8
  %225 = bitcast { i64, %Result* }* %3 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %225)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %226 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %227 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %226, 1
  store { i64, %Result* } %227, { i64, %Result* }* %2, align 8
  %228 = bitcast { i64, %Result* }* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %228)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__2
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %229 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %230 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %229, 0
  %qubits = insertvalue { [1 x %Qubit*], i64 } %230, i64 1, 1
  br label %continue__6

continue__6:                                      ; preds = %continue__4
  %231 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %231, i32 1)
  %232 = call %Result* @__quantum__rt__result_get_one()
  %233 = call i1 @__quantum__rt__result_equal(%Result* %231, %Result* %232)
  call void @__quantum__rt__result_update_reference_count(%Result* %231, i32 -1)
  br i1 %233, label %then0__3, label %continue__5

then0__3:                                         ; preds = %continue__6
  store i64 1, i64* %rand, align 4
  br label %continue__5

continue__5:                                      ; preds = %then0__3, %continue__6
  %234 = load i64, i64* %rand, align 4
  %235 = shl i64 %234, 1
  store i64 %235, i64* %rand, align 4
  %236 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %237 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %236, 0
  %qubits__1 = insertvalue { [1 x %Qubit*], i64 } %237, i64 1, 1
  br label %continue__8

continue__8:                                      ; preds = %continue__5
  %238 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %238, i32 1)
  %239 = call %Result* @__quantum__rt__result_get_one()
  %240 = call i1 @__quantum__rt__result_equal(%Result* %238, %Result* %239)
  call void @__quantum__rt__result_update_reference_count(%Result* %238, i32 -1)
  br i1 %240, label %then0__4, label %continue__7

then0__4:                                         ; preds = %continue__8
  %241 = add i64 %235, 1
  store i64 %241, i64* %rand, align 4
  br label %continue__7

continue__7:                                      ; preds = %then0__4, %continue__8
  %242 = call %Result* @__quantum__rt__result_get_zero()
  %a = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %242)
  %a__1 = call %Result* @__quantum__rt__result_get_zero()
  %243 = call %Result* @__quantum__rt__result_get_one()
  %244 = call i1 @__quantum__rt__result_equal(%Result* %a__1, %Result* %243)
  %c = or i1 %244, %a
  %245 = insertvalue { i1, i1 } zeroinitializer, i1 %a, 0
  %246 = insertvalue { i1, i1 } %245, i1 %c, 1
  store { i1, i1 } %246, { i1, i1 }* %1, align 1
  %247 = bitcast { i1, i1 }* %1 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %247)
  %248 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %m2)
  %249 = select i1 %248, i64 6, i64 0
  store i64 %249, i64* %foo, align 4
  %250 = call %Result* @__quantum__rt__result_get_zero()
  %251 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %250)
  br i1 %251, label %then0__5, label %else__2

then0__5:                                         ; preds = %continue__7
  store i64 0, i64* %bar, align 4
  %252 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %253 = call %Result* @__quantum__rt__result_get_one()
  %254 = call i1 @__quantum__rt__result_equal(%Result* %252, %Result* %253)
  %255 = select i1 %254, i64 1, i64 0
  %256 = add i64 0, %255
  store i64 %256, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %252, i32 -1)
  %257 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %258 = call %Result* @__quantum__rt__result_get_one()
  %259 = call i1 @__quantum__rt__result_equal(%Result* %257, %Result* %258)
  %260 = select i1 %259, i64 1, i64 0
  %261 = add i64 %256, %260
  store i64 %261, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %257, i32 -1)
  store i64 %261, i64* %foo, align 4
  br label %continue__9

else__2:                                          ; preds = %continue__7
  store i64 0, i64* %bar__1, align 4
  %262 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %263 = call %Result* @__quantum__rt__result_get_zero()
  %264 = call i1 @__quantum__rt__result_equal(%Result* %262, %Result* %263)
  %265 = select i1 %264, i64 1, i64 0
  %266 = add i64 0, %265
  store i64 %266, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %262, i32 -1)
  %267 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %268 = call %Result* @__quantum__rt__result_get_zero()
  %269 = call i1 @__quantum__rt__result_equal(%Result* %267, %Result* %268)
  %270 = select i1 %269, i64 1, i64 0
  %271 = add i64 %266, %270
  store i64 %271, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %267, i32 -1)
  store i64 %271, i64* %foo, align 4
  br label %continue__9

continue__9:                                      ; preds = %else__2, %then0__5
  %qubit__18 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__13 = call %Qubit* @__quantum__rt__qubit_allocate()
  %272 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__17 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %273 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__18, 0
  %274 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %273, 0
  %275 = insertvalue { [5 x %Qubit*], i64 } %274, i64 5, 1
  %276 = extractvalue { [5 x %Qubit*], i64 } %275, 0
  %277 = extractvalue { [5 x %Qubit*], i64 } %275, 1
  %278 = insertvalue [5 x %Qubit*] %276, %Qubit* %qubit__13, 1
  %279 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %278, 0
  %280 = insertvalue { [5 x %Qubit*], i64 } %279, i64 5, 1
  %281 = extractvalue { [5 x %Qubit*], i64 } %280, 0
  %282 = extractvalue { [5 x %Qubit*], i64 } %280, 1
  %283 = insertvalue [5 x %Qubit*] %281, %Qubit* %272, 2
  %284 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %283, 0
  %285 = insertvalue { [5 x %Qubit*], i64 } %284, i64 5, 1
  %286 = extractvalue { [5 x %Qubit*], i64 } %285, 0
  %287 = extractvalue { [5 x %Qubit*], i64 } %285, 1
  %288 = insertvalue [5 x %Qubit*] %286, %Qubit* %qubit__17, 3
  %289 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %288, 0
  %290 = insertvalue { [5 x %Qubit*], i64 } %289, i64 5, 1
  %291 = extractvalue { [5 x %Qubit*], i64 } %290, 0
  %292 = extractvalue { [5 x %Qubit*], i64 } %290, 1
  %293 = insertvalue [5 x %Qubit*] %291, %Qubit* %qubit__14, 4
  %294 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %293, 0
  %q__7 = insertvalue { [5 x %Qubit*], i64 } %294, i64 5, 1
  %r1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__13)
  %r2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__14)
  %r3 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %r4 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %295 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__14, 0
  %296 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %295, 0
  %297 = insertvalue { [5 x %Qubit*], i64 } %296, i64 5, 1
  %298 = extractvalue { [5 x %Qubit*], i64 } %297, 0
  %299 = extractvalue { [5 x %Qubit*], i64 } %297, 1
  %300 = insertvalue [5 x %Qubit*] %298, %Qubit* %qubit__17, 1
  %301 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %300, 0
  %302 = insertvalue { [5 x %Qubit*], i64 } %301, i64 5, 1
  %303 = extractvalue { [5 x %Qubit*], i64 } %302, 0
  %304 = extractvalue { [5 x %Qubit*], i64 } %302, 1
  %305 = insertvalue [5 x %Qubit*] %303, %Qubit* %272, 2
  %306 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %305, 0
  %307 = insertvalue { [5 x %Qubit*], i64 } %306, i64 5, 1
  %308 = extractvalue { [5 x %Qubit*], i64 } %307, 0
  %309 = extractvalue { [5 x %Qubit*], i64 } %307, 1
  %310 = insertvalue [5 x %Qubit*] %308, %Qubit* %qubit__13, 3
  %311 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %310, 0
  %312 = insertvalue { [5 x %Qubit*], i64 } %311, i64 5, 1
  %313 = extractvalue { [5 x %Qubit*], i64 } %312, 0
  %314 = extractvalue { [5 x %Qubit*], i64 } %312, 1
  %315 = insertvalue [5 x %Qubit*] %313, %Qubit* %qubit__18, 4
  %316 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %315, 0
  %317 = insertvalue { [5 x %Qubit*], i64 } %316, i64 5, 1
  %r5 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__17)
  %318 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__14, 0
  %319 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %318, 0
  %320 = insertvalue { [5 x %Qubit*], i64 } %319, i64 5, 1
  %321 = extractvalue { [5 x %Qubit*], i64 } %320, 0
  %322 = extractvalue { [5 x %Qubit*], i64 } %320, 1
  %323 = insertvalue [5 x %Qubit*] %321, %Qubit* %qubit__17, 1
  %324 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %323, 0
  %325 = insertvalue { [5 x %Qubit*], i64 } %324, i64 5, 1
  %326 = extractvalue { [5 x %Qubit*], i64 } %325, 0
  %327 = extractvalue { [5 x %Qubit*], i64 } %325, 1
  %328 = insertvalue [5 x %Qubit*] %326, %Qubit* %272, 2
  %329 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %328, 0
  %330 = insertvalue { [5 x %Qubit*], i64 } %329, i64 5, 1
  %331 = extractvalue { [5 x %Qubit*], i64 } %330, 0
  %332 = extractvalue { [5 x %Qubit*], i64 } %330, 1
  %333 = insertvalue [5 x %Qubit*] %331, %Qubit* %qubit__13, 3
  %334 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %333, 0
  %335 = insertvalue { [5 x %Qubit*], i64 } %334, i64 5, 1
  %336 = extractvalue { [5 x %Qubit*], i64 } %335, 0
  %337 = extractvalue { [5 x %Qubit*], i64 } %335, 1
  %338 = insertvalue [5 x %Qubit*] %336, %Qubit* %qubit__18, 4
  %339 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %338, 0
  %z2 = insertvalue { [5 x %Qubit*], i64 } %339, i64 5, 1
  %r6 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__18)
  call void @__quantum__rt__result_update_reference_count(%Result* %r1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r2, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r6, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__18)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__13)
  call void @__quantum__rt__qubit_release(%Qubit* %272)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__17)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__14)
  %340 = insertvalue [1 x %Result*] zeroinitializer, %Result* %m1, 0
  %341 = insertvalue { [1 x %Result*], i64 } zeroinitializer, [1 x %Result*] %340, 0
  %arr3 = insertvalue { [1 x %Result*], i64 } %341, i64 1, 1
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 1)
  %342 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %m1)
  %343 = xor i1 %342, true
  br i1 %343, label %then0__6, label %continue__10

then0__6:                                         ; preds = %continue__9
  %344 = extractvalue { [1 x %Result*], i64 } %arr3, 0
  %345 = extractvalue { [1 x %Result*], i64 } %arr3, 1
  %346 = insertvalue [1 x %Result*] %344, %Result* %m2, 0
  %347 = insertvalue { [1 x %Result*], i64 } zeroinitializer, [1 x %Result*] %346, 0
  %348 = insertvalue { [1 x %Result*], i64 } %347, i64 1, 1
  store { [1 x %Result*], i64 } %348, { [1 x %Result*], i64 }* %0, align 8
  %349 = bitcast { [1 x %Result*], i64 }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %349)
  br label %continue__10

continue__10:                                     ; preds = %then0__6, %continue__9
  %__rtrnVal1__ = load i64, i64* %rand, align 4
  %350 = insertvalue { i64, i64 } { i64 6, i64 0 }, i64 %__rtrnVal1__, 1
  call void @__quantum__rt__qubit_release(%Qubit* %67)
  call void @__quantum__rt__qubit_release(%Qubit* %68)
  call void @__quantum__rt__qubit_release(%Qubit* %76)
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
