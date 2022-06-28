define { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__Interop({ i64, i8* }* %arr, i8* %str, i8 %res, { i64, i64, i64 }* %range, i64 %cnt, i8 %b) #0 {
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
  %32 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i1 }* getelementptr ({ i64, i1 }, { i64, i1 }* null, i32 1) to i64))
  %33 = bitcast %Tuple* %32 to { i64, i1 }*
  %34 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %33, i32 0, i32 0
  %35 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %33, i32 0, i32 1
  store i64 %cnt, i64* %34, align 4
  store i1 %31, i1* %35, align 1
  %36 = call { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, %String* %17, %Result* %21, %Range %30, { i64, i1 }* %33)
  %37 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 0
  %38 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 1
  %39 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 2
  %40 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 3
  %41 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 4
  %42 = load %Array*, %Array** %37, align 8
  %43 = load %String*, %String** %38, align 8
  %44 = load %Result*, %Result** %39, align 8
  %45 = load %Range, %Range* %40, align 4
  %46 = load { i64, i1 }*, { i64, i1 }** %41, align 8
  %47 = call i64 @__quantum__rt__array_get_size_1d(%Array* %42)
  %48 = mul i64 %47, 1
  %49 = call i8* @__quantum__rt__memory_allocate(i64 %48)
  %50 = ptrtoint i8* %49 to i64
  %51 = sub i64 %47, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %52 = phi i64 [ 0, %exit__1 ], [ %61, %exiting__2 ]
  %53 = icmp sle i64 %52, %51
  br i1 %53, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %54 = mul i64 %52, 1
  %55 = add i64 %50, %54
  %56 = inttoptr i64 %55 to i8*
  %57 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %42, i64 %52)
  %58 = bitcast i8* %57 to i2*
  %59 = load i2, i2* %58, align 1
  %60 = sext i2 %59 to i8
  store i8 %60, i8* %56, align 1
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %61 = add i64 %52, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %62 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8* }* getelementptr ({ i64, i8* }, { i64, i8* }* null, i32 1) to i64))
  %63 = bitcast i8* %62 to { i64, i8* }*
  %64 = getelementptr { i64, i8* }, { i64, i8* }* %63, i64 0, i32 0
  store i64 %47, i64* %64, align 4
  %65 = getelementptr { i64, i8* }, { i64, i8* }* %63, i64 0, i32 1
  store i8* %49, i8** %65, align 8
  %66 = call i32 @__quantum__rt__string_get_length(%String* %43)
  %67 = sext i32 %66 to i64
  %68 = call i8* @__quantum__rt__string_get_data(%String* %43)
  %69 = call i8* @__quantum__rt__memory_allocate(i64 %67)
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %69, i8* %68, i64 %67, i1 false)
  %70 = call %Result* @__quantum__rt__result_get_zero()
  %71 = call i1 @__quantum__rt__result_equal(%Result* %44, %Result* %70)
  %72 = select i1 %71, i8 0, i8 -1
  %73 = extractvalue %Range %45, 0
  %74 = extractvalue %Range %45, 1
  %75 = extractvalue %Range %45, 2
  %76 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i64, i64 }* getelementptr ({ i64, i64, i64 }, { i64, i64, i64 }* null, i32 1) to i64))
  %77 = bitcast i8* %76 to { i64, i64, i64 }*
  %78 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %77, i64 0, i32 0
  store i64 %73, i64* %78, align 4
  %79 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %77, i64 0, i32 1
  store i64 %74, i64* %79, align 4
  %80 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %77, i64 0, i32 2
  store i64 %75, i64* %80, align 4
  %81 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %46, i32 0, i32 0
  %82 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %46, i32 0, i32 1
  %83 = load i64, i64* %81, align 4
  %84 = load i1, i1* %82, align 1
  %85 = sext i1 %84 to i8
  %86 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8 }* getelementptr ({ i64, i8 }, { i64, i8 }* null, i32 1) to i64))
  %87 = bitcast i8* %86 to { i64, i8 }*
  %88 = getelementptr { i64, i8 }, { i64, i8 }* %87, i64 0, i32 0
  store i64 %83, i64* %88, align 4
  %89 = getelementptr { i64, i8 }, { i64, i8 }* %87, i64 0, i32 1
  store i8 %85, i8* %89, align 1
  %90 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* getelementptr ({ { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* null, i32 1) to i64))
  %91 = bitcast i8* %90 to { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }*
  %92 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* %91, i64 0, i32 0
  store { i64, i8* }* %63, { i64, i8* }** %92, align 8
  %93 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* %91, i64 0, i32 1
  store i8* %69, i8** %93, align 8
  %94 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* %91, i64 0, i32 2
  store i8 %72, i8* %94, align 1
  %95 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* %91, i64 0, i32 3
  store { i64, i64, i64 }* %77, { i64, i64, i64 }** %95, align 8
  %96 = getelementptr { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }, { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* %91, i64 0, i32 4
  store { i64, i8 }* %87, { i64, i8 }** %96, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %32, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %42, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %43, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %44, i32 -1)
  %97 = bitcast { i64, i1 }* %46 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %97, i32 -1)
  %98 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %98, i32 -1)
  ret { { i64, i8* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }* }* %91
}
