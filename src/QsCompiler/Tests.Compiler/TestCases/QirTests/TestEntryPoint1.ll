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
  %36 = trunc i64 %34 to i32
  %37 = call %BigInt* @__quantum__rt__bigint_create_array(i32 %36, i8* %35)
  %38 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ double, %BigInt* }* getelementptr ({ double, %BigInt* }, { double, %BigInt* }* null, i32 1) to i64))
  %39 = bitcast %Tuple* %38 to { double, %BigInt* }*
  %40 = getelementptr inbounds { double, %BigInt* }, { double, %BigInt* }* %39, i32 0, i32 0
  %41 = getelementptr inbounds { double, %BigInt* }, { double, %BigInt* }* %39, i32 0, i32 1
  store double %d, double* %40, align 8
  store %BigInt* %37, %BigInt** %41, align 8
  %42 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i1, { double, %BigInt* }* }* getelementptr ({ i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* null, i32 1) to i64))
  %43 = bitcast %Tuple* %42 to { i64, i1, { double, %BigInt* }* }*
  %44 = getelementptr inbounds { i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* %43, i32 0, i32 0
  %45 = getelementptr inbounds { i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* %43, i32 0, i32 1
  %46 = getelementptr inbounds { i64, i1, { double, %BigInt* }* }, { i64, i1, { double, %BigInt* }* }* %43, i32 0, i32 2
  store i64 %cnt, i64* %44, align 4
  store i1 %31, i1* %45, align 1
  store { double, %BigInt* }* %39, { double, %BigInt* }** %46, align 8
  %47 = call { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, %String* %17, %Result* %21, %Range %30, { i64, i1, { double, %BigInt* }* }* %43)
  %48 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 0
  %49 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 1
  %50 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 2
  %51 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 3
  %52 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 4
  %53 = load %Array*, %Array** %48, align 8
  %54 = load %String*, %String** %49, align 8
  %55 = load %Result*, %Result** %50, align 8
  %56 = load %Range, %Range* %51, align 4
  %57 = load { i64, i1, double }*, { i64, i1, double }** %52, align 8
  %58 = call i64 @__quantum__rt__array_get_size_1d(%Array* %53)
  %59 = mul i64 %58, 1
  %60 = call i8* @__quantum__rt__memory_allocate(i64 %59)
  %61 = ptrtoint i8* %60 to i64
  %62 = sub i64 %58, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %63 = phi i64 [ 0, %exit__1 ], [ %72, %exiting__2 ]
  %64 = icmp sle i64 %63, %62
  br i1 %64, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %65 = mul i64 %63, 1
  %66 = add i64 %61, %65
  %67 = inttoptr i64 %66 to i8*
  %68 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %53, i64 %63)
  %69 = bitcast i8* %68 to i2*
  %70 = load i2, i2* %69, align 1
  %71 = sext i2 %70 to i8
  store i8 %71, i8* %67, align 1
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %72 = add i64 %63, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %73 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8* }* getelementptr ({ i64, i8* }, { i64, i8* }* null, i32 1) to i64))
  %74 = bitcast i8* %73 to { i64, i8* }*
  %75 = getelementptr { i64, i8* }, { i64, i8* }* %74, i64 0, i32 0
  store i64 %58, i64* %75, align 4
  %76 = getelementptr { i64, i8* }, { i64, i8* }* %74, i64 0, i32 1
  store i8* %60, i8** %76, align 8
  %77 = call i32 @__quantum__rt__string_get_length(%String* %54)
  %78 = sext i32 %77 to i64
  %79 = call i8* @__quantum__rt__string_get_data(%String* %54)
  %80 = call i8* @__quantum__rt__memory_allocate(i64 %78)
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %80, i8* %79, i64 %78, i1 false)
  %81 = call %Result* @__quantum__rt__result_get_zero()
  %82 = call i1 @__quantum__rt__result_equal(%Result* %55, %Result* %81)
  %83 = select i1 %82, i8 0, i8 -1
  %84 = extractvalue %Range %56, 0
  %85 = extractvalue %Range %56, 1
  %86 = extractvalue %Range %56, 2
  %87 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i64, i64 }* getelementptr ({ i64, i64, i64 }, { i64, i64, i64 }* null, i32 1) to i64))
  %88 = bitcast i8* %87 to { i64, i64, i64 }*
  %89 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %88, i64 0, i32 0
  store i64 %84, i64* %89, align 4
  %90 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %88, i64 0, i32 1
  store i64 %85, i64* %90, align 4
  %91 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %88, i64 0, i32 2
  store i64 %86, i64* %91, align 4
  %92 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %57, i32 0, i32 0
  %93 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %57, i32 0, i32 1
  %94 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %57, i32 0, i32 2
  %95 = load i64, i64* %92, align 4
  %96 = load i1, i1* %93, align 1
  %97 = load double, double* %94, align 8
  %98 = sext i1 %96 to i8
  %99 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8, double }* getelementptr ({ i64, i8, double }, { i64, i8, double }* null, i32 1) to i64))
  %100 = bitcast i8* %99 to { i64, i8, double }*
  %101 = getelementptr { i64, i8, double }, { i64, i8, double }* %100, i64 0, i32 0
  store i64 %95, i64* %101, align 4
  %102 = getelementptr { i64, i8, double }, { i64, i8, double }* %100, i64 0, i32 1
  store i8 %98, i8* %102, align 1
  %103 = getelementptr { i64, i8, double }, { i64, i8, double }* %100, i64 0, i32 2
  store double %97, double* %103, align 8
  %104 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* getelementptr ({ { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* null, i32 1) to i64))
  %105 = bitcast i8* %104 to { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }*
  %106 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %105, i64 0, i32 0
  store { i64, i8* }* %74, { i64, i8* }** %106, align 8
  %107 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %105, i64 0, i32 1
  store i8* %80, i8** %107, align 8
  %108 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %105, i64 0, i32 2
  store i8 %83, i8* %108, align 1
  %109 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %105, i64 0, i32 3
  store { i64, i64, i64 }* %88, { i64, i64, i64 }** %109, align 8
  %110 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %105, i64 0, i32 4
  store { i64, i8, double }* %100, { i64, i8, double }** %110, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %37, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %38, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %42, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %53, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %54, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %55, i32 -1)
  %111 = bitcast { i64, i1, double }* %57 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %111, i32 -1)
  %112 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %112, i32 -1)
  ret { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8, double }* }* %105
}