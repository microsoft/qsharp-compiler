define { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__Interop({ i64, i8* }* %arr, i8* %str, i8 %res, { i64, i64, i64 }* %range, i64 %cnt, i8 %b, double %d, { i64, i8* }* %l) #0 {
entry:
  %0 = getelementptr { i64, i8* }, { i64, i8* }* %arr, i64 0, i32 0
  %1 = getelementptr { i64, i8* }, { i64, i8* }* %arr, i64 0, i32 1
  %2 = load i64, i64* %0, align 4
  %3 = load i8*, i8** %1, align 8
  %4 = call %Array* @__quantum__rt__array_create_1d(i32 1, i64 %2)
  %5 = ptrtoint i8* %3 to i64
  %6 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %7 = phi i64 [ 0, %entry ], [ %16, %exiting__1 ]
  %8 = icmp sle i64 %7, %6
  br i1 %8, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %9 = mul i64 %7, 1
  %10 = add i64 %5, %9
  %11 = inttoptr i64 %10 to i8*
  %12 = load i8, i8* %11, align 1
  %13 = trunc i8 %12 to i2
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 %7)
  %15 = bitcast i8* %14 to i2*
  store i2 %13, i2* %15, align 1
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %16 = add i64 %7, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %17 = call %String* @__quantum__rt__string_create(i8* %str)
  %18 = icmp eq i8 %res, 0
  %19 = call %Result* @__quantum__rt__result_get_zero()
  %20 = call %Result* @__quantum__rt__result_get_one()
  %21 = select i1 %18, %Result* %19, %Result* %20
  %22 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 0
  %23 = load i64, i64* %22, align 4
  %24 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 1
  %25 = load i64, i64* %24, align 4
  %26 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 2
  %27 = load i64, i64* %26, align 4
  %28 = insertvalue %Range zeroinitializer, i64 %23, 0
  %29 = insertvalue %Range %28, i64 %25, 1
  %30 = insertvalue %Range %29, i64 %27, 2
  %31 = trunc i8 %b to i1
  %32 = getelementptr { i64, i8* }, { i64, i8* }* %l, i64 0, i32 0
  %33 = getelementptr { i64, i8* }, { i64, i8* }* %l, i64 0, i32 1
  %34 = load i64, i64* %32, align 4
  %35 = load i8*, i8** %33, align 8
  %36 = call %BigInt* @__quantum__rt__bigint_create_array(i64 %34, i8* %35)
  %37 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %BigInt* }* getelementptr ({ double, %BigInt* }, { double, %BigInt* }* null, i32 1) to i64))
  %38 = bitcast %Tuple* %37 to { double, %BigInt* }*
  %39 = getelementptr inbounds { double, %BigInt* }, { double, %BigInt* }* %38, i32 0, i32 0
  %40 = getelementptr inbounds { double, %BigInt* }, { double, %BigInt* }* %38, i32 0, i32 1
  store double %d, double* %39, align 8
  store %BigInt* %36, %BigInt** %40, align 8
  %41 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i1, { double, %BigInt* }* }* getelementptr ({ i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* null, i32 1) to i64))
  %42 = bitcast %Tuple* %41 to { i64, i1, { double, %BigInt* }* }*
  %43 = getelementptr inbounds { i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* %42, i32 0, i32 0
  %44 = getelementptr inbounds { i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* %42, i32 0, i32 1
  %45 = getelementptr inbounds { i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* %42, i32 0, i32 2
  store i64 %cnt, i64* %43, align 4
  store i1 %31, i1* %44, align 1
  store { double, %BigInt* }* %38, { double, %BigInt* }** %45, align 8
  %46 = call { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, %String* %17, %Result* %21, %Range %30, { i64, i1, { double, %BigInt* }* }* %42)
  %47 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 0
  %48 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 1
  %49 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 2
  %50 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 3
  %51 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 4
  %52 = load %Array*, %Array** %47, align 8
  %53 = load %String*, %String** %48, align 8
  %54 = load %Result*, %Result** %49, align 8
  %55 = load %Range, %Range* %50, align 4
  %56 = load { i64, i1, double }*, { i64, i1, double }** %51, align 8
  %57 = call i64 @__quantum__rt__array_get_size_1d(%Array* %52)
  %58 = mul i64 %57, 1
  %59 = call i8* @__quantum__rt__memory_allocate(i64 %58)
  %60 = ptrtoint i8* %59 to i64
  %61 = sub i64 %57, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %62 = phi i64 [ 0, %exit__1 ], [ %71, %exiting__2 ]
  %63 = icmp sle i64 %62, %61
  br i1 %63, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %64 = mul i64 %62, 1
  %65 = add i64 %60, %64
  %66 = inttoptr i64 %65 to i8*
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %52, i64 %62)
  %68 = bitcast i8* %67 to i2*
  %69 = load i2, i2* %68, align 1
  %70 = sext i2 %69 to i8
  store i8 %70, i8* %66, align 1
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %71 = add i64 %62, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %72 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8* }* getelementptr ({ i64, i8* }, { i64, i8* }* null, i32 1) to i64))
  %73 = bitcast i8* %72 to { i64, i8* }*
  %74 = getelementptr { i64, i8* }, { i64, i8* }* %73, i64 0, i32 0
  store i64 %57, i64* %74, align 4
  %75 = getelementptr { i64, i8* }, { i64, i8* }* %73, i64 0, i32 1
  store i8* %59, i8** %75, align 8
  %76 = call i32 @__quantum__rt__string_get_length(%String* %53)
  %77 = sext i32 %76 to i64
  %78 = call i8* @__quantum__rt__string_get_data(%String* %53)
  %79 = call i8* @__quantum__rt__memory_allocate(i64 %77)
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %79, i8* %78, i64 %77, i1 false)
  %80 = call %Result* @__quantum__rt__result_get_zero()
  %81 = call i1 @__quantum__rt__result_equal(%Result* %54, %Result* %80)
  %82 = select i1 %81, i8 0, i8 -1
  %83 = extractvalue %Range %55, 0
  %84 = extractvalue %Range %55, 1
  %85 = extractvalue %Range %55, 2
  %86 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i64, i64 }* getelementptr ({ i64, i64, i64 }, { i64, i64, i64 }* null, i32 1) to i64))
  %87 = bitcast i8* %86 to { i64, i64, i64 }*
  %88 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %87, i64 0, i32 0
  store i64 %83, i64* %88, align 4
  %89 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %87, i64 0, i32 1
  store i64 %84, i64* %89, align 4
  %90 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %87, i64 0, i32 2
  store i64 %85, i64* %90, align 4
  %91 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %56, i32 0, i32 0
  %92 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %56, i32 0, i32 1
  %93 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %56, i32 0, i32 2
  %94 = load i64, i64* %91, align 4
  %95 = load i1, i1* %92, align 1
  %96 = load double, double* %93, align 8
  %97 = sext i1 %95 to i8
  %98 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8, double }* getelementptr ({ i64, i8, double }, { i64, i8, double }* null, i32 1) to i64))
  %99 = bitcast i8* %98 to { i64, i8, double }*
  %100 = getelementptr { i64, i8, double }, { i64, i8, double }* %99, i64 0, i32 0
  store i64 %94, i64* %100, align 4
  %101 = getelementptr { i64, i8, double }, { i64, i8, double }* %99, i64 0, i32 1
  store i8 %97, i8* %101, align 1
  %102 = getelementptr { i64, i8, double }, { i64, i8, double }* %99, i64 0, i32 2
  store double %96, double* %102, align 8
  %103 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* getelementptr ({ { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* null, i32 1) to i64))
  %104 = bitcast i8* %103 to { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }*
  %105 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %104, i64 0, i32 0
  store { i64, i8* }* %73, { i64, i8* }** %105, align 8
  %106 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %104, i64 0, i32 1
  store i8* %79, i8** %106, align 8
  %107 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %104, i64 0, i32 2
  store i8 %82, i8* %107, align 1
  %108 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %104, i64 0, i32 3
  store { i64, i64, i64 }* %87, { i64, i64, i64 }** %108, align 8
  %109 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %104, i64 0, i32 4
  store { i64, i8, double }* %99, { i64, i8, double }** %109, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %36, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %37, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %41, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %52, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %54, i32 -1)
  %110 = bitcast { i64, i1, double }* %56 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %110, i32 -1)
  %111 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %111, i32 -1)
  ret { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %104
}
