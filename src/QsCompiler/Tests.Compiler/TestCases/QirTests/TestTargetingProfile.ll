
%Result = type opaque
%Qubit = type opaque
%Array = type opaque

define void @Microsoft__Quantum__Testing__QIR__TestProfileTargeting() #0 {
entry:
  %0 = alloca { i64, { [1 x %Result*] } }, align 8
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
  %21 = alloca { i64, { [4 x { i64, { [3 x %Qubit*] } }] } }, align 8
  %22 = alloca { i64, { [4 x { i64, { [2 x %Qubit*] } }] } }, align 8
  %23 = alloca { i64, { [3 x { i64, { [3 x i2] } }] } }, align 8
  %24 = alloca { i64, { [3 x { i64, { [2 x i2] } }] } }, align 8
  %25 = alloca { i64, { [4 x { i64, { [3 x i64] } }] } }, align 8
  %26 = alloca { i64, { [4 x { i64, { [2 x i64] } }] } }, align 8
  %27 = alloca { i64, { [4 x { i64, { [2 x i64] } }] } }, align 8
  %28 = alloca { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } }, align 8
  %29 = alloca i64, align 8
  %30 = alloca { i64, { [2 x i64] } }, align 8
  %31 = alloca i64, align 8
  %32 = alloca i64, align 8
  %33 = alloca double, align 8
  %34 = alloca { { i64, double }, double }, align 8
  %35 = alloca { { i64, double }, double }, align 8
  %36 = alloca {}, align 8
  %37 = alloca { i64, double }, align 8
  %38 = alloca { { i64, double }, { i64, double } }, align 8
  %39 = alloca { i64, { [3 x i64] } }, align 8
  %40 = alloca { i64, { [3 x i64] } }, align 8
  %41 = alloca { i64, { [3 x i64] } }, align 8
  %42 = alloca { i64, { [6 x i64] } }, align 8
  %43 = alloca i64, align 8
  %44 = alloca i64, align 8
  %45 = alloca i64, align 8
  %46 = alloca { i64, { [3 x i64] } }, align 8
  %sum = alloca i64, align 8
  %47 = alloca { i64, { [3 x i64] } }, align 8
  store { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 1, i64 2, i64 3] } }, { i64, { [3 x i64] } }* %47, align 4
  %48 = bitcast { i64, { [3 x i64] } }* %47 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %48)
  store i64 0, i64* %sum, align 4
  store i64 1, i64* %sum, align 4
  store i64 3, i64* %sum, align 4
  store i64 6, i64* %sum, align 4
  store { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 6, i64 6, i64 6] } }, { i64, { [3 x i64] } }* %46, align 4
  %49 = bitcast { i64, { [3 x i64] } }* %46 to i8*
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
  store { i64, { [6 x i64] } } { i64 6, { [6 x i64] } { [6 x i64] [i64 1, i64 2, i64 3, i64 6, i64 6, i64 6] } }, { i64, { [6 x i64] } }* %42, align 4
  %53 = bitcast { i64, { [6 x i64] } }* %42 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %53)
  store { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 6, i64 6, i64 2] } }, { i64, { [3 x i64] } }* %41, align 4
  %54 = bitcast { i64, { [3 x i64] } }* %41 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %54)
  store { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 2, i64 3, i64 6] } }, { i64, { [3 x i64] } }* %40, align 4
  %55 = bitcast { i64, { [3 x i64] } }* %40 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %55)
  store { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 4, i64 2, i64 3] } }, { i64, { [3 x i64] } }* %39, align 4
  %56 = bitcast { i64, { [3 x i64] } }* %39 to i8*
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
  %64 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %63, 0
  %65 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %64, 1
  %66 = extractvalue { i64, { [2 x %Qubit*] } } %65, 1
  %67 = extractvalue { [2 x %Qubit*] } %66, 0
  %68 = insertvalue [2 x %Qubit*] %67, %Qubit* %target, 1
  %69 = insertvalue { [2 x %Qubit*] } %66, [2 x %Qubit*] %68, 0
  %70 = extractvalue { i64, { [2 x %Qubit*] } } %65, 0
  %71 = insertvalue { i64, { [2 x %Qubit*] } } zeroinitializer, i64 %70, 0
  %qs = insertvalue { i64, { [2 x %Qubit*] } } %71, { [2 x %Qubit*] } %69, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  store i64 1, i64* %32, align 4
  %72 = bitcast i64* %32 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %72)
  call void @__quantum__qis__logpauli__body(i2 -2)
  store i64 2, i64* %31, align 4
  %73 = bitcast i64* %31 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %73)
  store { i64, { [2 x i64] } } { i64 2, { [2 x i64] } { [2 x i64] [i64 1, i64 2] } }, { i64, { [2 x i64] } }* %30, align 4
  %74 = bitcast { i64, { [2 x i64] } }* %30 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %74)
  store i64 3, i64* %29, align 4
  %75 = bitcast i64* %29 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %75)
  store { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } } { { i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } zeroinitializer } }, { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } }* %28, align 4
  %76 = bitcast { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } }* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %76)
  store { i64, { [4 x { i64, { [2 x i64] } }] } } { i64 4, { [4 x { i64, { [2 x i64] } }] } { [4 x { i64, { [2 x i64] } }] [{ i64, { [2 x i64] } } { i64 2, { [2 x i64] } { [2 x i64] [i64 2, i64 1] } }, { i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } { [2 x i64] [i64 3, i64 0] } }, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } zeroinitializer }] } }, { i64, { [4 x { i64, { [2 x i64] } }] } }* %27, align 4
  %77 = bitcast { i64, { [4 x { i64, { [2 x i64] } }] } }* %27 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %77)
  call void @__quantum__qis__logpauli__body(i2 0)
  store { i64, { [4 x { i64, { [2 x i64] } }] } } { i64 4, { [4 x { i64, { [2 x i64] } }] } { [4 x { i64, { [2 x i64] } }] [{ i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } { [2 x i64] [i64 3, i64 0] } }, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } zeroinitializer }] } }, { i64, { [4 x { i64, { [2 x i64] } }] } }* %26, align 4
  %78 = bitcast { i64, { [4 x { i64, { [2 x i64] } }] } }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %78)
  store { i64, { [4 x { i64, { [3 x i64] } }] } } { i64 4, { [4 x { i64, { [3 x i64] } }] } { [4 x { i64, { [3 x i64] } }] [{ i64, { [3 x i64] } } { i64 2, { [3 x i64] } { [3 x i64] [i64 2, i64 1, i64 0] } }, { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 1, i64 2, i64 3] } }, { i64, { [3 x i64] } } { i64 1, { [3 x i64] } { [3 x i64] [i64 3, i64 0, i64 0] } }, { i64, { [3 x i64] } } { i64 1, { [3 x i64] } zeroinitializer }] } }, { i64, { [4 x { i64, { [3 x i64] } }] } }* %25, align 4
  %79 = bitcast { i64, { [4 x { i64, { [3 x i64] } }] } }* %25 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %79)
  store { i64, { [3 x { i64, { [2 x i2] } }] } } { i64 3, { [3 x { i64, { [2 x i2] } }] } { [3 x { i64, { [2 x i2] } }] [{ i64, { [2 x i2] } } zeroinitializer, { i64, { [2 x i2] } } { i64 1, { [2 x i2] } { [2 x i2] [i2 -1, i2 0] } }, { i64, { [2 x i2] } } { i64 1, { [2 x i2] } zeroinitializer }] } }, { i64, { [3 x { i64, { [2 x i2] } }] } }* %24, align 4
  %80 = bitcast { i64, { [3 x { i64, { [2 x i2] } }] } }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %80)
  store { i64, { [3 x { i64, { [3 x i2] } }] } } { i64 3, { [3 x { i64, { [3 x i2] } }] } { [3 x { i64, { [3 x i2] } }] [{ i64, { [3 x i2] } } { i64 2, { [3 x i2] } { [3 x i2] [i2 1, i2 -2, i2 0] } }, { i64, { [3 x i2] } } { i64 3, { [3 x i2] } { [3 x i2] [i2 1, i2 1, i2 1] } }, { i64, { [3 x i2] } } { i64 1, { [3 x i2] } zeroinitializer }] } }, { i64, { [3 x { i64, { [3 x i2] } }] } }* %23, align 4
  %81 = bitcast { i64, { [3 x { i64, { [3 x i2] } }] } }* %23 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %81)
  %82 = call %Qubit* @__quantum__rt__qubit_allocate()
  %83 = call %Qubit* @__quantum__rt__qubit_allocate()
  %84 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %82, 0
  %85 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %84, 0
  %86 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %85, 1
  %87 = extractvalue { i64, { [2 x %Qubit*] } } %86, 1
  %88 = extractvalue { [2 x %Qubit*] } %87, 0
  %89 = insertvalue [2 x %Qubit*] %88, %Qubit* %83, 1
  %90 = insertvalue { [2 x %Qubit*] } %87, [2 x %Qubit*] %89, 0
  %91 = extractvalue { i64, { [2 x %Qubit*] } } %86, 0
  %92 = insertvalue { i64, { [2 x %Qubit*] } } zeroinitializer, i64 %91, 0
  %qs1 = insertvalue { i64, { [2 x %Qubit*] } } %92, { [2 x %Qubit*] } %90, 1
  %93 = call %Qubit* @__quantum__rt__qubit_allocate()
  %94 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %93, 0
  %95 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %94, 0
  %qs2 = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %95, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %96 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %97 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %96, 0
  %98 = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %97, 1
  %99 = extractvalue { i64, { [2 x %Qubit*] } } %qs1, 1
  %100 = extractvalue { i64, { [1 x %Qubit*] } } %qs2, 1
  %101 = extractvalue { i64, { [1 x %Qubit*] } } %98, 1
  %102 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %82, 0
  %103 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %102, 0
  %104 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %103, 1
  %105 = extractvalue { i64, { [2 x %Qubit*] } } %104, 1
  %106 = extractvalue { [2 x %Qubit*] } %105, 0
  %107 = insertvalue [2 x %Qubit*] %106, %Qubit* %83, 1
  %108 = insertvalue { [2 x %Qubit*] } %105, [2 x %Qubit*] %107, 0
  %109 = extractvalue { i64, { [2 x %Qubit*] } } %104, 0
  %110 = insertvalue { i64, { [2 x %Qubit*] } } zeroinitializer, i64 %109, 0
  %111 = insertvalue { i64, { [2 x %Qubit*] } } %110, { [2 x %Qubit*] } %108, 1
  %112 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %93, 0
  %113 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %112, 0
  %114 = insertvalue { i64, { [2 x %Qubit*] } } { i64 1, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %113, 1
  %115 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %116 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %115, 0
  %117 = insertvalue { i64, { [2 x %Qubit*] } } { i64 1, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %116, 1
  %118 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] zeroinitializer, { i64, { [2 x %Qubit*] } } %111, 0
  %119 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer, [4 x { i64, { [2 x %Qubit*] } }] %118, 0
  %120 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [2 x %Qubit*] } }] } %119, 1
  %121 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %120, 1
  %122 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %121, 0
  %123 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %122, { i64, { [2 x %Qubit*] } } zeroinitializer, 1
  %124 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %121, [4 x { i64, { [2 x %Qubit*] } }] %123, 0
  %125 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %120, 0
  %126 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } zeroinitializer, i64 %125, 0
  %127 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %126, { [4 x { i64, { [2 x %Qubit*] } }] } %124, 1
  %128 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %127, 1
  %129 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %128, 0
  %130 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %129, { i64, { [2 x %Qubit*] } } %114, 2
  %131 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %128, [4 x { i64, { [2 x %Qubit*] } }] %130, 0
  %132 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %127, 0
  %133 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } zeroinitializer, i64 %132, 0
  %134 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %133, { [4 x { i64, { [2 x %Qubit*] } }] } %131, 1
  %135 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %134, 1
  %136 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %135, 0
  %137 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %136, { i64, { [2 x %Qubit*] } } %117, 3
  %138 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %135, [4 x { i64, { [2 x %Qubit*] } }] %137, 0
  %139 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %134, 0
  %140 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } zeroinitializer, i64 %139, 0
  %qubitArrArr = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %140, { [4 x { i64, { [2 x %Qubit*] } }] } %138, 1
  %141 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 1
  %142 = extractvalue { i64, { [2 x %Qubit*] } } %111, 1
  %143 = extractvalue { i64, { [2 x %Qubit*] } } %114, 1
  %144 = extractvalue { i64, { [2 x %Qubit*] } } %117, 1
  %145 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %82, 0
  %146 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %145, 0
  %147 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %146, 1
  %148 = extractvalue { i64, { [2 x %Qubit*] } } %147, 1
  %149 = extractvalue { [2 x %Qubit*] } %148, 0
  %150 = insertvalue [2 x %Qubit*] %149, %Qubit* %83, 1
  %151 = insertvalue { [2 x %Qubit*] } %148, [2 x %Qubit*] %150, 0
  %152 = extractvalue { i64, { [2 x %Qubit*] } } %147, 0
  %153 = insertvalue { i64, { [2 x %Qubit*] } } zeroinitializer, i64 %152, 0
  %154 = insertvalue { i64, { [2 x %Qubit*] } } %153, { [2 x %Qubit*] } %151, 1
  %155 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %93, 0
  %156 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %155, 0
  %157 = insertvalue { i64, { [2 x %Qubit*] } } { i64 1, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %156, 1
  %158 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %159 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %158, 0
  %160 = insertvalue { i64, { [2 x %Qubit*] } } { i64 1, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %159, 1
  %161 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] zeroinitializer, { i64, { [2 x %Qubit*] } } %154, 0
  %162 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %161, { i64, { [2 x %Qubit*] } } zeroinitializer, 1
  %163 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %162, { i64, { [2 x %Qubit*] } } %157, 2
  %164 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %163, { i64, { [2 x %Qubit*] } } %160, 3
  %165 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer, [4 x { i64, { [2 x %Qubit*] } }] %164, 0
  %166 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %165, 0
  %167 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %166, { i64, { [2 x %Qubit*] } } zeroinitializer, 0
  %168 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %165, [4 x { i64, { [2 x %Qubit*] } }] %167, 0
  %169 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 0
  %170 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } zeroinitializer, i64 %169, 0
  %171 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %170, { [4 x { i64, { [2 x %Qubit*] } }] } %168, 1
  store { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %171, { i64, { [4 x { i64, { [2 x %Qubit*] } }] } }* %22, align 8
  %172 = bitcast { i64, { [4 x { i64, { [2 x %Qubit*] } }] } }* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %172)
  %173 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %174 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %173, 0
  %175 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %174, 1
  %176 = extractvalue { i64, { [3 x %Qubit*] } } %175, 1
  %177 = extractvalue { [3 x %Qubit*] } %176, 0
  %178 = insertvalue [3 x %Qubit*] %177, %Qubit* %q, 1
  %179 = insertvalue { [3 x %Qubit*] } %176, [3 x %Qubit*] %178, 0
  %180 = extractvalue { i64, { [3 x %Qubit*] } } %175, 0
  %181 = insertvalue { i64, { [3 x %Qubit*] } } zeroinitializer, i64 %180, 0
  %182 = insertvalue { i64, { [3 x %Qubit*] } } %181, { [3 x %Qubit*] } %179, 1
  %183 = extractvalue { i64, { [3 x %Qubit*] } } %182, 1
  %184 = extractvalue { [3 x %Qubit*] } %183, 0
  %185 = insertvalue [3 x %Qubit*] %184, %Qubit* %q, 2
  %186 = insertvalue { [3 x %Qubit*] } %183, [3 x %Qubit*] %185, 0
  %187 = extractvalue { i64, { [3 x %Qubit*] } } %182, 0
  %188 = insertvalue { i64, { [3 x %Qubit*] } } zeroinitializer, i64 %187, 0
  %189 = insertvalue { i64, { [3 x %Qubit*] } } %188, { [3 x %Qubit*] } %186, 1
  %190 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 1
  %191 = extractvalue { i64, { [3 x %Qubit*] } } %189, 1
  %192 = extractvalue { i64, { [2 x %Qubit*] } } %111, 1
  %193 = extractvalue { i64, { [2 x %Qubit*] } } %114, 1
  %194 = extractvalue { i64, { [2 x %Qubit*] } } %117, 1
  %195 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %196 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %195, 0
  %197 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %196, 1
  %198 = extractvalue { i64, { [3 x %Qubit*] } } %197, 1
  %199 = extractvalue { [3 x %Qubit*] } %198, 0
  %200 = insertvalue [3 x %Qubit*] %199, %Qubit* %q, 1
  %201 = insertvalue { [3 x %Qubit*] } %198, [3 x %Qubit*] %200, 0
  %202 = extractvalue { i64, { [3 x %Qubit*] } } %197, 0
  %203 = insertvalue { i64, { [3 x %Qubit*] } } zeroinitializer, i64 %202, 0
  %204 = insertvalue { i64, { [3 x %Qubit*] } } %203, { [3 x %Qubit*] } %201, 1
  %205 = extractvalue { i64, { [3 x %Qubit*] } } %204, 1
  %206 = extractvalue { [3 x %Qubit*] } %205, 0
  %207 = insertvalue [3 x %Qubit*] %206, %Qubit* %q, 2
  %208 = insertvalue { [3 x %Qubit*] } %205, [3 x %Qubit*] %207, 0
  %209 = extractvalue { i64, { [3 x %Qubit*] } } %204, 0
  %210 = insertvalue { i64, { [3 x %Qubit*] } } zeroinitializer, i64 %209, 0
  %211 = insertvalue { i64, { [3 x %Qubit*] } } %210, { [3 x %Qubit*] } %208, 1
  %212 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %82, 0
  %213 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %212, 0
  %214 = insertvalue { i64, { [3 x %Qubit*] } } { i64 2, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %213, 1
  %215 = extractvalue { i64, { [3 x %Qubit*] } } %214, 1
  %216 = extractvalue { [3 x %Qubit*] } %215, 0
  %217 = insertvalue [3 x %Qubit*] %216, %Qubit* %83, 1
  %218 = insertvalue { [3 x %Qubit*] } %215, [3 x %Qubit*] %217, 0
  %219 = extractvalue { i64, { [3 x %Qubit*] } } %214, 0
  %220 = insertvalue { i64, { [3 x %Qubit*] } } zeroinitializer, i64 %219, 0
  %221 = insertvalue { i64, { [3 x %Qubit*] } } %220, { [3 x %Qubit*] } %218, 1
  %222 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %93, 0
  %223 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %222, 0
  %224 = insertvalue { i64, { [3 x %Qubit*] } } { i64 1, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %223, 1
  %225 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %226 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %225, 0
  %227 = insertvalue { i64, { [3 x %Qubit*] } } { i64 1, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %226, 1
  %228 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] zeroinitializer, { i64, { [3 x %Qubit*] } } %221, 0
  %229 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %228, { i64, { [3 x %Qubit*] } } zeroinitializer, 1
  %230 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %229, { i64, { [3 x %Qubit*] } } %224, 2
  %231 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %230, { i64, { [3 x %Qubit*] } } %227, 3
  %232 = insertvalue { [4 x { i64, { [3 x %Qubit*] } }] } zeroinitializer, [4 x { i64, { [3 x %Qubit*] } }] %231, 0
  %233 = extractvalue { [4 x { i64, { [3 x %Qubit*] } }] } %232, 0
  %234 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %233, { i64, { [3 x %Qubit*] } } %211, 1
  %235 = insertvalue { [4 x { i64, { [3 x %Qubit*] } }] } %232, [4 x { i64, { [3 x %Qubit*] } }] %234, 0
  %236 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 0
  %237 = insertvalue { i64, { [4 x { i64, { [3 x %Qubit*] } }] } } zeroinitializer, i64 %236, 0
  %238 = insertvalue { i64, { [4 x { i64, { [3 x %Qubit*] } }] } } %237, { [4 x { i64, { [3 x %Qubit*] } }] } %235, 1
  store { i64, { [4 x { i64, { [3 x %Qubit*] } }] } } %238, { i64, { [4 x { i64, { [3 x %Qubit*] } }] } }* %21, align 8
  %239 = bitcast { i64, { [4 x { i64, { [3 x %Qubit*] } }] } }* %21 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %239)
  %240 = call %Result* @__quantum__rt__result_get_zero()
  %241 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %240)
  br i1 %241, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %242 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %243 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %242, 1
  store { i64, %Result* } %243, { i64, %Result* }* %20, align 8
  %244 = bitcast { i64, %Result* }* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %244)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %245 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %246 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %245, 1
  store { i64, %Result* } %246, { i64, %Result* }* %19, align 8
  %247 = bitcast { i64, %Result* }* %19 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %247)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %248 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %249 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %248, 1
  store { i64, %Result* } %249, { i64, %Result* }* %18, align 8
  %250 = bitcast { i64, %Result* }* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %250)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %251 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %252 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %251, 1
  store { i64, %Result* } %252, { i64, %Result* }* %17, align 8
  %253 = bitcast { i64, %Result* }* %17 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %253)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %254 = call %Result* @__quantum__rt__result_get_zero()
  %255 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %254)
  br i1 %255, label %then0__1, label %else__1

then0__1:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %256 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %257 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %256, 1
  store { i64, %Result* } %257, { i64, %Result* }* %16, align 8
  %258 = bitcast { i64, %Result* }* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %258)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %259 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %260 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %259, 1
  store { i64, %Result* } %260, { i64, %Result* }* %15, align 8
  %261 = bitcast { i64, %Result* }* %15 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %261)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__1
  %262 = call %Result* @__quantum__rt__result_get_zero()
  %263 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %262)
  br i1 %263, label %then0__2, label %test1__1

then0__2:                                         ; preds = %continue__4
  %q__7 = call %Qubit* @__quantum__rt__qubit_allocate()
  %264 = call %Result* @__quantum__qis__m__body(%Qubit* %q__7)
  %265 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %264, 1
  store { i64, %Result* } %265, { i64, %Result* }* %14, align 8
  %266 = bitcast { i64, %Result* }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %266)
  call void @__quantum__rt__qubit_release(%Qubit* %q__7)
  br label %continue__5

test1__1:                                         ; preds = %continue__4
  br label %continue__5

continue__5:                                      ; preds = %test1__1, %then0__2
  %267 = call %Result* @__quantum__rt__result_get_zero()
  %268 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %267)
  br i1 %268, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %continue__5
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %continue__5
  br label %continue__6

continue__6:                                      ; preds = %condContinue__1
  %269 = call %Result* @__quantum__rt__result_get_zero()
  %270 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %269)
  br i1 %270, label %condContinue__2, label %condFalse__1

condFalse__1:                                     ; preds = %continue__6
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__1, %continue__6
  br i1 %270, label %then0__3, label %continue__7

then0__3:                                         ; preds = %condContinue__2
  %q__8 = call %Qubit* @__quantum__rt__qubit_allocate()
  %271 = call %Result* @__quantum__qis__m__body(%Qubit* %q__8)
  %272 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %271, 1
  store { i64, %Result* } %272, { i64, %Result* }* %13, align 8
  %273 = bitcast { i64, %Result* }* %13 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %273)
  call void @__quantum__rt__qubit_release(%Qubit* %q__8)
  br label %continue__7

continue__7:                                      ; preds = %then0__3, %condContinue__2
  %274 = call %Result* @__quantum__rt__result_get_zero()
  %275 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %274)
  br i1 %275, label %then0__4, label %test1__2

then0__4:                                         ; preds = %continue__7
  %q__9 = call %Qubit* @__quantum__rt__qubit_allocate()
  %276 = call %Result* @__quantum__qis__m__body(%Qubit* %q__9)
  %277 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %276, 1
  store { i64, %Result* } %277, { i64, %Result* }* %12, align 8
  %278 = bitcast { i64, %Result* }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %278)
  call void @__quantum__rt__qubit_release(%Qubit* %q__9)
  br label %continue__8

test1__2:                                         ; preds = %continue__7
  %q__10 = call %Qubit* @__quantum__rt__qubit_allocate()
  %279 = call %Result* @__quantum__qis__m__body(%Qubit* %q__10)
  %280 = insertvalue { i64, %Result* } { i64 15, %Result* null }, %Result* %279, 1
  store { i64, %Result* } %280, { i64, %Result* }* %11, align 8
  %281 = bitcast { i64, %Result* }* %11 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %281)
  call void @__quantum__rt__qubit_release(%Qubit* %q__10)
  br label %continue__8

continue__8:                                      ; preds = %test1__2, %then0__4
  %282 = call %Result* @__quantum__rt__result_get_zero()
  %283 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %282)
  br i1 %283, label %condTrue__2, label %condContinue__3

condTrue__2:                                      ; preds = %continue__8
  br label %condContinue__3

condContinue__3:                                  ; preds = %condTrue__2, %continue__8
  br i1 %283, label %then0__5, label %continue__9

then0__5:                                         ; preds = %condContinue__3
  %q__11 = call %Qubit* @__quantum__rt__qubit_allocate()
  %284 = call %Result* @__quantum__qis__m__body(%Qubit* %q__11)
  %285 = insertvalue { i64, %Result* } { i64 16, %Result* null }, %Result* %284, 1
  store { i64, %Result* } %285, { i64, %Result* }* %10, align 8
  %286 = bitcast { i64, %Result* }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %286)
  call void @__quantum__rt__qubit_release(%Qubit* %q__11)
  br label %continue__9

continue__9:                                      ; preds = %then0__5, %condContinue__3
  %287 = call %Result* @__quantum__rt__result_get_zero()
  %288 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %287)
  br i1 %288, label %condContinue__4, label %condFalse__2

condFalse__2:                                     ; preds = %continue__9
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__2, %continue__9
  %q__12 = call %Qubit* @__quantum__rt__qubit_allocate()
  %289 = call %Result* @__quantum__qis__m__body(%Qubit* %q__12)
  %290 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %289, 1
  store { i64, %Result* } %290, { i64, %Result* }* %9, align 8
  %291 = bitcast { i64, %Result* }* %9 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %291)
  call void @__quantum__rt__qubit_release(%Qubit* %q__12)
  br label %continue__10

continue__10:                                     ; preds = %condContinue__4
  %q__13 = call %Qubit* @__quantum__rt__qubit_allocate()
  %292 = call %Result* @__quantum__qis__m__body(%Qubit* %q__13)
  %293 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %292, 1
  store { i64, %Result* } %293, { i64, %Result* }* %8, align 8
  %294 = bitcast { i64, %Result* }* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %294)
  call void @__quantum__rt__qubit_release(%Qubit* %q__13)
  br label %continue__11

continue__11:                                     ; preds = %continue__10
  br label %continue__12

continue__12:                                     ; preds = %continue__11
  %q__14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %295 = call %Result* @__quantum__qis__m__body(%Qubit* %q__14)
  %296 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %295, 1
  store { i64, %Result* } %296, { i64, %Result* }* %7, align 8
  %297 = bitcast { i64, %Result* }* %7 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %297)
  call void @__quantum__rt__qubit_release(%Qubit* %q__14)
  br label %continue__13

continue__13:                                     ; preds = %continue__12
  %q__15 = call %Qubit* @__quantum__rt__qubit_allocate()
  %298 = call %Result* @__quantum__qis__m__body(%Qubit* %q__15)
  %299 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %298, 1
  store { i64, %Result* } %299, { i64, %Result* }* %6, align 8
  %300 = bitcast { i64, %Result* }* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %300)
  call void @__quantum__rt__qubit_release(%Qubit* %q__15)
  br label %continue__14

continue__14:                                     ; preds = %continue__13
  %q__16 = call %Qubit* @__quantum__rt__qubit_allocate()
  %301 = call %Result* @__quantum__qis__m__body(%Qubit* %q__16)
  %302 = insertvalue { i64, %Result* } { i64 20, %Result* null }, %Result* %301, 1
  store { i64, %Result* } %302, { i64, %Result* }* %5, align 8
  %303 = bitcast { i64, %Result* }* %5 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %303)
  call void @__quantum__rt__qubit_release(%Qubit* %q__16)
  br label %continue__15

continue__15:                                     ; preds = %continue__14
  %q__17 = call %Qubit* @__quantum__rt__qubit_allocate()
  %304 = call %Result* @__quantum__qis__m__body(%Qubit* %q__17)
  %305 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %304, 1
  store { i64, %Result* } %305, { i64, %Result* }* %4, align 8
  %306 = bitcast { i64, %Result* }* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %306)
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
  %307 = call %Result* @__quantum__qis__m__body(%Qubit* %q__18)
  %308 = insertvalue { i64, %Result* } { i64 19, %Result* null }, %Result* %307, 1
  store { i64, %Result* } %308, { i64, %Result* }* %3, align 8
  %309 = bitcast { i64, %Result* }* %3 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %309)
  call void @__quantum__rt__qubit_release(%Qubit* %q__18)
  br label %continue__20

continue__20:                                     ; preds = %continue__19
  br label %continue__21

continue__21:                                     ; preds = %continue__20
  %q__19 = call %Qubit* @__quantum__rt__qubit_allocate()
  %310 = call %Result* @__quantum__qis__m__body(%Qubit* %q__19)
  %311 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %310, 1
  store { i64, %Result* } %311, { i64, %Result* }* %2, align 8
  %312 = bitcast { i64, %Result* }* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %312)
  call void @__quantum__rt__qubit_release(%Qubit* %q__19)
  br label %continue__22

continue__22:                                     ; preds = %continue__21
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %313 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %314 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %313, 0
  %qubits = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %314, 1
  br label %continue__24

continue__24:                                     ; preds = %continue__22
  %315 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %315, i32 1)
  %316 = call %Result* @__quantum__rt__result_get_one()
  %317 = call i1 @__quantum__rt__result_equal(%Result* %315, %Result* %316)
  call void @__quantum__rt__result_update_reference_count(%Result* %315, i32 -1)
  br i1 %317, label %then0__6, label %continue__23

then0__6:                                         ; preds = %continue__24
  store i64 1, i64* %rand, align 4
  br label %continue__23

continue__23:                                     ; preds = %then0__6, %continue__24
  %318 = load i64, i64* %rand, align 4
  %319 = shl i64 %318, 1
  store i64 %319, i64* %rand, align 4
  %320 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %321 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %320, 0
  %qubits__1 = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %321, 1
  br label %continue__26

continue__26:                                     ; preds = %continue__23
  %322 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %322, i32 1)
  %323 = call %Result* @__quantum__rt__result_get_one()
  %324 = call i1 @__quantum__rt__result_equal(%Result* %322, %Result* %323)
  call void @__quantum__rt__result_update_reference_count(%Result* %322, i32 -1)
  br i1 %324, label %then0__7, label %continue__25

then0__7:                                         ; preds = %continue__26
  %325 = add i64 %319, 1
  store i64 %325, i64* %rand, align 4
  br label %continue__25

continue__25:                                     ; preds = %then0__7, %continue__26
  %326 = call %Result* @__quantum__rt__result_get_zero()
  %a = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %326)
  %a__1 = call %Result* @__quantum__rt__result_get_zero()
  %327 = call %Result* @__quantum__rt__result_get_one()
  %328 = call i1 @__quantum__rt__result_equal(%Result* %a__1, %Result* %327)
  %c = or i1 %328, %a
  %329 = insertvalue { i1, i1 } zeroinitializer, i1 %a, 0
  %330 = insertvalue { i1, i1 } %329, i1 %c, 1
  store { i1, i1 } %330, { i1, i1 }* %1, align 1
  %331 = bitcast { i1, i1 }* %1 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %331)
  %332 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %m2)
  %333 = select i1 %332, i64 6, i64 0
  store i64 %333, i64* %foo, align 4
  %334 = call %Result* @__quantum__rt__result_get_zero()
  %335 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %334)
  br i1 %335, label %then0__8, label %else__2

then0__8:                                         ; preds = %continue__25
  store i64 0, i64* %bar, align 4
  %336 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %337 = call %Result* @__quantum__rt__result_get_one()
  %338 = call i1 @__quantum__rt__result_equal(%Result* %336, %Result* %337)
  %339 = select i1 %338, i64 1, i64 0
  %340 = add i64 0, %339
  store i64 %340, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %336, i32 -1)
  %341 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %342 = call %Result* @__quantum__rt__result_get_one()
  %343 = call i1 @__quantum__rt__result_equal(%Result* %341, %Result* %342)
  %344 = select i1 %343, i64 1, i64 0
  %345 = add i64 %340, %344
  store i64 %345, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %341, i32 -1)
  store i64 %345, i64* %foo, align 4
  br label %continue__27

else__2:                                          ; preds = %continue__25
  store i64 0, i64* %bar__1, align 4
  %346 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %347 = call %Result* @__quantum__rt__result_get_zero()
  %348 = call i1 @__quantum__rt__result_equal(%Result* %346, %Result* %347)
  %349 = select i1 %348, i64 1, i64 0
  %350 = add i64 0, %349
  store i64 %350, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %346, i32 -1)
  %351 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %352 = call %Result* @__quantum__rt__result_get_zero()
  %353 = call i1 @__quantum__rt__result_equal(%Result* %351, %Result* %352)
  %354 = select i1 %353, i64 1, i64 0
  %355 = add i64 %350, %354
  store i64 %355, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %351, i32 -1)
  store i64 %355, i64* %foo, align 4
  br label %continue__27

continue__27:                                     ; preds = %else__2, %then0__8
  %qubit__31 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__26 = call %Qubit* @__quantum__rt__qubit_allocate()
  %356 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__30 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__27 = call %Qubit* @__quantum__rt__qubit_allocate()
  %357 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__31, 0
  %358 = insertvalue { [5 x %Qubit*] } zeroinitializer, [5 x %Qubit*] %357, 0
  %359 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %358, 1
  %360 = extractvalue { i64, { [5 x %Qubit*] } } %359, 1
  %361 = extractvalue { [5 x %Qubit*] } %360, 0
  %362 = insertvalue [5 x %Qubit*] %361, %Qubit* %qubit__26, 1
  %363 = insertvalue { [5 x %Qubit*] } %360, [5 x %Qubit*] %362, 0
  %364 = extractvalue { i64, { [5 x %Qubit*] } } %359, 0
  %365 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %364, 0
  %366 = insertvalue { i64, { [5 x %Qubit*] } } %365, { [5 x %Qubit*] } %363, 1
  %367 = extractvalue { i64, { [5 x %Qubit*] } } %366, 1
  %368 = extractvalue { [5 x %Qubit*] } %367, 0
  %369 = insertvalue [5 x %Qubit*] %368, %Qubit* %356, 2
  %370 = insertvalue { [5 x %Qubit*] } %367, [5 x %Qubit*] %369, 0
  %371 = extractvalue { i64, { [5 x %Qubit*] } } %366, 0
  %372 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %371, 0
  %373 = insertvalue { i64, { [5 x %Qubit*] } } %372, { [5 x %Qubit*] } %370, 1
  %374 = extractvalue { i64, { [5 x %Qubit*] } } %373, 1
  %375 = extractvalue { [5 x %Qubit*] } %374, 0
  %376 = insertvalue [5 x %Qubit*] %375, %Qubit* %qubit__30, 3
  %377 = insertvalue { [5 x %Qubit*] } %374, [5 x %Qubit*] %376, 0
  %378 = extractvalue { i64, { [5 x %Qubit*] } } %373, 0
  %379 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %378, 0
  %380 = insertvalue { i64, { [5 x %Qubit*] } } %379, { [5 x %Qubit*] } %377, 1
  %381 = extractvalue { i64, { [5 x %Qubit*] } } %380, 1
  %382 = extractvalue { [5 x %Qubit*] } %381, 0
  %383 = insertvalue [5 x %Qubit*] %382, %Qubit* %qubit__27, 4
  %384 = insertvalue { [5 x %Qubit*] } %381, [5 x %Qubit*] %383, 0
  %385 = extractvalue { i64, { [5 x %Qubit*] } } %380, 0
  %386 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %385, 0
  %q__20 = insertvalue { i64, { [5 x %Qubit*] } } %386, { [5 x %Qubit*] } %384, 1
  %r1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__26)
  %r2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__27)
  %r3 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %r4 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %387 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %388 = insertvalue { [5 x %Qubit*] } zeroinitializer, [5 x %Qubit*] %387, 0
  %389 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %388, 1
  %390 = extractvalue { i64, { [5 x %Qubit*] } } %389, 1
  %391 = extractvalue { [5 x %Qubit*] } %390, 0
  %392 = insertvalue [5 x %Qubit*] %391, %Qubit* %qubit__30, 1
  %393 = insertvalue { [5 x %Qubit*] } %390, [5 x %Qubit*] %392, 0
  %394 = extractvalue { i64, { [5 x %Qubit*] } } %389, 0
  %395 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %394, 0
  %396 = insertvalue { i64, { [5 x %Qubit*] } } %395, { [5 x %Qubit*] } %393, 1
  %397 = extractvalue { i64, { [5 x %Qubit*] } } %396, 1
  %398 = extractvalue { [5 x %Qubit*] } %397, 0
  %399 = insertvalue [5 x %Qubit*] %398, %Qubit* %356, 2
  %400 = insertvalue { [5 x %Qubit*] } %397, [5 x %Qubit*] %399, 0
  %401 = extractvalue { i64, { [5 x %Qubit*] } } %396, 0
  %402 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %401, 0
  %403 = insertvalue { i64, { [5 x %Qubit*] } } %402, { [5 x %Qubit*] } %400, 1
  %404 = extractvalue { i64, { [5 x %Qubit*] } } %403, 1
  %405 = extractvalue { [5 x %Qubit*] } %404, 0
  %406 = insertvalue [5 x %Qubit*] %405, %Qubit* %qubit__26, 3
  %407 = insertvalue { [5 x %Qubit*] } %404, [5 x %Qubit*] %406, 0
  %408 = extractvalue { i64, { [5 x %Qubit*] } } %403, 0
  %409 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %408, 0
  %410 = insertvalue { i64, { [5 x %Qubit*] } } %409, { [5 x %Qubit*] } %407, 1
  %411 = extractvalue { i64, { [5 x %Qubit*] } } %410, 1
  %412 = extractvalue { [5 x %Qubit*] } %411, 0
  %413 = insertvalue [5 x %Qubit*] %412, %Qubit* %qubit__31, 4
  %414 = insertvalue { [5 x %Qubit*] } %411, [5 x %Qubit*] %413, 0
  %415 = extractvalue { i64, { [5 x %Qubit*] } } %410, 0
  %416 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %415, 0
  %417 = insertvalue { i64, { [5 x %Qubit*] } } %416, { [5 x %Qubit*] } %414, 1
  %r5 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__30)
  %418 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %419 = insertvalue { [5 x %Qubit*] } zeroinitializer, [5 x %Qubit*] %418, 0
  %420 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %419, 1
  %421 = extractvalue { i64, { [5 x %Qubit*] } } %420, 1
  %422 = extractvalue { [5 x %Qubit*] } %421, 0
  %423 = insertvalue [5 x %Qubit*] %422, %Qubit* %qubit__30, 1
  %424 = insertvalue { [5 x %Qubit*] } %421, [5 x %Qubit*] %423, 0
  %425 = extractvalue { i64, { [5 x %Qubit*] } } %420, 0
  %426 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %425, 0
  %427 = insertvalue { i64, { [5 x %Qubit*] } } %426, { [5 x %Qubit*] } %424, 1
  %428 = extractvalue { i64, { [5 x %Qubit*] } } %427, 1
  %429 = extractvalue { [5 x %Qubit*] } %428, 0
  %430 = insertvalue [5 x %Qubit*] %429, %Qubit* %356, 2
  %431 = insertvalue { [5 x %Qubit*] } %428, [5 x %Qubit*] %430, 0
  %432 = extractvalue { i64, { [5 x %Qubit*] } } %427, 0
  %433 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %432, 0
  %434 = insertvalue { i64, { [5 x %Qubit*] } } %433, { [5 x %Qubit*] } %431, 1
  %435 = extractvalue { i64, { [5 x %Qubit*] } } %434, 1
  %436 = extractvalue { [5 x %Qubit*] } %435, 0
  %437 = insertvalue [5 x %Qubit*] %436, %Qubit* %qubit__26, 3
  %438 = insertvalue { [5 x %Qubit*] } %435, [5 x %Qubit*] %437, 0
  %439 = extractvalue { i64, { [5 x %Qubit*] } } %434, 0
  %440 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %439, 0
  %441 = insertvalue { i64, { [5 x %Qubit*] } } %440, { [5 x %Qubit*] } %438, 1
  %442 = extractvalue { i64, { [5 x %Qubit*] } } %441, 1
  %443 = extractvalue { [5 x %Qubit*] } %442, 0
  %444 = insertvalue [5 x %Qubit*] %443, %Qubit* %qubit__31, 4
  %445 = insertvalue { [5 x %Qubit*] } %442, [5 x %Qubit*] %444, 0
  %446 = extractvalue { i64, { [5 x %Qubit*] } } %441, 0
  %447 = insertvalue { i64, { [5 x %Qubit*] } } zeroinitializer, i64 %446, 0
  %z2 = insertvalue { i64, { [5 x %Qubit*] } } %447, { [5 x %Qubit*] } %445, 1
  %r6 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__31)
  call void @__quantum__rt__result_update_reference_count(%Result* %r1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r2, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r6, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__31)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__26)
  call void @__quantum__rt__qubit_release(%Qubit* %356)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__30)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__27)
  %448 = insertvalue [1 x %Result*] zeroinitializer, %Result* %m1, 0
  %449 = insertvalue { [1 x %Result*] } zeroinitializer, [1 x %Result*] %448, 0
  %arr3 = insertvalue { i64, { [1 x %Result*] } } { i64 1, { [1 x %Result*] } zeroinitializer }, { [1 x %Result*] } %449, 1
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 1)
  %450 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %m1)
  %451 = xor i1 %450, true
  br i1 %451, label %then0__9, label %continue__28

then0__9:                                         ; preds = %continue__27
  %452 = extractvalue { i64, { [1 x %Result*] } } %arr3, 1
  %453 = extractvalue { [1 x %Result*] } %452, 0
  %454 = insertvalue [1 x %Result*] %453, %Result* %m2, 0
  %455 = insertvalue { [1 x %Result*] } %452, [1 x %Result*] %454, 0
  %456 = extractvalue { i64, { [1 x %Result*] } } %arr3, 0
  %457 = insertvalue { i64, { [1 x %Result*] } } zeroinitializer, i64 %456, 0
  %458 = insertvalue { i64, { [1 x %Result*] } } %457, { [1 x %Result*] } %455, 1
  store { i64, { [1 x %Result*] } } %458, { i64, { [1 x %Result*] } }* %0, align 8
  %459 = bitcast { i64, { [1 x %Result*] } }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %459)
  br label %continue__28

continue__28:                                     ; preds = %then0__9, %continue__27
  %__rtrnVal1__ = load i64, i64* %rand, align 4
  %460 = insertvalue { i64, i64 } { i64 6, i64 0 }, i64 %__rtrnVal1__, 1
  call void @__quantum__rt__qubit_release(%Qubit* %82)
  call void @__quantum__rt__qubit_release(%Qubit* %83)
  call void @__quantum__rt__qubit_release(%Qubit* %93)
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
