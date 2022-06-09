
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
  %7 = alloca { [4 x { [3 x %Qubit*], i64 }], i64 }, align 8
  %8 = alloca { [4 x { [2 x %Qubit*], i64 }], i64 }, align 8
  %9 = alloca { [3 x { [3 x i2], i64 }], i64 }, align 8
  %10 = alloca { [3 x { [2 x i2], i64 }], i64 }, align 8
  %11 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  %12 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %13 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %14 = alloca { { [2 x i64], i64 }, { [2 x i64], i64 } }, align 8
  %15 = alloca i64, align 8
  %16 = alloca { [2 x i64], i64 }, align 8
  %17 = alloca i64, align 8
  %18 = alloca i64, align 8
  %19 = alloca double, align 8
  %20 = alloca { { i64, double }, double }, align 8
  %21 = alloca { { i64, double }, double }, align 8
  %22 = alloca {}, align 8
  %23 = alloca { i64, double }, align 8
  %24 = alloca { { i64, double }, { i64, double } }, align 8
  %25 = alloca { [3 x i64], i64 }, align 8
  %26 = alloca { [3 x i64], i64 }, align 8
  %27 = alloca { [3 x i64], i64 }, align 8
  %28 = alloca { [6 x i64], i64 }, align 8
  %29 = alloca i64, align 8
  %30 = alloca i64, align 8
  %31 = alloca i64, align 8
  %32 = alloca { [3 x i64], i64 }, align 8
  %sum = alloca i64, align 8
  %33 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %33, align 4
  %34 = bitcast { [3 x i64], i64 }* %33 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %34)
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 6], i64 3 }, { [3 x i64], i64 }* %32, align 4
  %35 = bitcast { [3 x i64], i64 }* %32 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %35)
  store i64 1, i64* %31, align 4
  %36 = bitcast i64* %31 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %36)
  store i64 2, i64* %30, align 4
  %37 = bitcast i64* %30 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %37)
  store i64 3, i64* %29, align 4
  %38 = bitcast i64* %29 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %38)
  store { [6 x i64], i64 } { [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6], i64 6 }, { [6 x i64], i64 }* %28, align 4
  %39 = bitcast { [6 x i64], i64 }* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %39)
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 2], i64 3 }, { [3 x i64], i64 }* %27, align 4
  %40 = bitcast { [3 x i64], i64 }* %27 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %40)
  store { [3 x i64], i64 } { [3 x i64] [i64 2, i64 3, i64 6], i64 3 }, { [3 x i64], i64 }* %26, align 4
  %41 = bitcast { [3 x i64], i64 }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %41)
  store { [3 x i64], i64 } { [3 x i64] [i64 4, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %25, align 4
  %42 = bitcast { [3 x i64], i64 }* %25 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %42)
  store { { i64, double }, { i64, double } } { { i64, double } { i64 1, double 1.000000e+00 }, { i64, double } { i64 5, double 1.000000e+00 } }, { { i64, double }, { i64, double } }* %24, align 8
  %43 = bitcast { { i64, double }, { i64, double } }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %43)
  store { i64, double } { i64 1, double 2.000000e+00 }, { i64, double }* %23, align 8
  %44 = bitcast { i64, double }* %23 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %44)
  store {} zeroinitializer, {}* %22, align 1
  %45 = bitcast {}* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %45)
  store { { i64, double }, double } { { i64, double } { i64 1, double 1.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %21, align 8
  %46 = bitcast { { i64, double }, double }* %21 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %46)
  store { { i64, double }, double } { { i64, double } { i64 1, double 3.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %20, align 8
  %47 = bitcast { { i64, double }, double }* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %47)
  store double 3.000000e+00, double* %19, align 8
  %48 = bitcast double* %19 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  %49 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %50 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %49, 0
  %51 = insertvalue { [2 x %Qubit*], i64 } %50, i64 2, 1
  %52 = extractvalue { [2 x %Qubit*], i64 } %51, 0
  %53 = extractvalue { [2 x %Qubit*], i64 } %51, 1
  %54 = insertvalue [2 x %Qubit*] %52, %Qubit* %target, 1
  %55 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %54, 0
  %qs = insertvalue { [2 x %Qubit*], i64 } %55, i64 2, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  store i64 1, i64* %18, align 4
  %56 = bitcast i64* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %56)
  call void @__quantum__qis__logpauli__body(i2 -2)
  store i64 2, i64* %17, align 4
  %57 = bitcast i64* %17 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %57)
  store { [2 x i64], i64 } { [2 x i64] [i64 1, i64 2], i64 2 }, { [2 x i64], i64 }* %16, align 4
  %58 = bitcast { [2 x i64], i64 }* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %58)
  store i64 3, i64* %15, align 4
  %59 = bitcast i64* %15 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %59)
  store { { [2 x i64], i64 }, { [2 x i64], i64 } } { { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 } }, { { [2 x i64], i64 }, { [2 x i64], i64 } }* %14, align 4
  %60 = bitcast { { [2 x i64], i64 }, { [2 x i64], i64 } }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %60)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } { [2 x i64] [i64 2, i64 1], i64 2 }, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %13, align 4
  %61 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %13 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %61)
  call void @__quantum__qis__logpauli__body(i2 0)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %12, align 4
  %62 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %62)
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %11, align 4
  %63 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %11 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %63)
  store { [3 x { [2 x i2], i64 }], i64 } { [3 x { [2 x i2], i64 }] [{ [2 x i2], i64 } zeroinitializer, { [2 x i2], i64 } { [2 x i2] [i2 -1, i2 0], i64 1 }, { [2 x i2], i64 } { [2 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [2 x i2], i64 }], i64 }* %10, align 4
  %64 = bitcast { [3 x { [2 x i2], i64 }], i64 }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %64)
  store { [3 x { [3 x i2], i64 }], i64 } { [3 x { [3 x i2], i64 }] [{ [3 x i2], i64 } { [3 x i2] [i2 1, i2 -2, i2 0], i64 2 }, { [3 x i2], i64 } { [3 x i2] [i2 1, i2 1, i2 1], i64 3 }, { [3 x i2], i64 } { [3 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [3 x i2], i64 }], i64 }* %9, align 4
  %65 = bitcast { [3 x { [3 x i2], i64 }], i64 }* %9 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %65)
  %66 = call %Qubit* @__quantum__rt__qubit_allocate()
  %67 = call %Qubit* @__quantum__rt__qubit_allocate()
  %68 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %69 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %68, 0
  %70 = insertvalue { [2 x %Qubit*], i64 } %69, i64 2, 1
  %71 = extractvalue { [2 x %Qubit*], i64 } %70, 0
  %72 = extractvalue { [2 x %Qubit*], i64 } %70, 1
  %73 = insertvalue [2 x %Qubit*] %71, %Qubit* %67, 1
  %74 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %73, 0
  %qs1 = insertvalue { [2 x %Qubit*], i64 } %74, i64 2, 1
  %75 = call %Qubit* @__quantum__rt__qubit_allocate()
  %76 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %75, 0
  %77 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %76, 0
  %qs2 = insertvalue { [1 x %Qubit*], i64 } %77, i64 1, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %78 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %79 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %78, 0
  %80 = insertvalue { [1 x %Qubit*], i64 } %79, i64 1, 1
  %81 = extractvalue { [2 x %Qubit*], i64 } %qs1, 0
  %82 = extractvalue { [2 x %Qubit*], i64 } %qs1, 1
  %83 = extractvalue { [1 x %Qubit*], i64 } %qs2, 0
  %84 = extractvalue { [1 x %Qubit*], i64 } %qs2, 1
  %85 = extractvalue { [1 x %Qubit*], i64 } %80, 0
  %86 = extractvalue { [1 x %Qubit*], i64 } %80, 1
  %87 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %88 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %87, 0
  %89 = insertvalue { [2 x %Qubit*], i64 } %88, i64 2, 1
  %90 = extractvalue { [2 x %Qubit*], i64 } %89, 0
  %91 = extractvalue { [2 x %Qubit*], i64 } %89, 1
  %92 = insertvalue [2 x %Qubit*] %90, %Qubit* %67, 1
  %93 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %92, 0
  %94 = insertvalue { [2 x %Qubit*], i64 } %93, i64 2, 1
  %95 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %75, 0
  %96 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %95, 0
  %97 = insertvalue { [2 x %Qubit*], i64 } %96, i64 1, 1
  %98 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %99 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %98, 0
  %100 = insertvalue { [2 x %Qubit*], i64 } %99, i64 1, 1
  %101 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %94, 0
  %102 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %101, 0
  %103 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %102, i64 4, 1
  %104 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %103, 0
  %105 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %103, 1
  %106 = insertvalue [4 x { [2 x %Qubit*], i64 }] %104, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %107 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %106, 0
  %108 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %107, i64 4, 1
  %109 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %108, 0
  %110 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %108, 1
  %111 = insertvalue [4 x { [2 x %Qubit*], i64 }] %109, { [2 x %Qubit*], i64 } %97, 2
  %112 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %111, 0
  %113 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %112, i64 4, 1
  %114 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %113, 0
  %115 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %113, 1
  %116 = insertvalue [4 x { [2 x %Qubit*], i64 }] %114, { [2 x %Qubit*], i64 } %100, 3
  %117 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %116, 0
  %qubitArrArr = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %117, i64 4, 1
  %118 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %119 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %120 = extractvalue { [2 x %Qubit*], i64 } %94, 0
  %121 = extractvalue { [2 x %Qubit*], i64 } %94, 1
  %122 = extractvalue { [2 x %Qubit*], i64 } %97, 0
  %123 = extractvalue { [2 x %Qubit*], i64 } %97, 1
  %124 = extractvalue { [2 x %Qubit*], i64 } %100, 0
  %125 = extractvalue { [2 x %Qubit*], i64 } %100, 1
  %126 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %127 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %126, 0
  %128 = insertvalue { [2 x %Qubit*], i64 } %127, i64 2, 1
  %129 = extractvalue { [2 x %Qubit*], i64 } %128, 0
  %130 = extractvalue { [2 x %Qubit*], i64 } %128, 1
  %131 = insertvalue [2 x %Qubit*] %129, %Qubit* %67, 1
  %132 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %131, 0
  %133 = insertvalue { [2 x %Qubit*], i64 } %132, i64 2, 1
  %134 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %75, 0
  %135 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %134, 0
  %136 = insertvalue { [2 x %Qubit*], i64 } %135, i64 1, 1
  %137 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %138 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %137, 0
  %139 = insertvalue { [2 x %Qubit*], i64 } %138, i64 1, 1
  %140 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %133, 0
  %141 = insertvalue [4 x { [2 x %Qubit*], i64 }] %140, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %142 = insertvalue [4 x { [2 x %Qubit*], i64 }] %141, { [2 x %Qubit*], i64 } %136, 2
  %143 = insertvalue [4 x { [2 x %Qubit*], i64 }] %142, { [2 x %Qubit*], i64 } %139, 3
  %144 = insertvalue [4 x { [2 x %Qubit*], i64 }] %143, { [2 x %Qubit*], i64 } zeroinitializer, 0
  %145 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %144, 0
  %146 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %145, i64 4, 1
  store { [4 x { [2 x %Qubit*], i64 }], i64 } %146, { [4 x { [2 x %Qubit*], i64 }], i64 }* %8, align 8
  %147 = bitcast { [4 x { [2 x %Qubit*], i64 }], i64 }* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %147)
  %148 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %149 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %148, 0
  %150 = insertvalue { [3 x %Qubit*], i64 } %149, i64 3, 1
  %151 = extractvalue { [3 x %Qubit*], i64 } %150, 0
  %152 = extractvalue { [3 x %Qubit*], i64 } %150, 1
  %153 = insertvalue [3 x %Qubit*] %151, %Qubit* %q, 1
  %154 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %153, 0
  %155 = insertvalue { [3 x %Qubit*], i64 } %154, i64 3, 1
  %156 = extractvalue { [3 x %Qubit*], i64 } %155, 0
  %157 = extractvalue { [3 x %Qubit*], i64 } %155, 1
  %158 = insertvalue [3 x %Qubit*] %156, %Qubit* %q, 2
  %159 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %158, 0
  %160 = insertvalue { [3 x %Qubit*], i64 } %159, i64 3, 1
  %161 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %162 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %163 = extractvalue { [3 x %Qubit*], i64 } %160, 0
  %164 = extractvalue { [3 x %Qubit*], i64 } %160, 1
  %165 = extractvalue { [2 x %Qubit*], i64 } %94, 0
  %166 = extractvalue { [2 x %Qubit*], i64 } %94, 1
  %167 = extractvalue { [2 x %Qubit*], i64 } %97, 0
  %168 = extractvalue { [2 x %Qubit*], i64 } %97, 1
  %169 = extractvalue { [2 x %Qubit*], i64 } %100, 0
  %170 = extractvalue { [2 x %Qubit*], i64 } %100, 1
  %171 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %172 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %171, 0
  %173 = insertvalue { [3 x %Qubit*], i64 } %172, i64 3, 1
  %174 = extractvalue { [3 x %Qubit*], i64 } %173, 0
  %175 = extractvalue { [3 x %Qubit*], i64 } %173, 1
  %176 = insertvalue [3 x %Qubit*] %174, %Qubit* %q, 1
  %177 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %176, 0
  %178 = insertvalue { [3 x %Qubit*], i64 } %177, i64 3, 1
  %179 = extractvalue { [3 x %Qubit*], i64 } %178, 0
  %180 = extractvalue { [3 x %Qubit*], i64 } %178, 1
  %181 = insertvalue [3 x %Qubit*] %179, %Qubit* %q, 2
  %182 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %181, 0
  %183 = insertvalue { [3 x %Qubit*], i64 } %182, i64 3, 1
  %184 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %66, 0
  %185 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %184, 0
  %186 = insertvalue { [3 x %Qubit*], i64 } %185, i64 2, 1
  %187 = extractvalue { [3 x %Qubit*], i64 } %186, 0
  %188 = extractvalue { [3 x %Qubit*], i64 } %186, 1
  %189 = insertvalue [3 x %Qubit*] %187, %Qubit* %67, 1
  %190 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %189, 0
  %191 = insertvalue { [3 x %Qubit*], i64 } %190, i64 2, 1
  %192 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %75, 0
  %193 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %192, 0
  %194 = insertvalue { [3 x %Qubit*], i64 } %193, i64 1, 1
  %195 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %196 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %195, 0
  %197 = insertvalue { [3 x %Qubit*], i64 } %196, i64 1, 1
  %198 = insertvalue [4 x { [3 x %Qubit*], i64 }] zeroinitializer, { [3 x %Qubit*], i64 } %191, 0
  %199 = insertvalue [4 x { [3 x %Qubit*], i64 }] %198, { [3 x %Qubit*], i64 } zeroinitializer, 1
  %200 = insertvalue [4 x { [3 x %Qubit*], i64 }] %199, { [3 x %Qubit*], i64 } %194, 2
  %201 = insertvalue [4 x { [3 x %Qubit*], i64 }] %200, { [3 x %Qubit*], i64 } %197, 3
  %202 = insertvalue [4 x { [3 x %Qubit*], i64 }] %201, { [3 x %Qubit*], i64 } %183, 1
  %203 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [3 x %Qubit*], i64 }] %202, 0
  %204 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } %203, i64 4, 1
  store { [4 x { [3 x %Qubit*], i64 }], i64 } %204, { [4 x { [3 x %Qubit*], i64 }], i64 }* %7, align 8
  %205 = bitcast { [4 x { [3 x %Qubit*], i64 }], i64 }* %7 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %205)
  %206 = call %Result* @__quantum__rt__result_get_zero()
  %207 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %206)
  br i1 %207, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %208 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %209 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %208, 1
  store { i64, %Result* } %209, { i64, %Result* }* %6, align 8
  %210 = bitcast { i64, %Result* }* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %210)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %211 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %212 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %211, 1
  store { i64, %Result* } %212, { i64, %Result* }* %5, align 8
  %213 = bitcast { i64, %Result* }* %5 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %213)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  br i1 true, label %then1__2, label %continue__2

then1__2:                                         ; preds = %continue__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %214 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %215 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %214, 1
  store { i64, %Result* } %215, { i64, %Result* }* %4, align 8
  %216 = bitcast { i64, %Result* }* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %216)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %then1__2, %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  br i1 true, label %then0__1, label %continue__3

then0__1:                                         ; preds = %continue__2
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %217 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %218 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %217, 1
  store { i64, %Result* } %218, { i64, %Result* }* %3, align 8
  %219 = bitcast { i64, %Result* }* %3 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %219)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %then0__1, %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %220 = call %Result* @__quantum__rt__result_get_zero()
  %221 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %220)
  br i1 %221, label %then0__2, label %else__1

then0__2:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %222 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %223 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %222, 1
  store { i64, %Result* } %223, { i64, %Result* }* %2, align 8
  %224 = bitcast { i64, %Result* }* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %224)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %225 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %226 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %225, 1
  store { i64, %Result* } %226, { i64, %Result* }* %1, align 8
  %227 = bitcast { i64, %Result* }* %1 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %227)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__2
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %228 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %229 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %228, 0
  %qubits = insertvalue { [1 x %Qubit*], i64 } %229, i64 1, 1
  %230 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %230, i32 1)
  %231 = call %Result* @__quantum__rt__result_get_one()
  %232 = call i1 @__quantum__rt__result_equal(%Result* %230, %Result* %231)
  call void @__quantum__rt__result_update_reference_count(%Result* %230, i32 -1)
  br i1 %232, label %then0__3, label %continue__5

continue__6:                                      ; No predecessors!
  unreachable

then0__3:                                         ; preds = %continue__4
  store i64 1, i64* %rand, align 4
  br label %continue__5

continue__5:                                      ; preds = %then0__3, %continue__4
  %233 = load i64, i64* %rand, align 4
  %234 = shl i64 %233, 1
  store i64 %234, i64* %rand, align 4
  %235 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %236 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %235, 0
  %qubits__1 = insertvalue { [1 x %Qubit*], i64 } %236, i64 1, 1
  %237 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %237, i32 1)
  %238 = call %Result* @__quantum__rt__result_get_one()
  %239 = call i1 @__quantum__rt__result_equal(%Result* %237, %Result* %238)
  call void @__quantum__rt__result_update_reference_count(%Result* %237, i32 -1)
  br i1 %239, label %then0__4, label %continue__7

continue__8:                                      ; No predecessors!
  unreachable

then0__4:                                         ; preds = %continue__5
  %240 = add i64 %234, 1
  store i64 %240, i64* %rand, align 4
  br label %continue__7

continue__7:                                      ; preds = %then0__4, %continue__5
  %241 = call %Result* @__quantum__rt__result_get_zero()
  %a = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %241)
  %a__1 = call %Result* @__quantum__rt__result_get_zero()
  %242 = call %Result* @__quantum__rt__result_get_one()
  %243 = call i1 @__quantum__rt__result_equal(%Result* %a__1, %Result* %242)
  %c = or i1 %243, %a
  %244 = insertvalue { i1, i1 } zeroinitializer, i1 %a, 0
  %245 = insertvalue { i1, i1 } %244, i1 %c, 1
  store { i1, i1 } %245, { i1, i1 }* %0, align 1
  %246 = bitcast { i1, i1 }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %246)
  %247 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %m2)
  %248 = select i1 %247, i64 6, i64 0
  store i64 %248, i64* %foo, align 4
  %249 = call %Result* @__quantum__rt__result_get_zero()
  %250 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %249)
  br i1 %250, label %then0__5, label %else__2

then0__5:                                         ; preds = %continue__7
  store i64 0, i64* %bar, align 4
  %251 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %252 = call %Result* @__quantum__rt__result_get_one()
  %253 = call i1 @__quantum__rt__result_equal(%Result* %251, %Result* %252)
  %254 = select i1 %253, i64 1, i64 0
  %255 = add i64 0, %254
  store i64 %255, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %251, i32 -1)
  %256 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %257 = call %Result* @__quantum__rt__result_get_one()
  %258 = call i1 @__quantum__rt__result_equal(%Result* %256, %Result* %257)
  %259 = select i1 %258, i64 1, i64 0
  %260 = add i64 %255, %259
  store i64 %260, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %256, i32 -1)
  store i64 %260, i64* %foo, align 4
  br label %continue__9

else__2:                                          ; preds = %continue__7
  store i64 0, i64* %bar__1, align 4
  %261 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %262 = call %Result* @__quantum__rt__result_get_zero()
  %263 = call i1 @__quantum__rt__result_equal(%Result* %261, %Result* %262)
  %264 = select i1 %263, i64 1, i64 0
  %265 = add i64 0, %264
  store i64 %265, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %261, i32 -1)
  %266 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %267 = call %Result* @__quantum__rt__result_get_zero()
  %268 = call i1 @__quantum__rt__result_equal(%Result* %266, %Result* %267)
  %269 = select i1 %268, i64 1, i64 0
  %270 = add i64 %265, %269
  store i64 %270, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %266, i32 -1)
  store i64 %270, i64* %foo, align 4
  br label %continue__9

continue__9:                                      ; preds = %else__2, %then0__5
  %qubit__18 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__13 = call %Qubit* @__quantum__rt__qubit_allocate()
  %271 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__17 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %272 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__18, 0
  %273 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %272, 0
  %274 = insertvalue { [5 x %Qubit*], i64 } %273, i64 5, 1
  %275 = extractvalue { [5 x %Qubit*], i64 } %274, 0
  %276 = extractvalue { [5 x %Qubit*], i64 } %274, 1
  %277 = insertvalue [5 x %Qubit*] %275, %Qubit* %qubit__13, 1
  %278 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %277, 0
  %279 = insertvalue { [5 x %Qubit*], i64 } %278, i64 5, 1
  %280 = extractvalue { [5 x %Qubit*], i64 } %279, 0
  %281 = extractvalue { [5 x %Qubit*], i64 } %279, 1
  %282 = insertvalue [5 x %Qubit*] %280, %Qubit* %271, 2
  %283 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %282, 0
  %284 = insertvalue { [5 x %Qubit*], i64 } %283, i64 5, 1
  %285 = extractvalue { [5 x %Qubit*], i64 } %284, 0
  %286 = extractvalue { [5 x %Qubit*], i64 } %284, 1
  %287 = insertvalue [5 x %Qubit*] %285, %Qubit* %qubit__17, 3
  %288 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %287, 0
  %289 = insertvalue { [5 x %Qubit*], i64 } %288, i64 5, 1
  %290 = extractvalue { [5 x %Qubit*], i64 } %289, 0
  %291 = extractvalue { [5 x %Qubit*], i64 } %289, 1
  %292 = insertvalue [5 x %Qubit*] %290, %Qubit* %qubit__14, 4
  %293 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %292, 0
  %q__7 = insertvalue { [5 x %Qubit*], i64 } %293, i64 5, 1
  %r1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__13)
  %r2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__14)
  %r3 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %r4 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %294 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__14, 0
  %295 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %294, 0
  %296 = insertvalue { [5 x %Qubit*], i64 } %295, i64 5, 1
  %297 = extractvalue { [5 x %Qubit*], i64 } %296, 0
  %298 = extractvalue { [5 x %Qubit*], i64 } %296, 1
  %299 = insertvalue [5 x %Qubit*] %297, %Qubit* %qubit__17, 1
  %300 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %299, 0
  %301 = insertvalue { [5 x %Qubit*], i64 } %300, i64 5, 1
  %302 = extractvalue { [5 x %Qubit*], i64 } %301, 0
  %303 = extractvalue { [5 x %Qubit*], i64 } %301, 1
  %304 = insertvalue [5 x %Qubit*] %302, %Qubit* %271, 2
  %305 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %304, 0
  %306 = insertvalue { [5 x %Qubit*], i64 } %305, i64 5, 1
  %307 = extractvalue { [5 x %Qubit*], i64 } %306, 0
  %308 = extractvalue { [5 x %Qubit*], i64 } %306, 1
  %309 = insertvalue [5 x %Qubit*] %307, %Qubit* %qubit__13, 3
  %310 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %309, 0
  %311 = insertvalue { [5 x %Qubit*], i64 } %310, i64 5, 1
  %312 = extractvalue { [5 x %Qubit*], i64 } %311, 0
  %313 = extractvalue { [5 x %Qubit*], i64 } %311, 1
  %314 = insertvalue [5 x %Qubit*] %312, %Qubit* %qubit__18, 4
  %315 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %314, 0
  %316 = insertvalue { [5 x %Qubit*], i64 } %315, i64 5, 1
  %r5 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__17)
  %317 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__14, 0
  %318 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %317, 0
  %319 = insertvalue { [5 x %Qubit*], i64 } %318, i64 5, 1
  %320 = extractvalue { [5 x %Qubit*], i64 } %319, 0
  %321 = extractvalue { [5 x %Qubit*], i64 } %319, 1
  %322 = insertvalue [5 x %Qubit*] %320, %Qubit* %qubit__17, 1
  %323 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %322, 0
  %324 = insertvalue { [5 x %Qubit*], i64 } %323, i64 5, 1
  %325 = extractvalue { [5 x %Qubit*], i64 } %324, 0
  %326 = extractvalue { [5 x %Qubit*], i64 } %324, 1
  %327 = insertvalue [5 x %Qubit*] %325, %Qubit* %271, 2
  %328 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %327, 0
  %329 = insertvalue { [5 x %Qubit*], i64 } %328, i64 5, 1
  %330 = extractvalue { [5 x %Qubit*], i64 } %329, 0
  %331 = extractvalue { [5 x %Qubit*], i64 } %329, 1
  %332 = insertvalue [5 x %Qubit*] %330, %Qubit* %qubit__13, 3
  %333 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %332, 0
  %334 = insertvalue { [5 x %Qubit*], i64 } %333, i64 5, 1
  %335 = extractvalue { [5 x %Qubit*], i64 } %334, 0
  %336 = extractvalue { [5 x %Qubit*], i64 } %334, 1
  %337 = insertvalue [5 x %Qubit*] %335, %Qubit* %qubit__18, 4
  %338 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %337, 0
  %z2 = insertvalue { [5 x %Qubit*], i64 } %338, i64 5, 1
  %r6 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__18)
  call void @__quantum__rt__result_update_reference_count(%Result* %r1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r2, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r6, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__18)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__13)
  call void @__quantum__rt__qubit_release(%Qubit* %271)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__17)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__14)
  %__rtrnVal1__ = load i64, i64* %rand, align 4
  %339 = insertvalue { i64, i64 } { i64 6, i64 0 }, i64 %__rtrnVal1__, 1
  call void @__quantum__rt__qubit_release(%Qubit* %66)
  call void @__quantum__rt__qubit_release(%Qubit* %67)
  call void @__quantum__rt__qubit_release(%Qubit* %75)
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
