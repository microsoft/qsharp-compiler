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
  %20 = alloca { i64, %Result* }, align 8
  %21 = alloca { [4 x { [3 x %Qubit*], i64 }], i64 }, align 8
  %22 = alloca { [4 x { [2 x %Qubit*], i64 }], i64 }, align 8
  %23 = alloca { [3 x { [3 x i2], i64 }], i64 }, align 8
  %24 = alloca { [3 x { [2 x i2], i64 }], i64 }, align 8
  %25 = alloca { [4 x { [3 x i64], i64 }], i64 }, align 8
  %26 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %27 = alloca { [4 x { [2 x i64], i64 }], i64 }, align 8
  %28 = alloca { { [2 x i64], i64 }, { [2 x i64], i64 } }, align 8
  %29 = alloca i64, align 8
  %30 = alloca { [2 x i64], i64 }, align 8
  %31 = alloca i64, align 8
  %32 = alloca i64, align 8
  %33 = alloca double, align 8
  %34 = alloca { { i64, double }, double }, align 8
  %35 = alloca { { i64, double }, double }, align 8
  %36 = alloca {}, align 8
  %37 = alloca { i64, double }, align 8
  %38 = alloca { { i64, double }, { i64, double } }, align 8
  %39 = alloca { [3 x i64], i64 }, align 8
  %40 = alloca { [3 x i64], i64 }, align 8
  %41 = alloca { [3 x i64], i64 }, align 8
  %42 = alloca { [6 x i64], i64 }, align 8
  %43 = alloca i64, align 8
  %44 = alloca i64, align 8
  %45 = alloca i64, align 8
  %46 = alloca { [3 x i64], i64 }, align 8
  %sum = alloca i64, align 8
  %47 = alloca { [3 x i64], i64 }, align 8
  store { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %47, align 4
  %48 = bitcast { [3 x i64], i64 }* %47 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 6], i64 3 }, { [3 x i64], i64 }* %46, align 4
  %49 = bitcast { [3 x i64], i64 }* %46 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %49)
  store i64 1, i64* %45, align 4
  %50 = bitcast i64* %45 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %50)
  store i64 2, i64* %44, align 4
  %51 = bitcast i64* %44 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %51)
  store i64 3, i64* %43, align 4
  %52 = bitcast i64* %43 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %52)
  store { [6 x i64], i64 } { [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6], i64 6 }, { [6 x i64], i64 }* %42, align 4
  %53 = bitcast { [6 x i64], i64 }* %42 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %53)
  store { [3 x i64], i64 } { [3 x i64] [i64 6, i64 6, i64 2], i64 3 }, { [3 x i64], i64 }* %41, align 4
  %54 = bitcast { [3 x i64], i64 }* %41 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %54)
  store { [3 x i64], i64 } { [3 x i64] [i64 2, i64 3, i64 6], i64 3 }, { [3 x i64], i64 }* %40, align 4
  %55 = bitcast { [3 x i64], i64 }* %40 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %55)
  store { [3 x i64], i64 } { [3 x i64] [i64 4, i64 2, i64 3], i64 3 }, { [3 x i64], i64 }* %39, align 4
  %56 = bitcast { [3 x i64], i64 }* %39 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %56)
  store { { i64, double }, { i64, double } } { { i64, double } { i64 1, double 1.000000e+00 }, { i64, double } { i64 5, double 1.000000e+00 } }, { { i64, double }, { i64, double } }* %38, align 8
  %57 = bitcast { { i64, double }, { i64, double } }* %38 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %57)
  store { i64, double } { i64 1, double 2.000000e+00 }, { i64, double }* %37, align 8
  %58 = bitcast { i64, double }* %37 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %58)
  store {} zeroinitializer, {}* %36, align 1
  %59 = bitcast {}* %36 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %59)
  store { { i64, double }, double } { { i64, double } { i64 1, double 1.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %35, align 8
  %60 = bitcast { { i64, double }, double }* %35 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %60)
  store { { i64, double }, double } { { i64, double } { i64 1, double 3.000000e+00 }, double 0.000000e+00 }, { { i64, double }, double }* %34, align 8
  %61 = bitcast { { i64, double }, double }* %34 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %61)
  store double 3.000000e+00, double* %33, align 8
  %62 = bitcast double* %33 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %62)
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  %target = call %Qubit* @__quantum__rt__qubit_allocate()
  %63 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %64 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %63, 0
  %65 = insertvalue { [2 x %Qubit*], i64 } %64, i64 2, 1
  %66 = extractvalue { [2 x %Qubit*], i64 } %65, 0
  %67 = extractvalue { [2 x %Qubit*], i64 } %65, 1
  %68 = insertvalue [2 x %Qubit*] %66, %Qubit* %target, 1
  %69 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %68, 0
  %qs = insertvalue { [2 x %Qubit*], i64 } %69, i64 2, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  store i64 1, i64* %32, align 4
  %70 = bitcast i64* %32 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %70)
  call void @__quantum__qis__logpauli__body(i2 -2)
  store i64 2, i64* %31, align 4
  %71 = bitcast i64* %31 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %71)
  store { [2 x i64], i64 } { [2 x i64] [i64 1, i64 2], i64 2 }, { [2 x i64], i64 }* %30, align 4
  %72 = bitcast { [2 x i64], i64 }* %30 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %72)
  store i64 3, i64* %29, align 4
  %73 = bitcast i64* %29 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %73)
  store { { [2 x i64], i64 }, { [2 x i64], i64 } } { { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 } }, { { [2 x i64], i64 }, { [2 x i64], i64 } }* %28, align 4
  %74 = bitcast { { [2 x i64], i64 }, { [2 x i64], i64 } }* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %74)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } { [2 x i64] [i64 2, i64 1], i64 2 }, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %27, align 4
  %75 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %27 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %75)
  call void @__quantum__qis__logpauli__body(i2 0)
  store { [4 x { [2 x i64], i64 }], i64 } { [4 x { [2 x i64], i64 }] [{ [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } zeroinitializer, { [2 x i64], i64 } { [2 x i64] [i64 3, i64 0], i64 1 }, { [2 x i64], i64 } { [2 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [2 x i64], i64 }], i64 }* %26, align 4
  %76 = bitcast { [4 x { [2 x i64], i64 }], i64 }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %76)
  store { [4 x { [3 x i64], i64 }], i64 } { [4 x { [3 x i64], i64 }] [{ [3 x i64], i64 } { [3 x i64] [i64 2, i64 1, i64 0], i64 2 }, { [3 x i64], i64 } { [3 x i64] [i64 1, i64 2, i64 3], i64 3 }, { [3 x i64], i64 } { [3 x i64] [i64 3, i64 0, i64 0], i64 1 }, { [3 x i64], i64 } { [3 x i64] zeroinitializer, i64 1 }], i64 4 }, { [4 x { [3 x i64], i64 }], i64 }* %25, align 4
  %77 = bitcast { [4 x { [3 x i64], i64 }], i64 }* %25 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %77)
  store { [3 x { [2 x i2], i64 }], i64 } { [3 x { [2 x i2], i64 }] [{ [2 x i2], i64 } zeroinitializer, { [2 x i2], i64 } { [2 x i2] [i2 -1, i2 0], i64 1 }, { [2 x i2], i64 } { [2 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [2 x i2], i64 }], i64 }* %24, align 4
  %78 = bitcast { [3 x { [2 x i2], i64 }], i64 }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %78)
  store { [3 x { [3 x i2], i64 }], i64 } { [3 x { [3 x i2], i64 }] [{ [3 x i2], i64 } { [3 x i2] [i2 1, i2 -2, i2 0], i64 2 }, { [3 x i2], i64 } { [3 x i2] [i2 1, i2 1, i2 1], i64 3 }, { [3 x i2], i64 } { [3 x i2] zeroinitializer, i64 1 }], i64 3 }, { [3 x { [3 x i2], i64 }], i64 }* %23, align 4
  %79 = bitcast { [3 x { [3 x i2], i64 }], i64 }* %23 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %79)
  %80 = call %Qubit* @__quantum__rt__qubit_allocate()
  %81 = call %Qubit* @__quantum__rt__qubit_allocate()
  %82 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %80, 0
  %83 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %82, 0
  %84 = insertvalue { [2 x %Qubit*], i64 } %83, i64 2, 1
  %85 = extractvalue { [2 x %Qubit*], i64 } %84, 0
  %86 = extractvalue { [2 x %Qubit*], i64 } %84, 1
  %87 = insertvalue [2 x %Qubit*] %85, %Qubit* %81, 1
  %88 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %87, 0
  %qs1 = insertvalue { [2 x %Qubit*], i64 } %88, i64 2, 1
  %89 = call %Qubit* @__quantum__rt__qubit_allocate()
  %90 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %89, 0
  %91 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %90, 0
  %qs2 = insertvalue { [1 x %Qubit*], i64 } %91, i64 1, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %92 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %93 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %92, 0
  %94 = insertvalue { [1 x %Qubit*], i64 } %93, i64 1, 1
  %95 = extractvalue { [2 x %Qubit*], i64 } %qs1, 0
  %96 = extractvalue { [2 x %Qubit*], i64 } %qs1, 1
  %97 = extractvalue { [1 x %Qubit*], i64 } %qs2, 0
  %98 = extractvalue { [1 x %Qubit*], i64 } %qs2, 1
  %99 = extractvalue { [1 x %Qubit*], i64 } %94, 0
  %100 = extractvalue { [1 x %Qubit*], i64 } %94, 1
  %101 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %80, 0
  %102 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %101, 0
  %103 = insertvalue { [2 x %Qubit*], i64 } %102, i64 2, 1
  %104 = extractvalue { [2 x %Qubit*], i64 } %103, 0
  %105 = extractvalue { [2 x %Qubit*], i64 } %103, 1
  %106 = insertvalue [2 x %Qubit*] %104, %Qubit* %81, 1
  %107 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %106, 0
  %108 = insertvalue { [2 x %Qubit*], i64 } %107, i64 2, 1
  %109 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %89, 0
  %110 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %109, 0
  %111 = insertvalue { [2 x %Qubit*], i64 } %110, i64 1, 1
  %112 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %113 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %112, 0
  %114 = insertvalue { [2 x %Qubit*], i64 } %113, i64 1, 1
  %115 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %108, 0
  %116 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %115, 0
  %117 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %116, i64 4, 1
  %118 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %117, 0
  %119 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %117, 1
  %120 = insertvalue [4 x { [2 x %Qubit*], i64 }] %118, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %121 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %120, 0
  %122 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %121, i64 4, 1
  %123 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %122, 0
  %124 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %122, 1
  %125 = insertvalue [4 x { [2 x %Qubit*], i64 }] %123, { [2 x %Qubit*], i64 } %111, 2
  %126 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %125, 0
  %127 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %126, i64 4, 1
  %128 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %127, 0
  %129 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %127, 1
  %130 = insertvalue [4 x { [2 x %Qubit*], i64 }] %128, { [2 x %Qubit*], i64 } %114, 3
  %131 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %130, 0
  %qubitArrArr = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %131, i64 4, 1
  %132 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %133 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %134 = extractvalue { [2 x %Qubit*], i64 } %108, 0
  %135 = extractvalue { [2 x %Qubit*], i64 } %108, 1
  %136 = extractvalue { [2 x %Qubit*], i64 } %111, 0
  %137 = extractvalue { [2 x %Qubit*], i64 } %111, 1
  %138 = extractvalue { [2 x %Qubit*], i64 } %114, 0
  %139 = extractvalue { [2 x %Qubit*], i64 } %114, 1
  %140 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %80, 0
  %141 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %140, 0
  %142 = insertvalue { [2 x %Qubit*], i64 } %141, i64 2, 1
  %143 = extractvalue { [2 x %Qubit*], i64 } %142, 0
  %144 = extractvalue { [2 x %Qubit*], i64 } %142, 1
  %145 = insertvalue [2 x %Qubit*] %143, %Qubit* %81, 1
  %146 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %145, 0
  %147 = insertvalue { [2 x %Qubit*], i64 } %146, i64 2, 1
  %148 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %89, 0
  %149 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %148, 0
  %150 = insertvalue { [2 x %Qubit*], i64 } %149, i64 1, 1
  %151 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %152 = insertvalue { [2 x %Qubit*], i64 } zeroinitializer, [2 x %Qubit*] %151, 0
  %153 = insertvalue { [2 x %Qubit*], i64 } %152, i64 1, 1
  %154 = insertvalue [4 x { [2 x %Qubit*], i64 }] zeroinitializer, { [2 x %Qubit*], i64 } %147, 0
  %155 = insertvalue [4 x { [2 x %Qubit*], i64 }] %154, { [2 x %Qubit*], i64 } zeroinitializer, 1
  %156 = insertvalue [4 x { [2 x %Qubit*], i64 }] %155, { [2 x %Qubit*], i64 } %150, 2
  %157 = insertvalue [4 x { [2 x %Qubit*], i64 }] %156, { [2 x %Qubit*], i64 } %153, 3
  %158 = insertvalue [4 x { [2 x %Qubit*], i64 }] %157, { [2 x %Qubit*], i64 } zeroinitializer, 0
  %159 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [2 x %Qubit*], i64 }] %158, 0
  %160 = insertvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %159, i64 4, 1
  store { [4 x { [2 x %Qubit*], i64 }], i64 } %160, { [4 x { [2 x %Qubit*], i64 }], i64 }* %22, align 8
  %161 = bitcast { [4 x { [2 x %Qubit*], i64 }], i64 }* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %161)
  %162 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %163 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %162, 0
  %164 = insertvalue { [3 x %Qubit*], i64 } %163, i64 3, 1
  %165 = extractvalue { [3 x %Qubit*], i64 } %164, 0
  %166 = extractvalue { [3 x %Qubit*], i64 } %164, 1
  %167 = insertvalue [3 x %Qubit*] %165, %Qubit* %q, 1
  %168 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %167, 0
  %169 = insertvalue { [3 x %Qubit*], i64 } %168, i64 3, 1
  %170 = extractvalue { [3 x %Qubit*], i64 } %169, 0
  %171 = extractvalue { [3 x %Qubit*], i64 } %169, 1
  %172 = insertvalue [3 x %Qubit*] %170, %Qubit* %q, 2
  %173 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %172, 0
  %174 = insertvalue { [3 x %Qubit*], i64 } %173, i64 3, 1
  %175 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 0
  %176 = extractvalue { [4 x { [2 x %Qubit*], i64 }], i64 } %qubitArrArr, 1
  %177 = extractvalue { [3 x %Qubit*], i64 } %174, 0
  %178 = extractvalue { [3 x %Qubit*], i64 } %174, 1
  %179 = extractvalue { [2 x %Qubit*], i64 } %108, 0
  %180 = extractvalue { [2 x %Qubit*], i64 } %108, 1
  %181 = extractvalue { [2 x %Qubit*], i64 } %111, 0
  %182 = extractvalue { [2 x %Qubit*], i64 } %111, 1
  %183 = extractvalue { [2 x %Qubit*], i64 } %114, 0
  %184 = extractvalue { [2 x %Qubit*], i64 } %114, 1
  %185 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %186 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %185, 0
  %187 = insertvalue { [3 x %Qubit*], i64 } %186, i64 3, 1
  %188 = extractvalue { [3 x %Qubit*], i64 } %187, 0
  %189 = extractvalue { [3 x %Qubit*], i64 } %187, 1
  %190 = insertvalue [3 x %Qubit*] %188, %Qubit* %q, 1
  %191 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %190, 0
  %192 = insertvalue { [3 x %Qubit*], i64 } %191, i64 3, 1
  %193 = extractvalue { [3 x %Qubit*], i64 } %192, 0
  %194 = extractvalue { [3 x %Qubit*], i64 } %192, 1
  %195 = insertvalue [3 x %Qubit*] %193, %Qubit* %q, 2
  %196 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %195, 0
  %197 = insertvalue { [3 x %Qubit*], i64 } %196, i64 3, 1
  %198 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %80, 0
  %199 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %198, 0
  %200 = insertvalue { [3 x %Qubit*], i64 } %199, i64 2, 1
  %201 = extractvalue { [3 x %Qubit*], i64 } %200, 0
  %202 = extractvalue { [3 x %Qubit*], i64 } %200, 1
  %203 = insertvalue [3 x %Qubit*] %201, %Qubit* %81, 1
  %204 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %203, 0
  %205 = insertvalue { [3 x %Qubit*], i64 } %204, i64 2, 1
  %206 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %89, 0
  %207 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %206, 0
  %208 = insertvalue { [3 x %Qubit*], i64 } %207, i64 1, 1
  %209 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %210 = insertvalue { [3 x %Qubit*], i64 } zeroinitializer, [3 x %Qubit*] %209, 0
  %211 = insertvalue { [3 x %Qubit*], i64 } %210, i64 1, 1
  %212 = insertvalue [4 x { [3 x %Qubit*], i64 }] zeroinitializer, { [3 x %Qubit*], i64 } %205, 0
  %213 = insertvalue [4 x { [3 x %Qubit*], i64 }] %212, { [3 x %Qubit*], i64 } zeroinitializer, 1
  %214 = insertvalue [4 x { [3 x %Qubit*], i64 }] %213, { [3 x %Qubit*], i64 } %208, 2
  %215 = insertvalue [4 x { [3 x %Qubit*], i64 }] %214, { [3 x %Qubit*], i64 } %211, 3
  %216 = insertvalue [4 x { [3 x %Qubit*], i64 }] %215, { [3 x %Qubit*], i64 } %197, 1
  %217 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } zeroinitializer, [4 x { [3 x %Qubit*], i64 }] %216, 0
  %218 = insertvalue { [4 x { [3 x %Qubit*], i64 }], i64 } %217, i64 4, 1
  store { [4 x { [3 x %Qubit*], i64 }], i64 } %218, { [4 x { [3 x %Qubit*], i64 }], i64 }* %21, align 8
  %219 = bitcast { [4 x { [3 x %Qubit*], i64 }], i64 }* %21 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %219)
  %220 = call %Result* @__quantum__rt__result_get_zero()
  %221 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %220)
  br i1 %221, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %222 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %223 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %222, 1
  store { i64, %Result* } %223, { i64, %Result* }* %20, align 8
  %224 = bitcast { i64, %Result* }* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %224)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %225 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %226 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %225, 1
  store { i64, %Result* } %226, { i64, %Result* }* %19, align 8
  %227 = bitcast { i64, %Result* }* %19 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %227)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %228 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %229 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %228, 1
  store { i64, %Result* } %229, { i64, %Result* }* %18, align 8
  %230 = bitcast { i64, %Result* }* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %230)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %231 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %232 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %231, 1
  store { i64, %Result* } %232, { i64, %Result* }* %17, align 8
  %233 = bitcast { i64, %Result* }* %17 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %233)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %234 = call %Result* @__quantum__rt__result_get_zero()
  %235 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %234)
  br i1 %235, label %then0__1, label %else__1

then0__1:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %236 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %237 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %236, 1
  store { i64, %Result* } %237, { i64, %Result* }* %16, align 8
  %238 = bitcast { i64, %Result* }* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %238)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %239 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %240 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %239, 1
  store { i64, %Result* } %240, { i64, %Result* }* %15, align 8
  %241 = bitcast { i64, %Result* }* %15 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %241)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__1
  %242 = call %Result* @__quantum__rt__result_get_zero()
  %243 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %242)
  br i1 %243, label %then0__2, label %test1__1

then0__2:                                         ; preds = %continue__4
  %q__7 = call %Qubit* @__quantum__rt__qubit_allocate()
  %244 = call %Result* @__quantum__qis__m__body(%Qubit* %q__7)
  %245 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %244, 1
  store { i64, %Result* } %245, { i64, %Result* }* %14, align 8
  %246 = bitcast { i64, %Result* }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %246)
  call void @__quantum__rt__qubit_release(%Qubit* %q__7)
  br label %continue__5

test1__1:                                         ; preds = %continue__4
  br label %continue__5

continue__5:                                      ; preds = %test1__1, %then0__2
  %247 = call %Result* @__quantum__rt__result_get_zero()
  %248 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %247)
  br i1 %248, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %continue__5
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %continue__5
  br label %continue__6

continue__6:                                      ; preds = %condContinue__1
  %249 = call %Result* @__quantum__rt__result_get_zero()
  %250 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %249)
  br i1 %250, label %condContinue__2, label %condFalse__1

condFalse__1:                                     ; preds = %continue__6
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__1, %continue__6
  br i1 %250, label %then0__3, label %continue__7

then0__3:                                         ; preds = %condContinue__2
  %q__8 = call %Qubit* @__quantum__rt__qubit_allocate()
  %251 = call %Result* @__quantum__qis__m__body(%Qubit* %q__8)
  %252 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %251, 1
  store { i64, %Result* } %252, { i64, %Result* }* %13, align 8
  %253 = bitcast { i64, %Result* }* %13 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %253)
  call void @__quantum__rt__qubit_release(%Qubit* %q__8)
  br label %continue__7

continue__7:                                      ; preds = %then0__3, %condContinue__2
  %254 = call %Result* @__quantum__rt__result_get_zero()
  %255 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %254)
  br i1 %255, label %then0__4, label %test1__2

then0__4:                                         ; preds = %continue__7
  %q__9 = call %Qubit* @__quantum__rt__qubit_allocate()
  %256 = call %Result* @__quantum__qis__m__body(%Qubit* %q__9)
  %257 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %256, 1
  store { i64, %Result* } %257, { i64, %Result* }* %12, align 8
  %258 = bitcast { i64, %Result* }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %258)
  call void @__quantum__rt__qubit_release(%Qubit* %q__9)
  br label %continue__8

test1__2:                                         ; preds = %continue__7
  %q__10 = call %Qubit* @__quantum__rt__qubit_allocate()
  %259 = call %Result* @__quantum__qis__m__body(%Qubit* %q__10)
  %260 = insertvalue { i64, %Result* } { i64 15, %Result* null }, %Result* %259, 1
  store { i64, %Result* } %260, { i64, %Result* }* %11, align 8
  %261 = bitcast { i64, %Result* }* %11 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %261)
  call void @__quantum__rt__qubit_release(%Qubit* %q__10)
  br label %continue__8

continue__8:                                      ; preds = %test1__2, %then0__4
  %262 = call %Result* @__quantum__rt__result_get_zero()
  %263 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %262)
  br i1 %263, label %condTrue__2, label %condContinue__3

condTrue__2:                                      ; preds = %continue__8
  br label %condContinue__3

condContinue__3:                                  ; preds = %condTrue__2, %continue__8
  br i1 %263, label %then0__5, label %continue__9

then0__5:                                         ; preds = %condContinue__3
  %q__11 = call %Qubit* @__quantum__rt__qubit_allocate()
  %264 = call %Result* @__quantum__qis__m__body(%Qubit* %q__11)
  %265 = insertvalue { i64, %Result* } { i64 16, %Result* null }, %Result* %264, 1
  store { i64, %Result* } %265, { i64, %Result* }* %10, align 8
  %266 = bitcast { i64, %Result* }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %266)
  call void @__quantum__rt__qubit_release(%Qubit* %q__11)
  br label %continue__9

continue__9:                                      ; preds = %then0__5, %condContinue__3
  %267 = call %Result* @__quantum__rt__result_get_zero()
  %268 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %267)
  br i1 %268, label %condContinue__4, label %condFalse__2

condFalse__2:                                     ; preds = %continue__9
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__2, %continue__9
  %q__12 = call %Qubit* @__quantum__rt__qubit_allocate()
  %269 = call %Result* @__quantum__qis__m__body(%Qubit* %q__12)
  %270 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %269, 1
  store { i64, %Result* } %270, { i64, %Result* }* %9, align 8
  %271 = bitcast { i64, %Result* }* %9 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %271)
  call void @__quantum__rt__qubit_release(%Qubit* %q__12)
  br label %continue__10

continue__10:                                     ; preds = %condContinue__4
  %q__13 = call %Qubit* @__quantum__rt__qubit_allocate()
  %272 = call %Result* @__quantum__qis__m__body(%Qubit* %q__13)
  %273 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %272, 1
  store { i64, %Result* } %273, { i64, %Result* }* %8, align 8
  %274 = bitcast { i64, %Result* }* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %274)
  call void @__quantum__rt__qubit_release(%Qubit* %q__13)
  br label %continue__11

continue__11:                                     ; preds = %continue__10
  br label %continue__12

continue__12:                                     ; preds = %continue__11
  %q__14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %275 = call %Result* @__quantum__qis__m__body(%Qubit* %q__14)
  %276 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %275, 1
  store { i64, %Result* } %276, { i64, %Result* }* %7, align 8
  %277 = bitcast { i64, %Result* }* %7 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %277)
  call void @__quantum__rt__qubit_release(%Qubit* %q__14)
  br label %continue__13

continue__13:                                     ; preds = %continue__12
  %q__15 = call %Qubit* @__quantum__rt__qubit_allocate()
  %278 = call %Result* @__quantum__qis__m__body(%Qubit* %q__15)
  %279 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %278, 1
  store { i64, %Result* } %279, { i64, %Result* }* %6, align 8
  %280 = bitcast { i64, %Result* }* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %280)
  call void @__quantum__rt__qubit_release(%Qubit* %q__15)
  br label %continue__14

continue__14:                                     ; preds = %continue__13
  %q__16 = call %Qubit* @__quantum__rt__qubit_allocate()
  %281 = call %Result* @__quantum__qis__m__body(%Qubit* %q__16)
  %282 = insertvalue { i64, %Result* } { i64 20, %Result* null }, %Result* %281, 1
  store { i64, %Result* } %282, { i64, %Result* }* %5, align 8
  %283 = bitcast { i64, %Result* }* %5 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %283)
  call void @__quantum__rt__qubit_release(%Qubit* %q__16)
  br label %continue__15

continue__15:                                     ; preds = %continue__14
  %q__17 = call %Qubit* @__quantum__rt__qubit_allocate()
  %284 = call %Result* @__quantum__qis__m__body(%Qubit* %q__17)
  %285 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %284, 1
  store { i64, %Result* } %285, { i64, %Result* }* %4, align 8
  %286 = bitcast { i64, %Result* }* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %286)
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
  %287 = call %Result* @__quantum__qis__m__body(%Qubit* %q__18)
  %288 = insertvalue { i64, %Result* } { i64 19, %Result* null }, %Result* %287, 1
  store { i64, %Result* } %288, { i64, %Result* }* %3, align 8
  %289 = bitcast { i64, %Result* }* %3 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %289)
  call void @__quantum__rt__qubit_release(%Qubit* %q__18)
  br label %continue__20

continue__20:                                     ; preds = %continue__19
  br label %continue__21

continue__21:                                     ; preds = %continue__20
  %q__19 = call %Qubit* @__quantum__rt__qubit_allocate()
  %290 = call %Result* @__quantum__qis__m__body(%Qubit* %q__19)
  %291 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %290, 1
  store { i64, %Result* } %291, { i64, %Result* }* %2, align 8
  %292 = bitcast { i64, %Result* }* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %292)
  call void @__quantum__rt__qubit_release(%Qubit* %q__19)
  br label %continue__22

continue__22:                                     ; preds = %continue__21
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %293 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %294 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %293, 0
  %qubits = insertvalue { [1 x %Qubit*], i64 } %294, i64 1, 1
  br label %continue__24

continue__24:                                     ; preds = %continue__22
  %295 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %295, i32 1)
  %296 = call %Result* @__quantum__rt__result_get_one()
  %297 = call i1 @__quantum__rt__result_equal(%Result* %295, %Result* %296)
  call void @__quantum__rt__result_update_reference_count(%Result* %295, i32 -1)
  br i1 %297, label %then0__6, label %continue__23

then0__6:                                         ; preds = %continue__24
  store i64 1, i64* %rand, align 4
  br label %continue__23

continue__23:                                     ; preds = %then0__6, %continue__24
  %298 = load i64, i64* %rand, align 4
  %299 = shl i64 %298, 1
  store i64 %299, i64* %rand, align 4
  %300 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %301 = insertvalue { [1 x %Qubit*], i64 } zeroinitializer, [1 x %Qubit*] %300, 0
  %qubits__1 = insertvalue { [1 x %Qubit*], i64 } %301, i64 1, 1
  br label %continue__26

continue__26:                                     ; preds = %continue__23
  %302 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %302, i32 1)
  %303 = call %Result* @__quantum__rt__result_get_one()
  %304 = call i1 @__quantum__rt__result_equal(%Result* %302, %Result* %303)
  call void @__quantum__rt__result_update_reference_count(%Result* %302, i32 -1)
  br i1 %304, label %then0__7, label %continue__25

then0__7:                                         ; preds = %continue__26
  %305 = add i64 %299, 1
  store i64 %305, i64* %rand, align 4
  br label %continue__25

continue__25:                                     ; preds = %then0__7, %continue__26
  %306 = call %Result* @__quantum__rt__result_get_zero()
  %a = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %306)
  %a__1 = call %Result* @__quantum__rt__result_get_zero()
  %307 = call %Result* @__quantum__rt__result_get_one()
  %308 = call i1 @__quantum__rt__result_equal(%Result* %a__1, %Result* %307)
  %c = or i1 %308, %a
  %309 = insertvalue { i1, i1 } zeroinitializer, i1 %a, 0
  %310 = insertvalue { i1, i1 } %309, i1 %c, 1
  store { i1, i1 } %310, { i1, i1 }* %1, align 1
  %311 = bitcast { i1, i1 }* %1 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %311)
  %312 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %m2)
  %313 = select i1 %312, i64 6, i64 0
  store i64 %313, i64* %foo, align 4
  %314 = call %Result* @__quantum__rt__result_get_zero()
  %315 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %314)
  br i1 %315, label %then0__8, label %else__2

then0__8:                                         ; preds = %continue__25
  store i64 0, i64* %bar, align 4
  %316 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %317 = call %Result* @__quantum__rt__result_get_one()
  %318 = call i1 @__quantum__rt__result_equal(%Result* %316, %Result* %317)
  %319 = select i1 %318, i64 1, i64 0
  %320 = add i64 0, %319
  store i64 %320, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %316, i32 -1)
  %321 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %322 = call %Result* @__quantum__rt__result_get_one()
  %323 = call i1 @__quantum__rt__result_equal(%Result* %321, %Result* %322)
  %324 = select i1 %323, i64 1, i64 0
  %325 = add i64 %320, %324
  store i64 %325, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %321, i32 -1)
  store i64 %325, i64* %foo, align 4
  br label %continue__27

else__2:                                          ; preds = %continue__25
  store i64 0, i64* %bar__1, align 4
  %326 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %327 = call %Result* @__quantum__rt__result_get_zero()
  %328 = call i1 @__quantum__rt__result_equal(%Result* %326, %Result* %327)
  %329 = select i1 %328, i64 1, i64 0
  %330 = add i64 0, %329
  store i64 %330, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %326, i32 -1)
  %331 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %332 = call %Result* @__quantum__rt__result_get_zero()
  %333 = call i1 @__quantum__rt__result_equal(%Result* %331, %Result* %332)
  %334 = select i1 %333, i64 1, i64 0
  %335 = add i64 %330, %334
  store i64 %335, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %331, i32 -1)
  store i64 %335, i64* %foo, align 4
  br label %continue__27

continue__27:                                     ; preds = %else__2, %then0__8
  %qubit__31 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__26 = call %Qubit* @__quantum__rt__qubit_allocate()
  %336 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__30 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__27 = call %Qubit* @__quantum__rt__qubit_allocate()
  %337 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__31, 0
  %338 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %337, 0
  %339 = insertvalue { [5 x %Qubit*], i64 } %338, i64 5, 1
  %340 = extractvalue { [5 x %Qubit*], i64 } %339, 0
  %341 = extractvalue { [5 x %Qubit*], i64 } %339, 1
  %342 = insertvalue [5 x %Qubit*] %340, %Qubit* %qubit__26, 1
  %343 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %342, 0
  %344 = insertvalue { [5 x %Qubit*], i64 } %343, i64 5, 1
  %345 = extractvalue { [5 x %Qubit*], i64 } %344, 0
  %346 = extractvalue { [5 x %Qubit*], i64 } %344, 1
  %347 = insertvalue [5 x %Qubit*] %345, %Qubit* %336, 2
  %348 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %347, 0
  %349 = insertvalue { [5 x %Qubit*], i64 } %348, i64 5, 1
  %350 = extractvalue { [5 x %Qubit*], i64 } %349, 0
  %351 = extractvalue { [5 x %Qubit*], i64 } %349, 1
  %352 = insertvalue [5 x %Qubit*] %350, %Qubit* %qubit__30, 3
  %353 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %352, 0
  %354 = insertvalue { [5 x %Qubit*], i64 } %353, i64 5, 1
  %355 = extractvalue { [5 x %Qubit*], i64 } %354, 0
  %356 = extractvalue { [5 x %Qubit*], i64 } %354, 1
  %357 = insertvalue [5 x %Qubit*] %355, %Qubit* %qubit__27, 4
  %358 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %357, 0
  %q__20 = insertvalue { [5 x %Qubit*], i64 } %358, i64 5, 1
  %r1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__26)
  %r2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__27)
  %r3 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %r4 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %359 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %360 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %359, 0
  %361 = insertvalue { [5 x %Qubit*], i64 } %360, i64 5, 1
  %362 = extractvalue { [5 x %Qubit*], i64 } %361, 0
  %363 = extractvalue { [5 x %Qubit*], i64 } %361, 1
  %364 = insertvalue [5 x %Qubit*] %362, %Qubit* %qubit__30, 1
  %365 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %364, 0
  %366 = insertvalue { [5 x %Qubit*], i64 } %365, i64 5, 1
  %367 = extractvalue { [5 x %Qubit*], i64 } %366, 0
  %368 = extractvalue { [5 x %Qubit*], i64 } %366, 1
  %369 = insertvalue [5 x %Qubit*] %367, %Qubit* %336, 2
  %370 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %369, 0
  %371 = insertvalue { [5 x %Qubit*], i64 } %370, i64 5, 1
  %372 = extractvalue { [5 x %Qubit*], i64 } %371, 0
  %373 = extractvalue { [5 x %Qubit*], i64 } %371, 1
  %374 = insertvalue [5 x %Qubit*] %372, %Qubit* %qubit__26, 3
  %375 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %374, 0
  %376 = insertvalue { [5 x %Qubit*], i64 } %375, i64 5, 1
  %377 = extractvalue { [5 x %Qubit*], i64 } %376, 0
  %378 = extractvalue { [5 x %Qubit*], i64 } %376, 1
  %379 = insertvalue [5 x %Qubit*] %377, %Qubit* %qubit__31, 4
  %380 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %379, 0
  %381 = insertvalue { [5 x %Qubit*], i64 } %380, i64 5, 1
  %r5 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__30)
  %382 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %383 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %382, 0
  %384 = insertvalue { [5 x %Qubit*], i64 } %383, i64 5, 1
  %385 = extractvalue { [5 x %Qubit*], i64 } %384, 0
  %386 = extractvalue { [5 x %Qubit*], i64 } %384, 1
  %387 = insertvalue [5 x %Qubit*] %385, %Qubit* %qubit__30, 1
  %388 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %387, 0
  %389 = insertvalue { [5 x %Qubit*], i64 } %388, i64 5, 1
  %390 = extractvalue { [5 x %Qubit*], i64 } %389, 0
  %391 = extractvalue { [5 x %Qubit*], i64 } %389, 1
  %392 = insertvalue [5 x %Qubit*] %390, %Qubit* %336, 2
  %393 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %392, 0
  %394 = insertvalue { [5 x %Qubit*], i64 } %393, i64 5, 1
  %395 = extractvalue { [5 x %Qubit*], i64 } %394, 0
  %396 = extractvalue { [5 x %Qubit*], i64 } %394, 1
  %397 = insertvalue [5 x %Qubit*] %395, %Qubit* %qubit__26, 3
  %398 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %397, 0
  %399 = insertvalue { [5 x %Qubit*], i64 } %398, i64 5, 1
  %400 = extractvalue { [5 x %Qubit*], i64 } %399, 0
  %401 = extractvalue { [5 x %Qubit*], i64 } %399, 1
  %402 = insertvalue [5 x %Qubit*] %400, %Qubit* %qubit__31, 4
  %403 = insertvalue { [5 x %Qubit*], i64 } zeroinitializer, [5 x %Qubit*] %402, 0
  %z2 = insertvalue { [5 x %Qubit*], i64 } %403, i64 5, 1
  %r6 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__31)
  call void @__quantum__rt__result_update_reference_count(%Result* %r1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r2, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r6, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__31)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__26)
  call void @__quantum__rt__qubit_release(%Qubit* %336)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__30)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__27)
  %404 = insertvalue [1 x %Result*] zeroinitializer, %Result* %m1, 0
  %405 = insertvalue { [1 x %Result*], i64 } zeroinitializer, [1 x %Result*] %404, 0
  %arr3 = insertvalue { [1 x %Result*], i64 } %405, i64 1, 1
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 1)
  %406 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %m1)
  %407 = xor i1 %406, true
  br i1 %407, label %then0__9, label %continue__28

then0__9:                                         ; preds = %continue__27
  %408 = extractvalue { [1 x %Result*], i64 } %arr3, 0
  %409 = extractvalue { [1 x %Result*], i64 } %arr3, 1
  %410 = insertvalue [1 x %Result*] %408, %Result* %m2, 0
  %411 = insertvalue { [1 x %Result*], i64 } zeroinitializer, [1 x %Result*] %410, 0
  %412 = insertvalue { [1 x %Result*], i64 } %411, i64 1, 1
  store { [1 x %Result*], i64 } %412, { [1 x %Result*], i64 }* %0, align 8
  %413 = bitcast { [1 x %Result*], i64 }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %413)
  br label %continue__28

continue__28:                                     ; preds = %then0__9, %continue__27
  %__rtrnVal1__ = load i64, i64* %rand, align 4
  %414 = insertvalue { i64, i64 } { i64 6, i64 0 }, i64 %__rtrnVal1__, 1
  call void @__quantum__rt__qubit_release(%Qubit* %80)
  call void @__quantum__rt__qubit_release(%Qubit* %81)
  call void @__quantum__rt__qubit_release(%Qubit* %89)
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
