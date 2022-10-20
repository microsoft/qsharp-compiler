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
  %47 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %48 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 0
  %49 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 1
  %50 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 2
  %51 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 3
  %52 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }, { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46, i32 0, i32 4
  %53 = load %Array*, %Array** %48, align 8
  %54 = load %String*, %String** %49, align 8
  %55 = load %Result*, %Result** %50, align 8
  %56 = load %Range, %Range* %51, align 4
  %57 = load { i64, i1, double }*, { i64, i1, double }** %52, align 8
  %58 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %59 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %59, i32 1)
  %60 = call i64 @__quantum__rt__array_get_size_1d(%Array* %53)
  %61 = sub i64 %60, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %62 = phi %String* [ %59, %exit__1 ], [ %81, %exiting__2 ]
  %63 = phi i64 [ 0, %exit__1 ], [ %82, %exiting__2 ]
  %64 = icmp sle i64 %63, %61
  br i1 %64, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %65 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %53, i64 %63)
  %66 = bitcast i8* %65 to i2*
  %67 = load i2, i2* %66, align 1
  %68 = icmp ne %String* %62, %59
  br i1 %68, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %body__2
  %69 = call %String* @__quantum__rt__string_concatenate(%String* %62, %String* %58)
  call void @__quantum__rt__string_update_reference_count(%String* %62, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__2
  %70 = phi %String* [ %69, %condTrue__1 ], [ %62, %body__2 ]
  %71 = icmp eq i2 1, %67
  br i1 %71, label %condTrue__2, label %condFalse__1

condTrue__2:                                      ; preds = %condContinue__1
  %72 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @3, i32 0, i32 0))
  br label %condContinue__2

condFalse__1:                                     ; preds = %condContinue__1
  %73 = icmp eq i2 -1, %67
  br i1 %73, label %condTrue__3, label %condFalse__2

condTrue__3:                                      ; preds = %condFalse__1
  %74 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @4, i32 0, i32 0))
  br label %condContinue__3

condFalse__2:                                     ; preds = %condFalse__1
  %75 = icmp eq i2 -2, %67
  br i1 %75, label %condTrue__4, label %condFalse__3

condTrue__4:                                      ; preds = %condFalse__2
  %76 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  br label %condContinue__4

condFalse__3:                                     ; preds = %condFalse__2
  %77 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @6, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__3, %condTrue__4
  %78 = phi %String* [ %76, %condTrue__4 ], [ %77, %condFalse__3 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %79 = phi %String* [ %74, %condTrue__3 ], [ %78, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %80 = phi %String* [ %72, %condTrue__2 ], [ %79, %condContinue__3 ]
  %81 = call %String* @__quantum__rt__string_concatenate(%String* %70, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %70, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %80, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__2
  %82 = add i64 %63, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %83 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %84 = call %String* @__quantum__rt__string_concatenate(%String* %62, %String* %83)
  call void @__quantum__rt__string_update_reference_count(%String* %62, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %83, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %58, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %59, i32 -1)
  %85 = call %String* @__quantum__rt__string_concatenate(%String* %47, %String* %84)
  call void @__quantum__rt__string_update_reference_count(%String* %47, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  %86 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %87 = call %String* @__quantum__rt__string_concatenate(%String* %85, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %85, i32 -1)
  %88 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @8, i32 0, i32 0))
  %89 = call %String* @__quantum__rt__string_concatenate(%String* %88, %String* %54)
  %90 = call %String* @__quantum__rt__string_concatenate(%String* %89, %String* %88)
  call void @__quantum__rt__string_update_reference_count(%String* %89, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %88, i32 -1)
  %91 = call %String* @__quantum__rt__string_concatenate(%String* %87, %String* %90)
  call void @__quantum__rt__string_update_reference_count(%String* %87, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %90, i32 -1)
  %92 = call %String* @__quantum__rt__string_concatenate(%String* %91, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %91, i32 -1)
  %93 = call %String* @__quantum__rt__result_to_string(%Result* %55)
  %94 = call %String* @__quantum__rt__string_concatenate(%String* %92, %String* %93)
  call void @__quantum__rt__string_update_reference_count(%String* %92, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %93, i32 -1)
  %95 = call %String* @__quantum__rt__string_concatenate(%String* %94, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %94, i32 -1)
  %96 = call %String* @__quantum__rt__range_to_string(%Range %56)
  %97 = call %String* @__quantum__rt__string_concatenate(%String* %95, %String* %96)
  call void @__quantum__rt__string_update_reference_count(%String* %95, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %96, i32 -1)
  %98 = call %String* @__quantum__rt__string_concatenate(%String* %97, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %97, i32 -1)
  %99 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %100 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %57, i32 0, i32 0
  %101 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %57, i32 0, i32 1
  %102 = getelementptr inbounds { i64, i1, double }, { i64, i1, double }* %57, i32 0, i32 2
  %103 = load i64, i64* %100, align 4
  %104 = load i1, i1* %101, align 1
  %105 = load double, double* %102, align 8
  %106 = call %String* @__quantum__rt__int_to_string(i64 %103)
  %107 = call %String* @__quantum__rt__string_concatenate(%String* %99, %String* %106)
  call void @__quantum__rt__string_update_reference_count(%String* %99, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %106, i32 -1)
  %108 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %109 = call %String* @__quantum__rt__string_concatenate(%String* %107, %String* %108)
  call void @__quantum__rt__string_update_reference_count(%String* %107, i32 -1)
  br i1 %104, label %condTrue__5, label %condFalse__4

condTrue__5:                                      ; preds = %exit__2
  %110 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @9, i32 0, i32 0))
  br label %condContinue__5

condFalse__4:                                     ; preds = %exit__2
  %111 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @10, i32 0, i32 0))
  br label %condContinue__5

condContinue__5:                                  ; preds = %condFalse__4, %condTrue__5
  %112 = phi %String* [ %110, %condTrue__5 ], [ %111, %condFalse__4 ]
  %113 = call %String* @__quantum__rt__string_concatenate(%String* %109, %String* %112)
  call void @__quantum__rt__string_update_reference_count(%String* %109, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %112, i32 -1)
  %114 = call %String* @__quantum__rt__string_concatenate(%String* %113, %String* %108)
  call void @__quantum__rt__string_update_reference_count(%String* %113, i32 -1)
  %115 = call %String* @__quantum__rt__double_to_string(double %105)
  %116 = call %String* @__quantum__rt__string_concatenate(%String* %114, %String* %115)
  call void @__quantum__rt__string_update_reference_count(%String* %114, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %115, i32 -1)
  %117 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %118 = call %String* @__quantum__rt__string_concatenate(%String* %116, %String* %117)
  call void @__quantum__rt__string_update_reference_count(%String* %116, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %117, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %108, i32 -1)
  %119 = call %String* @__quantum__rt__string_concatenate(%String* %98, %String* %118)
  call void @__quantum__rt__string_update_reference_count(%String* %98, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %118, i32 -1)
  %120 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %121 = call %String* @__quantum__rt__string_concatenate(%String* %119, %String* %120)
  call void @__quantum__rt__string_update_reference_count(%String* %119, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %120, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  call void @__quantum__rt__message(%String* %121)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %36, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %37, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %41, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %53, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %54, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %55, i32 -1)
  %122 = bitcast { i64, i1, double }* %57 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %122, i32 -1)
  %123 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1, double }* }* %46 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %123, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %121, i32 -1)
  ret void
}
