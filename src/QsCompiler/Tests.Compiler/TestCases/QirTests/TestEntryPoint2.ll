define void @Microsoft__Quantum__Testing__QIR__TestEntryPoint({ i64, i8* }* %arr, i8* %str, i8 %res, { i64, i64, i64 }* %range, i64 %cnt, i8 %b, double %d, { i64, i8* }* %l) #2 {
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
  %48 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %49 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 0
  %50 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 1
  %51 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 2
  %52 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 3
  %53 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47, i32 0, i32 4
  %54 = load %Array*, %Array** %49, align 8
  %55 = load %String*, %String** %50, align 8
  %56 = load %Result*, %Result** %51, align 8
  %57 = load %Range, %Range* %52, align 4
  %58 = load { i64, i1, double }*, { i64, i1, double }** %53, align 8
  %59 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %60 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 1)
  %61 = call i64 @__quantum__rt__array_get_size_1d(%Array* %54)
  %62 = sub i64 %61, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %63 = phi %String* [ %60, %exit__1 ], [ %82, %exiting__2 ]
  %64 = phi i64 [ 0, %exit__1 ], [ %83, %exiting__2 ]
  %65 = icmp sle i64 %64, %62
  br i1 %65, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %66 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %54, i64 %64)
  %67 = bitcast i8* %66 to i2*
  %68 = load i2, i2* %67, align 1
  %69 = icmp ne %String* %63, %60
  br i1 %69, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %body__2
  %70 = call %String* @__quantum__rt__string_concatenate(%String* %63, %String* %59)
  call void @__quantum__rt__string_update_reference_count(%String* %63, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__2
  %71 = phi %String* [ %70, %condTrue__1 ], [ %63, %body__2 ]
  %72 = icmp eq i2 1, %68
  br i1 %72, label %condTrue__2, label %condFalse__1

condTrue__2:                                      ; preds = %condContinue__1
  %73 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @3, i32 0, i32 0))
  br label %condContinue__2

condFalse__1:                                     ; preds = %condContinue__1
  %74 = icmp eq i2 -1, %68
  br i1 %74, label %condTrue__3, label %condFalse__2

condTrue__3:                                      ; preds = %condFalse__1
  %75 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @4, i32 0, i32 0))
  br label %condContinue__3

condFalse__2:                                     ; preds = %condFalse__1
  %76 = icmp eq i2 -2, %68
  br i1 %76, label %condTrue__4, label %condFalse__3

condTrue__4:                                      ; preds = %condFalse__2
  %77 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  br label %condContinue__4

condFalse__3:                                     ; preds = %condFalse__2
  %78 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @6, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__3, %condTrue__4
  %79 = phi %String* [ %77, %condTrue__4 ], [ %78, %condFalse__3 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %80 = phi %String* [ %75, %condTrue__3 ], [ %79, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %81 = phi %String* [ %73, %condTrue__2 ], [ %80, %condContinue__3 ]
  %82 = call %String* @__quantum__rt__string_concatenate(%String* %71, %String* %81)
  call void @__quantum__rt__string_update_reference_count(%String* %71, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %81, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__2
  %83 = add i64 %64, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %84 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %85 = call %String* @__quantum__rt__string_concatenate(%String* %63, %String* %84)
  call void @__quantum__rt__string_update_reference_count(%String* %63, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %59, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 -1)
  %86 = call %String* @__quantum__rt__string_concatenate(%String* %48, %String* %85)
  call void @__quantum__rt__string_update_reference_count(%String* %48, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %85, i32 -1)
  %87 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %88 = call %String* @__quantum__rt__string_concatenate(%String* %86, %String* %87)
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  %89 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @8, i32 0, i32 0))
  %90 = call %String* @__quantum__rt__string_concatenate(%String* %89, %String* %55)
  %91 = call %String* @__quantum__rt__string_concatenate(%String* %90, %String* %89)
  call void @__quantum__rt__string_update_reference_count(%String* %90, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %89, i32 -1)
  %92 = call %String* @__quantum__rt__string_concatenate(%String* %88, %String* %91)
  call void @__quantum__rt__string_update_reference_count(%String* %88, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %91, i32 -1)
  %93 = call %String* @__quantum__rt__string_concatenate(%String* %92, %String* %87)
  call void @__quantum__rt__string_update_reference_count(%String* %92, i32 -1)
  %94 = call %String* @__quantum__rt__result_to_string(%Result* %56)
  %95 = call %String* @__quantum__rt__string_concatenate(%String* %93, %String* %94)
  call void @__quantum__rt__string_update_reference_count(%String* %93, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %94, i32 -1)
  %96 = call %String* @__quantum__rt__string_concatenate(%String* %95, %String* %87)
  call void @__quantum__rt__string_update_reference_count(%String* %95, i32 -1)
  %97 = call %String* @__quantum__rt__range_to_string(%Range %57)
  %98 = call %String* @__quantum__rt__string_concatenate(%String* %96, %String* %97)
  call void @__quantum__rt__string_update_reference_count(%String* %96, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %97, i32 -1)
  %99 = call %String* @__quantum__rt__string_concatenate(%String* %98, %String* %87)
  call void @__quantum__rt__string_update_reference_count(%String* %98, i32 -1)
  %100 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %101 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %58, i32 0, i32 0
  %102 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %58, i32 0, i32 1
  %103 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %58, i32 0, i32 2
  %104 = load i64, i64* %101, align 4
  %105 = load i1, i1* %102, align 1
  %106 = load double, double* %103, align 8
  %107 = call %String* @__quantum__rt__int_to_string(i64 %104)
  %108 = call %String* @__quantum__rt__string_concatenate(%String* %100, %String* %107)
  call void @__quantum__rt__string_update_reference_count(%String* %100, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %107, i32 -1)
  %109 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %110 = call %String* @__quantum__rt__string_concatenate(%String* %108, %String* %109)
  call void @__quantum__rt__string_update_reference_count(%String* %108, i32 -1)
  br i1 %105, label %condTrue__5, label %condFalse__4

condTrue__5:                                      ; preds = %exit__2
  %111 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @9, i32 0, i32 0))
  br label %condContinue__5

condFalse__4:                                     ; preds = %exit__2
  %112 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @10, i32 0, i32 0))
  br label %condContinue__5

condContinue__5:                                  ; preds = %condFalse__4, %condTrue__5
  %113 = phi %String* [ %111, %condTrue__5 ], [ %112, %condFalse__4 ]
  %114 = call %String* @__quantum__rt__string_concatenate(%String* %110, %String* %113)
  call void @__quantum__rt__string_update_reference_count(%String* %110, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %113, i32 -1)
  %115 = call %String* @__quantum__rt__string_concatenate(%String* %114, %String* %109)
  call void @__quantum__rt__string_update_reference_count(%String* %114, i32 -1)
  %116 = call %String* @__quantum__rt__double_to_string(double %106)
  %117 = call %String* @__quantum__rt__string_concatenate(%String* %115, %String* %116)
  call void @__quantum__rt__string_update_reference_count(%String* %115, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %116, i32 -1)
  %118 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %119 = call %String* @__quantum__rt__string_concatenate(%String* %117, %String* %118)
  call void @__quantum__rt__string_update_reference_count(%String* %117, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %118, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %109, i32 -1)
  %120 = call %String* @__quantum__rt__string_concatenate(%String* %99, %String* %119)
  call void @__quantum__rt__string_update_reference_count(%String* %99, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %119, i32 -1)
  %121 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %122 = call %String* @__quantum__rt__string_concatenate(%String* %120, %String* %121)
  call void @__quantum__rt__string_update_reference_count(%String* %120, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %121, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %87, i32 -1)
  call void @__quantum__rt__message(%String* %122)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %37, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %38, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %42, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %54, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %55, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %56, i32 -1)
  %123 = bitcast { i64, i1, double }* %58 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %123, i32 -1)
  %124 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %47 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %124, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %122, i32 -1)
  ret void
}