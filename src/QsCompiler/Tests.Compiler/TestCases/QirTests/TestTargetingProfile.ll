
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
  %66 = extractvalue { i64, { [2 x %Qubit*] } } %65, 0
  %67 = extractvalue { i64, { [2 x %Qubit*] } } %65, 1
  %68 = extractvalue { [2 x %Qubit*] } %67, 0
  %69 = insertvalue [2 x %Qubit*] %68, %Qubit* %target, 1
  %70 = insertvalue { [2 x %Qubit*] } %67, [2 x %Qubit*] %69, 0
  %qs = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %70, 1
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__qis__cnot__body(%Qubit* %qubit, %Qubit* %target)
  %m1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %m2 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  store i64 1, i64* %32, align 4
  %71 = bitcast i64* %32 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %71)
  call void @__quantum__qis__logpauli__body(i2 -2)
  store i64 2, i64* %31, align 4
  %72 = bitcast i64* %31 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %72)
  store { i64, { [2 x i64] } } { i64 2, { [2 x i64] } { [2 x i64] [i64 1, i64 2] } }, { i64, { [2 x i64] } }* %30, align 4
  %73 = bitcast { i64, { [2 x i64] } }* %30 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %73)
  store i64 3, i64* %29, align 4
  %74 = bitcast i64* %29 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %74)
  store { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } } { { i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } zeroinitializer } }, { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } }* %28, align 4
  %75 = bitcast { { i64, { [2 x i64] } }, { i64, { [2 x i64] } } }* %28 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %75)
  store { i64, { [4 x { i64, { [2 x i64] } }] } } { i64 4, { [4 x { i64, { [2 x i64] } }] } { [4 x { i64, { [2 x i64] } }] [{ i64, { [2 x i64] } } { i64 2, { [2 x i64] } { [2 x i64] [i64 2, i64 1] } }, { i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } { [2 x i64] [i64 3, i64 0] } }, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } zeroinitializer }] } }, { i64, { [4 x { i64, { [2 x i64] } }] } }* %27, align 4
  %76 = bitcast { i64, { [4 x { i64, { [2 x i64] } }] } }* %27 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %76)
  call void @__quantum__qis__logpauli__body(i2 0)
  store { i64, { [4 x { i64, { [2 x i64] } }] } } { i64 4, { [4 x { i64, { [2 x i64] } }] } { [4 x { i64, { [2 x i64] } }] [{ i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } zeroinitializer, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } { [2 x i64] [i64 3, i64 0] } }, { i64, { [2 x i64] } } { i64 1, { [2 x i64] } zeroinitializer }] } }, { i64, { [4 x { i64, { [2 x i64] } }] } }* %26, align 4
  %77 = bitcast { i64, { [4 x { i64, { [2 x i64] } }] } }* %26 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %77)
  store { i64, { [4 x { i64, { [3 x i64] } }] } } { i64 4, { [4 x { i64, { [3 x i64] } }] } { [4 x { i64, { [3 x i64] } }] [{ i64, { [3 x i64] } } { i64 2, { [3 x i64] } { [3 x i64] [i64 2, i64 1, i64 0] } }, { i64, { [3 x i64] } } { i64 3, { [3 x i64] } { [3 x i64] [i64 1, i64 2, i64 3] } }, { i64, { [3 x i64] } } { i64 1, { [3 x i64] } { [3 x i64] [i64 3, i64 0, i64 0] } }, { i64, { [3 x i64] } } { i64 1, { [3 x i64] } zeroinitializer }] } }, { i64, { [4 x { i64, { [3 x i64] } }] } }* %25, align 4
  %78 = bitcast { i64, { [4 x { i64, { [3 x i64] } }] } }* %25 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %78)
  store { i64, { [3 x { i64, { [2 x i2] } }] } } { i64 3, { [3 x { i64, { [2 x i2] } }] } { [3 x { i64, { [2 x i2] } }] [{ i64, { [2 x i2] } } zeroinitializer, { i64, { [2 x i2] } } { i64 1, { [2 x i2] } { [2 x i2] [i2 -1, i2 0] } }, { i64, { [2 x i2] } } { i64 1, { [2 x i2] } zeroinitializer }] } }, { i64, { [3 x { i64, { [2 x i2] } }] } }* %24, align 4
  %79 = bitcast { i64, { [3 x { i64, { [2 x i2] } }] } }* %24 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %79)
  store { i64, { [3 x { i64, { [3 x i2] } }] } } { i64 3, { [3 x { i64, { [3 x i2] } }] } { [3 x { i64, { [3 x i2] } }] [{ i64, { [3 x i2] } } { i64 2, { [3 x i2] } { [3 x i2] [i2 1, i2 -2, i2 0] } }, { i64, { [3 x i2] } } { i64 3, { [3 x i2] } { [3 x i2] [i2 1, i2 1, i2 1] } }, { i64, { [3 x i2] } } { i64 1, { [3 x i2] } zeroinitializer }] } }, { i64, { [3 x { i64, { [3 x i2] } }] } }* %23, align 4
  %80 = bitcast { i64, { [3 x { i64, { [3 x i2] } }] } }* %23 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %80)
  %81 = call %Qubit* @__quantum__rt__qubit_allocate()
  %82 = call %Qubit* @__quantum__rt__qubit_allocate()
  %83 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %81, 0
  %84 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %83, 0
  %85 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %84, 1
  %86 = extractvalue { i64, { [2 x %Qubit*] } } %85, 0
  %87 = extractvalue { i64, { [2 x %Qubit*] } } %85, 1
  %88 = extractvalue { [2 x %Qubit*] } %87, 0
  %89 = insertvalue [2 x %Qubit*] %88, %Qubit* %82, 1
  %90 = insertvalue { [2 x %Qubit*] } %87, [2 x %Qubit*] %89, 0
  %qs1 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %90, 1
  %91 = call %Qubit* @__quantum__rt__qubit_allocate()
  %92 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %91, 0
  %93 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %92, 0
  %qs2 = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %93, 1
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %94 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %95 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %94, 0
  %96 = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %95, 1
  %97 = extractvalue { i64, { [2 x %Qubit*] } } %qs1, 0
  %98 = extractvalue { i64, { [2 x %Qubit*] } } %qs1, 1
  %99 = extractvalue { i64, { [1 x %Qubit*] } } %qs2, 0
  %100 = extractvalue { i64, { [1 x %Qubit*] } } %qs2, 1
  %101 = extractvalue { i64, { [1 x %Qubit*] } } %96, 0
  %102 = extractvalue { i64, { [1 x %Qubit*] } } %96, 1
  %103 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %81, 0
  %104 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %103, 0
  %105 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %104, 1
  %106 = extractvalue { i64, { [2 x %Qubit*] } } %105, 0
  %107 = extractvalue { i64, { [2 x %Qubit*] } } %105, 1
  %108 = extractvalue { [2 x %Qubit*] } %107, 0
  %109 = insertvalue [2 x %Qubit*] %108, %Qubit* %82, 1
  %110 = insertvalue { [2 x %Qubit*] } %107, [2 x %Qubit*] %109, 0
  %111 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %110, 1
  %112 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %91, 0
  %113 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %112, 0
  %114 = insertvalue { i64, { [2 x %Qubit*] } } { i64 1, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %113, 1
  %115 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %116 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %115, 0
  %117 = insertvalue { i64, { [2 x %Qubit*] } } { i64 1, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %116, 1
  %118 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] zeroinitializer, { i64, { [2 x %Qubit*] } } %111, 0
  %119 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer, [4 x { i64, { [2 x %Qubit*] } }] %118, 0
  %120 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [2 x %Qubit*] } }] } %119, 1
  %121 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %120, 0
  %122 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %120, 1
  %123 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %122, 0
  %124 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %123, { i64, { [2 x %Qubit*] } } zeroinitializer, 1
  %125 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %122, [4 x { i64, { [2 x %Qubit*] } }] %124, 0
  %126 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [2 x %Qubit*] } }] } %125, 1
  %127 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %126, 0
  %128 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %126, 1
  %129 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %128, 0
  %130 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %129, { i64, { [2 x %Qubit*] } } %114, 2
  %131 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %128, [4 x { i64, { [2 x %Qubit*] } }] %130, 0
  %132 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [2 x %Qubit*] } }] } %131, 1
  %133 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %132, 0
  %134 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %132, 1
  %135 = extractvalue { [4 x { i64, { [2 x %Qubit*] } }] } %134, 0
  %136 = insertvalue [4 x { i64, { [2 x %Qubit*] } }] %135, { i64, { [2 x %Qubit*] } } %117, 3
  %137 = insertvalue { [4 x { i64, { [2 x %Qubit*] } }] } %134, [4 x { i64, { [2 x %Qubit*] } }] %136, 0
  %qubitArrArr = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [2 x %Qubit*] } }] } %137, 1
  %138 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 0
  %139 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 1
  %140 = extractvalue { i64, { [2 x %Qubit*] } } %111, 0
  %141 = extractvalue { i64, { [2 x %Qubit*] } } %111, 1
  %142 = extractvalue { i64, { [2 x %Qubit*] } } %114, 0
  %143 = extractvalue { i64, { [2 x %Qubit*] } } %114, 1
  %144 = extractvalue { i64, { [2 x %Qubit*] } } %117, 0
  %145 = extractvalue { i64, { [2 x %Qubit*] } } %117, 1
  %146 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %81, 0
  %147 = insertvalue { [2 x %Qubit*] } zeroinitializer, [2 x %Qubit*] %146, 0
  %148 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %147, 1
  %149 = extractvalue { i64, { [2 x %Qubit*] } } %148, 0
  %150 = extractvalue { i64, { [2 x %Qubit*] } } %148, 1
  %151 = extractvalue { [2 x %Qubit*] } %150, 0
  %152 = insertvalue [2 x %Qubit*] %151, %Qubit* %82, 1
  %153 = insertvalue { [2 x %Qubit*] } %150, [2 x %Qubit*] %152, 0
  %154 = insertvalue { i64, { [2 x %Qubit*] } } { i64 2, { [2 x %Qubit*] } zeroinitializer }, { [2 x %Qubit*] } %153, 1
  %155 = insertvalue [2 x %Qubit*] zeroinitializer, %Qubit* %91, 0
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
  %169 = insertvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [2 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [2 x %Qubit*] } }] } %168, 1
  store { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %169, { i64, { [4 x { i64, { [2 x %Qubit*] } }] } }* %22, align 8
  %170 = bitcast { i64, { [4 x { i64, { [2 x %Qubit*] } }] } }* %22 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %170)
  %171 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %172 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %171, 0
  %173 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %172, 1
  %174 = extractvalue { i64, { [3 x %Qubit*] } } %173, 0
  %175 = extractvalue { i64, { [3 x %Qubit*] } } %173, 1
  %176 = extractvalue { [3 x %Qubit*] } %175, 0
  %177 = insertvalue [3 x %Qubit*] %176, %Qubit* %q, 1
  %178 = insertvalue { [3 x %Qubit*] } %175, [3 x %Qubit*] %177, 0
  %179 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %178, 1
  %180 = extractvalue { i64, { [3 x %Qubit*] } } %179, 0
  %181 = extractvalue { i64, { [3 x %Qubit*] } } %179, 1
  %182 = extractvalue { [3 x %Qubit*] } %181, 0
  %183 = insertvalue [3 x %Qubit*] %182, %Qubit* %q, 2
  %184 = insertvalue { [3 x %Qubit*] } %181, [3 x %Qubit*] %183, 0
  %185 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %184, 1
  %186 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 0
  %187 = extractvalue { i64, { [4 x { i64, { [2 x %Qubit*] } }] } } %qubitArrArr, 1
  %188 = extractvalue { i64, { [3 x %Qubit*] } } %185, 0
  %189 = extractvalue { i64, { [3 x %Qubit*] } } %185, 1
  %190 = extractvalue { i64, { [2 x %Qubit*] } } %111, 0
  %191 = extractvalue { i64, { [2 x %Qubit*] } } %111, 1
  %192 = extractvalue { i64, { [2 x %Qubit*] } } %114, 0
  %193 = extractvalue { i64, { [2 x %Qubit*] } } %114, 1
  %194 = extractvalue { i64, { [2 x %Qubit*] } } %117, 0
  %195 = extractvalue { i64, { [2 x %Qubit*] } } %117, 1
  %196 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %197 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %196, 0
  %198 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %197, 1
  %199 = extractvalue { i64, { [3 x %Qubit*] } } %198, 0
  %200 = extractvalue { i64, { [3 x %Qubit*] } } %198, 1
  %201 = extractvalue { [3 x %Qubit*] } %200, 0
  %202 = insertvalue [3 x %Qubit*] %201, %Qubit* %q, 1
  %203 = insertvalue { [3 x %Qubit*] } %200, [3 x %Qubit*] %202, 0
  %204 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %203, 1
  %205 = extractvalue { i64, { [3 x %Qubit*] } } %204, 0
  %206 = extractvalue { i64, { [3 x %Qubit*] } } %204, 1
  %207 = extractvalue { [3 x %Qubit*] } %206, 0
  %208 = insertvalue [3 x %Qubit*] %207, %Qubit* %q, 2
  %209 = insertvalue { [3 x %Qubit*] } %206, [3 x %Qubit*] %208, 0
  %210 = insertvalue { i64, { [3 x %Qubit*] } } { i64 3, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %209, 1
  %211 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %81, 0
  %212 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %211, 0
  %213 = insertvalue { i64, { [3 x %Qubit*] } } { i64 2, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %212, 1
  %214 = extractvalue { i64, { [3 x %Qubit*] } } %213, 0
  %215 = extractvalue { i64, { [3 x %Qubit*] } } %213, 1
  %216 = extractvalue { [3 x %Qubit*] } %215, 0
  %217 = insertvalue [3 x %Qubit*] %216, %Qubit* %82, 1
  %218 = insertvalue { [3 x %Qubit*] } %215, [3 x %Qubit*] %217, 0
  %219 = insertvalue { i64, { [3 x %Qubit*] } } { i64 2, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %218, 1
  %220 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %91, 0
  %221 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %220, 0
  %222 = insertvalue { i64, { [3 x %Qubit*] } } { i64 1, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %221, 1
  %223 = insertvalue [3 x %Qubit*] zeroinitializer, %Qubit* %q, 0
  %224 = insertvalue { [3 x %Qubit*] } zeroinitializer, [3 x %Qubit*] %223, 0
  %225 = insertvalue { i64, { [3 x %Qubit*] } } { i64 1, { [3 x %Qubit*] } zeroinitializer }, { [3 x %Qubit*] } %224, 1
  %226 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] zeroinitializer, { i64, { [3 x %Qubit*] } } %219, 0
  %227 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %226, { i64, { [3 x %Qubit*] } } zeroinitializer, 1
  %228 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %227, { i64, { [3 x %Qubit*] } } %222, 2
  %229 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %228, { i64, { [3 x %Qubit*] } } %225, 3
  %230 = insertvalue { [4 x { i64, { [3 x %Qubit*] } }] } zeroinitializer, [4 x { i64, { [3 x %Qubit*] } }] %229, 0
  %231 = extractvalue { [4 x { i64, { [3 x %Qubit*] } }] } %230, 0
  %232 = insertvalue [4 x { i64, { [3 x %Qubit*] } }] %231, { i64, { [3 x %Qubit*] } } %210, 1
  %233 = insertvalue { [4 x { i64, { [3 x %Qubit*] } }] } %230, [4 x { i64, { [3 x %Qubit*] } }] %232, 0
  %234 = insertvalue { i64, { [4 x { i64, { [3 x %Qubit*] } }] } } { i64 4, { [4 x { i64, { [3 x %Qubit*] } }] } zeroinitializer }, { [4 x { i64, { [3 x %Qubit*] } }] } %233, 1
  store { i64, { [4 x { i64, { [3 x %Qubit*] } }] } } %234, { i64, { [4 x { i64, { [3 x %Qubit*] } }] } }* %21, align 8
  %235 = bitcast { i64, { [4 x { i64, { [3 x %Qubit*] } }] } }* %21 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %235)
  %236 = call %Result* @__quantum__rt__result_get_zero()
  %237 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %236)
  br i1 %237, label %then1__1, label %test2__1

then1__1:                                         ; preds = %entry
  %q__1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %238 = call %Result* @__quantum__qis__m__body(%Qubit* %q__1)
  %239 = insertvalue { i64, %Result* } { i64 2, %Result* null }, %Result* %238, 1
  store { i64, %Result* } %239, { i64, %Result* }* %20, align 8
  %240 = bitcast { i64, %Result* }* %20 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %240)
  call void @__quantum__rt__qubit_release(%Qubit* %q__1)
  br label %continue__1

test2__1:                                         ; preds = %entry
  %q__2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %241 = call %Result* @__quantum__qis__m__body(%Qubit* %q__2)
  %242 = insertvalue { i64, %Result* } { i64 4, %Result* null }, %Result* %241, 1
  store { i64, %Result* } %242, { i64, %Result* }* %19, align 8
  %243 = bitcast { i64, %Result* }* %19 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %243)
  call void @__quantum__rt__qubit_release(%Qubit* %q__2)
  br label %continue__1

continue__1:                                      ; preds = %test2__1, %then1__1
  %q__3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %244 = call %Result* @__quantum__qis__m__body(%Qubit* %q__3)
  %245 = insertvalue { i64, %Result* } { i64 6, %Result* null }, %Result* %244, 1
  store { i64, %Result* } %245, { i64, %Result* }* %18, align 8
  %246 = bitcast { i64, %Result* }* %18 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %246)
  call void @__quantum__rt__qubit_release(%Qubit* %q__3)
  br label %continue__2

continue__2:                                      ; preds = %continue__1
  %q2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %q__4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %247 = call %Result* @__quantum__qis__m__body(%Qubit* %q__4)
  %248 = insertvalue { i64, %Result* } { i64 9, %Result* null }, %Result* %247, 1
  store { i64, %Result* } %248, { i64, %Result* }* %17, align 8
  %249 = bitcast { i64, %Result* }* %17 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %249)
  call void @__quantum__rt__qubit_release(%Qubit* %q__4)
  br label %continue__3

continue__3:                                      ; preds = %continue__2
  call void @__quantum__rt__qubit_release(%Qubit* %q2)
  %250 = call %Result* @__quantum__rt__result_get_zero()
  %251 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %250)
  br i1 %251, label %then0__1, label %else__1

then0__1:                                         ; preds = %continue__3
  %q__5 = call %Qubit* @__quantum__rt__qubit_allocate()
  %252 = call %Result* @__quantum__qis__m__body(%Qubit* %q__5)
  %253 = insertvalue { i64, %Result* } { i64 12, %Result* null }, %Result* %252, 1
  store { i64, %Result* } %253, { i64, %Result* }* %16, align 8
  %254 = bitcast { i64, %Result* }* %16 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %254)
  call void @__quantum__rt__qubit_release(%Qubit* %q__5)
  br label %continue__4

else__1:                                          ; preds = %continue__3
  %q__6 = call %Qubit* @__quantum__rt__qubit_allocate()
  %255 = call %Result* @__quantum__qis__m__body(%Qubit* %q__6)
  %256 = insertvalue { i64, %Result* } { i64 13, %Result* null }, %Result* %255, 1
  store { i64, %Result* } %256, { i64, %Result* }* %15, align 8
  %257 = bitcast { i64, %Result* }* %15 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %257)
  call void @__quantum__rt__qubit_release(%Qubit* %q__6)
  br label %continue__4

continue__4:                                      ; preds = %else__1, %then0__1
  %258 = call %Result* @__quantum__rt__result_get_zero()
  %259 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %258)
  br i1 %259, label %then0__2, label %test1__1

then0__2:                                         ; preds = %continue__4
  %q__7 = call %Qubit* @__quantum__rt__qubit_allocate()
  %260 = call %Result* @__quantum__qis__m__body(%Qubit* %q__7)
  %261 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %260, 1
  store { i64, %Result* } %261, { i64, %Result* }* %14, align 8
  %262 = bitcast { i64, %Result* }* %14 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %262)
  call void @__quantum__rt__qubit_release(%Qubit* %q__7)
  br label %continue__5

test1__1:                                         ; preds = %continue__4
  br label %continue__5

continue__5:                                      ; preds = %test1__1, %then0__2
  %263 = call %Result* @__quantum__rt__result_get_zero()
  %264 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %263)
  br i1 %264, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %continue__5
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %continue__5
  br label %continue__6

continue__6:                                      ; preds = %condContinue__1
  %265 = call %Result* @__quantum__rt__result_get_zero()
  %266 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %265)
  br i1 %266, label %condContinue__2, label %condFalse__1

condFalse__1:                                     ; preds = %continue__6
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__1, %continue__6
  br i1 %266, label %then0__3, label %continue__7

then0__3:                                         ; preds = %condContinue__2
  %q__8 = call %Qubit* @__quantum__rt__qubit_allocate()
  %267 = call %Result* @__quantum__qis__m__body(%Qubit* %q__8)
  %268 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %267, 1
  store { i64, %Result* } %268, { i64, %Result* }* %13, align 8
  %269 = bitcast { i64, %Result* }* %13 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %269)
  call void @__quantum__rt__qubit_release(%Qubit* %q__8)
  br label %continue__7

continue__7:                                      ; preds = %then0__3, %condContinue__2
  %270 = call %Result* @__quantum__rt__result_get_zero()
  %271 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %270)
  br i1 %271, label %then0__4, label %test1__2

then0__4:                                         ; preds = %continue__7
  %q__9 = call %Qubit* @__quantum__rt__qubit_allocate()
  %272 = call %Result* @__quantum__qis__m__body(%Qubit* %q__9)
  %273 = insertvalue { i64, %Result* } { i64 14, %Result* null }, %Result* %272, 1
  store { i64, %Result* } %273, { i64, %Result* }* %12, align 8
  %274 = bitcast { i64, %Result* }* %12 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %274)
  call void @__quantum__rt__qubit_release(%Qubit* %q__9)
  br label %continue__8

test1__2:                                         ; preds = %continue__7
  %q__10 = call %Qubit* @__quantum__rt__qubit_allocate()
  %275 = call %Result* @__quantum__qis__m__body(%Qubit* %q__10)
  %276 = insertvalue { i64, %Result* } { i64 15, %Result* null }, %Result* %275, 1
  store { i64, %Result* } %276, { i64, %Result* }* %11, align 8
  %277 = bitcast { i64, %Result* }* %11 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %277)
  call void @__quantum__rt__qubit_release(%Qubit* %q__10)
  br label %continue__8

continue__8:                                      ; preds = %test1__2, %then0__4
  %278 = call %Result* @__quantum__rt__result_get_zero()
  %279 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %278)
  br i1 %279, label %condTrue__2, label %condContinue__3

condTrue__2:                                      ; preds = %continue__8
  br label %condContinue__3

condContinue__3:                                  ; preds = %condTrue__2, %continue__8
  br i1 %279, label %then0__5, label %continue__9

then0__5:                                         ; preds = %condContinue__3
  %q__11 = call %Qubit* @__quantum__rt__qubit_allocate()
  %280 = call %Result* @__quantum__qis__m__body(%Qubit* %q__11)
  %281 = insertvalue { i64, %Result* } { i64 16, %Result* null }, %Result* %280, 1
  store { i64, %Result* } %281, { i64, %Result* }* %10, align 8
  %282 = bitcast { i64, %Result* }* %10 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %282)
  call void @__quantum__rt__qubit_release(%Qubit* %q__11)
  br label %continue__9

continue__9:                                      ; preds = %then0__5, %condContinue__3
  %283 = call %Result* @__quantum__rt__result_get_zero()
  %284 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %283)
  br i1 %284, label %condContinue__4, label %condFalse__2

condFalse__2:                                     ; preds = %continue__9
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__2, %continue__9
  %q__12 = call %Qubit* @__quantum__rt__qubit_allocate()
  %285 = call %Result* @__quantum__qis__m__body(%Qubit* %q__12)
  %286 = insertvalue { i64, %Result* } { i64 17, %Result* null }, %Result* %285, 1
  store { i64, %Result* } %286, { i64, %Result* }* %9, align 8
  %287 = bitcast { i64, %Result* }* %9 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %287)
  call void @__quantum__rt__qubit_release(%Qubit* %q__12)
  br label %continue__10

continue__10:                                     ; preds = %condContinue__4
  %q__13 = call %Qubit* @__quantum__rt__qubit_allocate()
  %288 = call %Result* @__quantum__qis__m__body(%Qubit* %q__13)
  %289 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %288, 1
  store { i64, %Result* } %289, { i64, %Result* }* %8, align 8
  %290 = bitcast { i64, %Result* }* %8 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %290)
  call void @__quantum__rt__qubit_release(%Qubit* %q__13)
  br label %continue__11

continue__11:                                     ; preds = %continue__10
  br label %continue__12

continue__12:                                     ; preds = %continue__11
  %q__14 = call %Qubit* @__quantum__rt__qubit_allocate()
  %291 = call %Result* @__quantum__qis__m__body(%Qubit* %q__14)
  %292 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %291, 1
  store { i64, %Result* } %292, { i64, %Result* }* %7, align 8
  %293 = bitcast { i64, %Result* }* %7 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %293)
  call void @__quantum__rt__qubit_release(%Qubit* %q__14)
  br label %continue__13

continue__13:                                     ; preds = %continue__12
  %q__15 = call %Qubit* @__quantum__rt__qubit_allocate()
  %294 = call %Result* @__quantum__qis__m__body(%Qubit* %q__15)
  %295 = insertvalue { i64, %Result* } { i64 18, %Result* null }, %Result* %294, 1
  store { i64, %Result* } %295, { i64, %Result* }* %6, align 8
  %296 = bitcast { i64, %Result* }* %6 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %296)
  call void @__quantum__rt__qubit_release(%Qubit* %q__15)
  br label %continue__14

continue__14:                                     ; preds = %continue__13
  %q__16 = call %Qubit* @__quantum__rt__qubit_allocate()
  %297 = call %Result* @__quantum__qis__m__body(%Qubit* %q__16)
  %298 = insertvalue { i64, %Result* } { i64 20, %Result* null }, %Result* %297, 1
  store { i64, %Result* } %298, { i64, %Result* }* %5, align 8
  %299 = bitcast { i64, %Result* }* %5 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %299)
  call void @__quantum__rt__qubit_release(%Qubit* %q__16)
  br label %continue__15

continue__15:                                     ; preds = %continue__14
  %q__17 = call %Qubit* @__quantum__rt__qubit_allocate()
  %300 = call %Result* @__quantum__qis__m__body(%Qubit* %q__17)
  %301 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %300, 1
  store { i64, %Result* } %301, { i64, %Result* }* %4, align 8
  %302 = bitcast { i64, %Result* }* %4 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %302)
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
  %303 = call %Result* @__quantum__qis__m__body(%Qubit* %q__18)
  %304 = insertvalue { i64, %Result* } { i64 19, %Result* null }, %Result* %303, 1
  store { i64, %Result* } %304, { i64, %Result* }* %3, align 8
  %305 = bitcast { i64, %Result* }* %3 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %305)
  call void @__quantum__rt__qubit_release(%Qubit* %q__18)
  br label %continue__20

continue__20:                                     ; preds = %continue__19
  br label %continue__21

continue__21:                                     ; preds = %continue__20
  %q__19 = call %Qubit* @__quantum__rt__qubit_allocate()
  %306 = call %Result* @__quantum__qis__m__body(%Qubit* %q__19)
  %307 = insertvalue { i64, %Result* } { i64 21, %Result* null }, %Result* %306, 1
  store { i64, %Result* } %307, { i64, %Result* }* %2, align 8
  %308 = bitcast { i64, %Result* }* %2 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %308)
  call void @__quantum__rt__qubit_release(%Qubit* %q__19)
  br label %continue__22

continue__22:                                     ; preds = %continue__21
  store i64 0, i64* %rand, align 4
  store i64 0, i64* %rand, align 4
  %309 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %qubit, 0
  %310 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %309, 0
  %qubits = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %310, 1
  br label %continue__24

continue__24:                                     ; preds = %continue__22
  %311 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %311, i32 1)
  %312 = call %Result* @__quantum__rt__result_get_one()
  %313 = call i1 @__quantum__rt__result_equal(%Result* %311, %Result* %312)
  call void @__quantum__rt__result_update_reference_count(%Result* %311, i32 -1)
  br i1 %313, label %then0__6, label %continue__23

then0__6:                                         ; preds = %continue__24
  store i64 1, i64* %rand, align 4
  br label %continue__23

continue__23:                                     ; preds = %then0__6, %continue__24
  %314 = load i64, i64* %rand, align 4
  %315 = shl i64 %314, 1
  store i64 %315, i64* %rand, align 4
  %316 = insertvalue [1 x %Qubit*] zeroinitializer, %Qubit* %target, 0
  %317 = insertvalue { [1 x %Qubit*] } zeroinitializer, [1 x %Qubit*] %316, 0
  %qubits__1 = insertvalue { i64, { [1 x %Qubit*] } } { i64 1, { [1 x %Qubit*] } zeroinitializer }, { [1 x %Qubit*] } %317, 1
  br label %continue__26

continue__26:                                     ; preds = %continue__23
  %318 = call %Result* @__quantum__rt__result_get_one()
  call void @__quantum__rt__result_update_reference_count(%Result* %318, i32 1)
  %319 = call %Result* @__quantum__rt__result_get_one()
  %320 = call i1 @__quantum__rt__result_equal(%Result* %318, %Result* %319)
  call void @__quantum__rt__result_update_reference_count(%Result* %318, i32 -1)
  br i1 %320, label %then0__7, label %continue__25

then0__7:                                         ; preds = %continue__26
  %321 = add i64 %315, 1
  store i64 %321, i64* %rand, align 4
  br label %continue__25

continue__25:                                     ; preds = %then0__7, %continue__26
  %322 = call %Result* @__quantum__rt__result_get_zero()
  %a = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %322)
  %a__1 = call %Result* @__quantum__rt__result_get_zero()
  %323 = call %Result* @__quantum__rt__result_get_one()
  %324 = call i1 @__quantum__rt__result_equal(%Result* %a__1, %Result* %323)
  %c = or i1 %324, %a
  %325 = insertvalue { i1, i1 } zeroinitializer, i1 %a, 0
  %326 = insertvalue { i1, i1 } %325, i1 %c, 1
  store { i1, i1 } %326, { i1, i1 }* %1, align 1
  %327 = bitcast { i1, i1 }* %1 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %327)
  %328 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %m2)
  %329 = select i1 %328, i64 6, i64 0
  store i64 %329, i64* %foo, align 4
  %330 = call %Result* @__quantum__rt__result_get_zero()
  %331 = call i1 @__quantum__rt__result_equal(%Result* %m1, %Result* %330)
  br i1 %331, label %then0__8, label %else__2

then0__8:                                         ; preds = %continue__25
  store i64 0, i64* %bar, align 4
  %332 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %333 = call %Result* @__quantum__rt__result_get_one()
  %334 = call i1 @__quantum__rt__result_equal(%Result* %332, %Result* %333)
  %335 = select i1 %334, i64 1, i64 0
  %336 = add i64 0, %335
  store i64 %336, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %332, i32 -1)
  %337 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %338 = call %Result* @__quantum__rt__result_get_one()
  %339 = call i1 @__quantum__rt__result_equal(%Result* %337, %Result* %338)
  %340 = select i1 %339, i64 1, i64 0
  %341 = add i64 %336, %340
  store i64 %341, i64* %bar, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %337, i32 -1)
  store i64 %341, i64* %foo, align 4
  br label %continue__27

else__2:                                          ; preds = %continue__25
  store i64 0, i64* %bar__1, align 4
  %342 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit)
  %343 = call %Result* @__quantum__rt__result_get_zero()
  %344 = call i1 @__quantum__rt__result_equal(%Result* %342, %Result* %343)
  %345 = select i1 %344, i64 1, i64 0
  %346 = add i64 0, %345
  store i64 %346, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %342, i32 -1)
  %347 = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  %348 = call %Result* @__quantum__rt__result_get_zero()
  %349 = call i1 @__quantum__rt__result_equal(%Result* %347, %Result* %348)
  %350 = select i1 %349, i64 1, i64 0
  %351 = add i64 %346, %350
  store i64 %351, i64* %bar__1, align 4
  call void @__quantum__rt__result_update_reference_count(%Result* %347, i32 -1)
  store i64 %351, i64* %foo, align 4
  br label %continue__27

continue__27:                                     ; preds = %else__2, %then0__8
  %qubit__31 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__26 = call %Qubit* @__quantum__rt__qubit_allocate()
  %352 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__30 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit__27 = call %Qubit* @__quantum__rt__qubit_allocate()
  %353 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__31, 0
  %354 = insertvalue { [5 x %Qubit*] } zeroinitializer, [5 x %Qubit*] %353, 0
  %355 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %354, 1
  %356 = extractvalue { i64, { [5 x %Qubit*] } } %355, 0
  %357 = extractvalue { i64, { [5 x %Qubit*] } } %355, 1
  %358 = extractvalue { [5 x %Qubit*] } %357, 0
  %359 = insertvalue [5 x %Qubit*] %358, %Qubit* %qubit__26, 1
  %360 = insertvalue { [5 x %Qubit*] } %357, [5 x %Qubit*] %359, 0
  %361 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %360, 1
  %362 = extractvalue { i64, { [5 x %Qubit*] } } %361, 0
  %363 = extractvalue { i64, { [5 x %Qubit*] } } %361, 1
  %364 = extractvalue { [5 x %Qubit*] } %363, 0
  %365 = insertvalue [5 x %Qubit*] %364, %Qubit* %352, 2
  %366 = insertvalue { [5 x %Qubit*] } %363, [5 x %Qubit*] %365, 0
  %367 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %366, 1
  %368 = extractvalue { i64, { [5 x %Qubit*] } } %367, 0
  %369 = extractvalue { i64, { [5 x %Qubit*] } } %367, 1
  %370 = extractvalue { [5 x %Qubit*] } %369, 0
  %371 = insertvalue [5 x %Qubit*] %370, %Qubit* %qubit__30, 3
  %372 = insertvalue { [5 x %Qubit*] } %369, [5 x %Qubit*] %371, 0
  %373 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %372, 1
  %374 = extractvalue { i64, { [5 x %Qubit*] } } %373, 0
  %375 = extractvalue { i64, { [5 x %Qubit*] } } %373, 1
  %376 = extractvalue { [5 x %Qubit*] } %375, 0
  %377 = insertvalue [5 x %Qubit*] %376, %Qubit* %qubit__27, 4
  %378 = insertvalue { [5 x %Qubit*] } %375, [5 x %Qubit*] %377, 0
  %q__20 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %378, 1
  %r1 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__26)
  %r2 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__27)
  %r3 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %r4 = call %Result* @__quantum__qis__m__body(%Qubit* poison)
  %379 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %380 = insertvalue { [5 x %Qubit*] } zeroinitializer, [5 x %Qubit*] %379, 0
  %381 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %380, 1
  %382 = extractvalue { i64, { [5 x %Qubit*] } } %381, 0
  %383 = extractvalue { i64, { [5 x %Qubit*] } } %381, 1
  %384 = extractvalue { [5 x %Qubit*] } %383, 0
  %385 = insertvalue [5 x %Qubit*] %384, %Qubit* %qubit__30, 1
  %386 = insertvalue { [5 x %Qubit*] } %383, [5 x %Qubit*] %385, 0
  %387 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %386, 1
  %388 = extractvalue { i64, { [5 x %Qubit*] } } %387, 0
  %389 = extractvalue { i64, { [5 x %Qubit*] } } %387, 1
  %390 = extractvalue { [5 x %Qubit*] } %389, 0
  %391 = insertvalue [5 x %Qubit*] %390, %Qubit* %352, 2
  %392 = insertvalue { [5 x %Qubit*] } %389, [5 x %Qubit*] %391, 0
  %393 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %392, 1
  %394 = extractvalue { i64, { [5 x %Qubit*] } } %393, 0
  %395 = extractvalue { i64, { [5 x %Qubit*] } } %393, 1
  %396 = extractvalue { [5 x %Qubit*] } %395, 0
  %397 = insertvalue [5 x %Qubit*] %396, %Qubit* %qubit__26, 3
  %398 = insertvalue { [5 x %Qubit*] } %395, [5 x %Qubit*] %397, 0
  %399 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %398, 1
  %400 = extractvalue { i64, { [5 x %Qubit*] } } %399, 0
  %401 = extractvalue { i64, { [5 x %Qubit*] } } %399, 1
  %402 = extractvalue { [5 x %Qubit*] } %401, 0
  %403 = insertvalue [5 x %Qubit*] %402, %Qubit* %qubit__31, 4
  %404 = insertvalue { [5 x %Qubit*] } %401, [5 x %Qubit*] %403, 0
  %405 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %404, 1
  %r5 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__30)
  %406 = insertvalue [5 x %Qubit*] zeroinitializer, %Qubit* %qubit__27, 0
  %407 = insertvalue { [5 x %Qubit*] } zeroinitializer, [5 x %Qubit*] %406, 0
  %408 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %407, 1
  %409 = extractvalue { i64, { [5 x %Qubit*] } } %408, 0
  %410 = extractvalue { i64, { [5 x %Qubit*] } } %408, 1
  %411 = extractvalue { [5 x %Qubit*] } %410, 0
  %412 = insertvalue [5 x %Qubit*] %411, %Qubit* %qubit__30, 1
  %413 = insertvalue { [5 x %Qubit*] } %410, [5 x %Qubit*] %412, 0
  %414 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %413, 1
  %415 = extractvalue { i64, { [5 x %Qubit*] } } %414, 0
  %416 = extractvalue { i64, { [5 x %Qubit*] } } %414, 1
  %417 = extractvalue { [5 x %Qubit*] } %416, 0
  %418 = insertvalue [5 x %Qubit*] %417, %Qubit* %352, 2
  %419 = insertvalue { [5 x %Qubit*] } %416, [5 x %Qubit*] %418, 0
  %420 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %419, 1
  %421 = extractvalue { i64, { [5 x %Qubit*] } } %420, 0
  %422 = extractvalue { i64, { [5 x %Qubit*] } } %420, 1
  %423 = extractvalue { [5 x %Qubit*] } %422, 0
  %424 = insertvalue [5 x %Qubit*] %423, %Qubit* %qubit__26, 3
  %425 = insertvalue { [5 x %Qubit*] } %422, [5 x %Qubit*] %424, 0
  %426 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %425, 1
  %427 = extractvalue { i64, { [5 x %Qubit*] } } %426, 0
  %428 = extractvalue { i64, { [5 x %Qubit*] } } %426, 1
  %429 = extractvalue { [5 x %Qubit*] } %428, 0
  %430 = insertvalue [5 x %Qubit*] %429, %Qubit* %qubit__31, 4
  %431 = insertvalue { [5 x %Qubit*] } %428, [5 x %Qubit*] %430, 0
  %z2 = insertvalue { i64, { [5 x %Qubit*] } } { i64 5, { [5 x %Qubit*] } zeroinitializer }, { [5 x %Qubit*] } %431, 1
  %r6 = call %Result* @__quantum__qis__m__body(%Qubit* %qubit__31)
  call void @__quantum__rt__result_update_reference_count(%Result* %r1, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r2, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r5, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %r6, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__31)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__26)
  call void @__quantum__rt__qubit_release(%Qubit* %352)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__30)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit__27)
  %432 = insertvalue [1 x %Result*] zeroinitializer, %Result* %m1, 0
  %433 = insertvalue { [1 x %Result*] } zeroinitializer, [1 x %Result*] %432, 0
  %arr3 = insertvalue { i64, { [1 x %Result*] } } { i64 1, { [1 x %Result*] } zeroinitializer }, { [1 x %Result*] } %433, 1
  call void @__quantum__rt__result_update_reference_count(%Result* %m1, i32 1)
  %434 = call i1 @__quantum__rt__result_equal(%Result* %m2, %Result* %m1)
  %435 = xor i1 %434, true
  br i1 %435, label %then0__9, label %continue__28

then0__9:                                         ; preds = %continue__27
  %436 = extractvalue { i64, { [1 x %Result*] } } %arr3, 0
  %437 = extractvalue { i64, { [1 x %Result*] } } %arr3, 1
  %438 = extractvalue { [1 x %Result*] } %437, 0
  %439 = insertvalue [1 x %Result*] %438, %Result* %m2, 0
  %440 = insertvalue { [1 x %Result*] } %437, [1 x %Result*] %439, 0
  %441 = insertvalue { i64, { [1 x %Result*] } } { i64 1, { [1 x %Result*] } zeroinitializer }, { [1 x %Result*] } %440, 1
  store { i64, { [1 x %Result*] } } %441, { i64, { [1 x %Result*] } }* %0, align 8
  %442 = bitcast { i64, { [1 x %Result*] } }* %0 to i8*
  call void @__quantum__qis__dumpmachine__body(i8* %442)
  br label %continue__28

continue__28:                                     ; preds = %then0__9, %continue__27
  %__rtrnVal1__ = load i64, i64* %rand, align 4
  %443 = insertvalue { i64, i64 } { i64 6, i64 0 }, i64 %__rtrnVal1__, 1
  call void @__quantum__rt__qubit_release(%Qubit* %81)
  call void @__quantum__rt__qubit_release(%Qubit* %82)
  call void @__quantum__rt__qubit_release(%Qubit* %91)
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
