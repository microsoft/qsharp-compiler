define { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__Interop({ i64, i64* }* %arr, i8* %str, i8 %res, { i64, i64, i64 }* %range, i64 %cnt, i8 %b, { i64, i8* }* %paulis) #0 {
entry:
  %0 = getelementptr { i64, i64* }, { i64, i64* }* %arr, i64 0, i32 0
  %1 = getelementptr { i64, i64* }, { i64, i64* }* %arr, i64 0, i32 1
  %2 = load i64, i64* %0, align 4
  %3 = load i8*, i64** %1, align 8
  %4 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 %2)
  %5 = ptrtoint i8* %3 to i64
  %6 = sub i64 %2, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %7 = phi i64 [ 0, %entry ], [ %15, %exiting__1 ]
  %8 = icmp sle i64 %7, %6
  br i1 %8, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %9 = mul i64 %7, 8
  %10 = add i64 %5, %9
  %11 = inttoptr i64 %10 to i64*
  %12 = load i64, i64* %11, align 4
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 %7)
  %14 = bitcast i8* %13 to i64*
  store i64 %12, i64* %14, align 4
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %15 = add i64 %7, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %16 = call %String* @__quantum__rt__string_create(i8* %str)
  %17 = icmp eq i8 %res, 0
  %18 = call %Result* @__quantum__rt__result_get_zero()
  %19 = call %Result* @__quantum__rt__result_get_one()
  %20 = select i1 %17, %Result* %18, %Result* %19
  %21 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 0
  %22 = load i64, i64* %21, align 4
  %23 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 1
  %24 = load i64, i64* %23, align 4
  %25 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %range, i64 0, i32 2
  %26 = load i64, i64* %25, align 4
  %27 = load %Range, %Range* @EmptyRange, align 4
  %28 = insertvalue %Range %27, i64 %22, 0
  %29 = insertvalue %Range %28, i64 %24, 1
  %30 = insertvalue %Range %29, i64 %26, 2
  %31 = trunc i8 %b to i1
  %32 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i1 }* getelementptr ({ i64, i1 }, { i64, i1 }* null, i32 1) to i64))
  %33 = bitcast %Tuple* %32 to { i64, i1 }*
  %34 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %33, i32 0, i32 0
  %35 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %33, i32 0, i32 1
  store i64 %cnt, i64* %34, align 4
  store i1 %31, i1* %35, align 1
  %36 = getelementptr { i64, i8* }, { i64, i8* }* %paulis, i64 0, i32 0
  %37 = getelementptr { i64, i8* }, { i64, i8* }* %paulis, i64 0, i32 1
  %38 = load i64, i64* %36, align 4
  %39 = load i8*, i8** %37, align 8
  %40 = call %Array* @__quantum__rt__array_create_1d(i32 1, i64 %38)
  %41 = ptrtoint i8* %39 to i64
  %42 = sub i64 %38, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %43 = phi i64 [ 0, %exit__1 ], [ %52, %exiting__2 ]
  %44 = icmp sle i64 %43, %42
  br i1 %44, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %45 = mul i64 %43, 1
  %46 = add i64 %41, %45
  %47 = inttoptr i64 %46 to i8*
  %48 = load i8, i8* %47, align 1
  %49 = trunc i8 %48 to i2
  %50 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %40, i64 %43)
  %51 = bitcast i8* %50 to i2*
  store i2 %49, i2* %51, align 1
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %52 = add i64 %43, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %53 = call { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, %String* %16, %Result* %20, %Range %30, { i64, i1 }* %33, %Array* %40)
  %54 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 0
  %55 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 1
  %56 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 2
  %57 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 3
  %58 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 4
  %59 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53, i32 0, i32 5
  %60 = load %Array*, %Array** %54, align 8
  %61 = load %String*, %String** %55, align 8
  %62 = load %Result*, %Result** %56, align 8
  %63 = load %Range, %Range* %57, align 4
  %64 = load { i64, i1 }*, { i64, i1 }** %58, align 8
  %65 = load %Array*, %Array** %59, align 8
  %66 = call i64 @__quantum__rt__array_get_size_1d(%Array* %60)
  %67 = mul i64 %66, 8
  %68 = call i8* @__quantum__rt__memory_allocate(i64 %67)
  %69 = ptrtoint i8* %68 to i64
  %70 = sub i64 %66, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %71 = phi i64 [ 0, %exit__2 ], [ %79, %exiting__3 ]
  %72 = icmp sle i64 %71, %70
  br i1 %72, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %73 = mul i64 %71, 8
  %74 = add i64 %69, %73
  %75 = inttoptr i64 %74 to i64*
  %76 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %60, i64 %71)
  %77 = bitcast i8* %76 to i64*
  %78 = load i64, i64* %77, align 4
  store i64 %78, i64* %75, align 4
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %79 = add i64 %71, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  %80 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i64* }* getelementptr ({ i64, i64* }, { i64, i64* }* null, i32 1) to i64))
  %81 = bitcast i8* %80 to { i64, i64* }*
  %82 = getelementptr { i64, i64* }, { i64, i64* }* %81, i64 0, i32 0
  store i64 %66, i64* %82, align 4
  %83 = getelementptr { i64, i64* }, { i64, i64* }* %81, i64 0, i32 1
  %84 = bitcast i8* %68 to i64*
  store i64* %84, i64** %83, align 8
  %85 = call i32 @__quantum__rt__string_get_length(%String* %61)
  %86 = sext i32 %85 to i64
  %87 = call i8* @__quantum__rt__string_get_data(%String* %61)
  %88 = call i8* @__quantum__rt__memory_allocate(i64 %86)
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %88, i8* %87, i64 %86, i1 false)
  %89 = call %Result* @__quantum__rt__result_get_zero()
  %90 = call i1 @__quantum__rt__result_equal(%Result* %62, %Result* %89)
  %91 = select i1 %90, i8 0, i8 -1
  %92 = extractvalue %Range %63, 0
  %93 = extractvalue %Range %63, 1
  %94 = extractvalue %Range %63, 2
  %95 = call i8* @__quantum__rt__memory_allocate(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 3))
  %96 = bitcast i8* %95 to { i64, i64, i64 }*
  %97 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %96, i64 0, i32 0
  store i64 %92, i64* %97, align 4
  %98 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %96, i64 0, i32 1
  store i64 %93, i64* %98, align 4
  %99 = getelementptr { i64, i64, i64 }, { i64, i64, i64 }* %96, i64 0, i32 2
  store i64 %94, i64* %99, align 4
  %100 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %64, i32 0, i32 0
  %101 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %64, i32 0, i32 1
  %102 = load i64, i64* %100, align 4
  %103 = load i1, i1* %101, align 1
  %104 = sext i1 %103 to i8
  %105 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8 }* getelementptr ({ i64, i8 }, { i64, i8 }* null, i32 1) to i64))
  %106 = bitcast i8* %105 to { i64, i8 }*
  %107 = getelementptr { i64, i8 }, { i64, i8 }* %106, i64 0, i32 0
  store i64 %102, i64* %107, align 4
  %108 = getelementptr { i64, i8 }, { i64, i8 }* %106, i64 0, i32 1
  store i8 %104, i8* %108, align 1
  %109 = call i64 @__quantum__rt__array_get_size_1d(%Array* %65)
  %110 = mul i64 %109, 1
  %111 = call i8* @__quantum__rt__memory_allocate(i64 %110)
  %112 = ptrtoint i8* %111 to i64
  %113 = sub i64 %109, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %114 = phi i64 [ 0, %exit__3 ], [ %123, %exiting__4 ]
  %115 = icmp sle i64 %114, %113
  br i1 %115, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %116 = mul i64 %114, 1
  %117 = add i64 %112, %116
  %118 = inttoptr i64 %117 to i8*
  %119 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %65, i64 %114)
  %120 = bitcast i8* %119 to i2*
  %121 = load i2, i2* %120, align 1
  %122 = sext i2 %121 to i8
  store i8 %122, i8* %118, align 1
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %123 = add i64 %114, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  %124 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ i64, i8* }* getelementptr ({ i64, i8* }, { i64, i8* }* null, i32 1) to i64))
  %125 = bitcast i8* %124 to { i64, i8* }*
  %126 = getelementptr { i64, i8* }, { i64, i8* }* %125, i64 0, i32 0
  store i64 %109, i64* %126, align 4
  %127 = getelementptr { i64, i8* }, { i64, i8* }* %125, i64 0, i32 1
  store i8* %111, i8** %127, align 8
  %128 = call i8* @__quantum__rt__memory_allocate(i64 ptrtoint ({ { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* getelementptr ({ { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* null, i32 1) to i64))
  %129 = bitcast i8* %128 to { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }*
  %130 = getelementptr { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129, i64 0, i32 0
  store { i64, i64* }* %81, { i64, i64* }** %130, align 8
  %131 = getelementptr { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129, i64 0, i32 1
  store i8* %88, i8** %131, align 8
  %132 = getelementptr { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129, i64 0, i32 2
  store i8 %91, i8* %132, align 1
  %133 = getelementptr { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129, i64 0, i32 3
  store { i64, i64, i64 }* %96, { i64, i64, i64 }** %133, align 8
  %134 = getelementptr { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129, i64 0, i32 4
  store { i64, i8 }* %106, { i64, i8 }** %134, align 8
  %135 = getelementptr { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }, { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129, i64 0, i32 5
  store { i64, i8* }* %125, { i64, i8* }** %135, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %32, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %40, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %60, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %62, i32 -1)
  %136 = bitcast { i64, i1 }* %64 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %136, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %65, i32 -1)
  %137 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1 }*, %Array* }* %53 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %137, i32 -1)
  ret { { i64, i64* }*, i8*, i8, { i64, i64, i64 }*, { i64, i8 }*, { i64, i8* }* }* %129
}
