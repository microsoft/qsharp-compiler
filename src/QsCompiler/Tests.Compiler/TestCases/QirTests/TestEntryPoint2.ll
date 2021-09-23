define void @Microsoft__Quantum__Testing__QIR__TestEntryPoint({ i64, i8* }* %arr, i8* %str, i8 %res, { i64, i64, i64 }* %range, i64 %cnt, i8 %b) #2 {
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
  %28 = load %Range, %Range* @EmptyRange, align 4
  %29 = insertvalue %Range %28, i64 %23, 0
  %30 = insertvalue %Range %29, i64 %25, 1
  %31 = insertvalue %Range %30, i64 %27, 2
  %32 = trunc i8 %b to i1
  %33 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i1 }* getelementptr ({ i64, i1 }, { i64, i1 }* null, i32 1) to i64))
  %34 = bitcast %Tuple* %33 to { i64, i1 }*
  %35 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %34, i32 0, i32 0
  %36 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %34, i32 0, i32 1
  store i64 %cnt, i64* %35, align 4
  store i1 %32, i1* %36, align 1
  %37 = call { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* @Microsoft__Quantum__Testing__QIR__TestEntryPoint__body(%Array* %4, %String* %17, %Result* %21, %Range %31, { i64, i1 }* %34)
  %38 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %39 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37, i32 0, i32 0
  %40 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37, i32 0, i32 1
  %41 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37, i32 0, i32 2
  %42 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37, i32 0, i32 3
  %43 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37, i32 0, i32 4
  %44 = load %Array*, %Array** %39, align 8
  %45 = load %String*, %String** %40, align 8
  %46 = load %Result*, %Result** %41, align 8
  %47 = load %Range, %Range* %42, align 4
  %48 = load { i64, i1 }*, { i64, i1 }** %43, align 8
  %49 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %50 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %50, i32 1)
  %51 = call i64 @__quantum__rt__array_get_size_1d(%Array* %44)
  %52 = sub i64 %51, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %53 = phi %String* [ %50, %exit__1 ], [ %75, %exiting__2 ]
  %54 = phi i64 [ 0, %exit__1 ], [ %76, %exiting__2 ]
  %55 = icmp sle i64 %54, %52
  br i1 %55, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %56 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %44, i64 %54)
  %57 = bitcast i8* %56 to i2*
  %58 = load i2, i2* %57, align 1
  %59 = icmp ne %String* %53, %50
  br i1 %59, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %body__2
  %60 = call %String* @__quantum__rt__string_concatenate(%String* %53, %String* %49)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__2
  %61 = phi %String* [ %60, %condTrue__1 ], [ %53, %body__2 ]
  %62 = load i2, i2* @PauliX, align 1
  %63 = icmp eq i2 %62, %58
  br i1 %63, label %condTrue__2, label %condFalse__1

condTrue__2:                                      ; preds = %condContinue__1
  %64 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @3, i32 0, i32 0))
  br label %condContinue__2

condFalse__1:                                     ; preds = %condContinue__1
  %65 = load i2, i2* @PauliY, align 1
  %66 = icmp eq i2 %65, %58
  br i1 %66, label %condTrue__3, label %condFalse__2

condTrue__3:                                      ; preds = %condFalse__1
  %67 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @4, i32 0, i32 0))
  br label %condContinue__3

condFalse__2:                                     ; preds = %condFalse__1
  %68 = load i2, i2* @PauliZ, align 1
  %69 = icmp eq i2 %68, %58
  br i1 %69, label %condTrue__4, label %condFalse__3

condTrue__4:                                      ; preds = %condFalse__2
  %70 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  br label %condContinue__4

condFalse__3:                                     ; preds = %condFalse__2
  %71 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @6, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__3, %condTrue__4
  %72 = phi %String* [ %70, %condTrue__4 ], [ %71, %condFalse__3 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %73 = phi %String* [ %67, %condTrue__3 ], [ %72, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %74 = phi %String* [ %64, %condTrue__2 ], [ %73, %condContinue__3 ]
  %75 = call %String* @__quantum__rt__string_concatenate(%String* %61, %String* %74)
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %74, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__2
  %76 = add i64 %54, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %77 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %78 = call %String* @__quantum__rt__string_concatenate(%String* %53, %String* %77)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %77, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %50, i32 -1)
  %79 = call %String* @__quantum__rt__string_concatenate(%String* %38, %String* %78)
  call void @__quantum__rt__string_update_reference_count(%String* %38, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %78, i32 -1)
  %80 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %81 = call %String* @__quantum__rt__string_concatenate(%String* %79, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %79, i32 -1)
  %82 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @8, i32 0, i32 0))
  %83 = call %String* @__quantum__rt__string_concatenate(%String* %82, %String* %45)
  %84 = call %String* @__quantum__rt__string_concatenate(%String* %83, %String* %82)
  call void @__quantum__rt__string_update_reference_count(%String* %83, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %82, i32 -1)
  %85 = call %String* @__quantum__rt__string_concatenate(%String* %81, %String* %84)
  call void @__quantum__rt__string_update_reference_count(%String* %81, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  %86 = call %String* @__quantum__rt__string_concatenate(%String* %85, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %85, i32 -1)
  %87 = call %String* @__quantum__rt__result_to_string(%Result* %46)
  %88 = call %String* @__quantum__rt__string_concatenate(%String* %86, %String* %87)
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %87, i32 -1)
  %89 = call %String* @__quantum__rt__string_concatenate(%String* %88, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %88, i32 -1)
  %90 = call %String* @__quantum__rt__range_to_string(%Range %47)
  %91 = call %String* @__quantum__rt__string_concatenate(%String* %89, %String* %90)
  call void @__quantum__rt__string_update_reference_count(%String* %89, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %90, i32 -1)
  %92 = call %String* @__quantum__rt__string_concatenate(%String* %91, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %91, i32 -1)
  %93 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %94 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %48, i32 0, i32 0
  %95 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %48, i32 0, i32 1
  %96 = load i64, i64* %94, align 4
  %97 = load i1, i1* %95, align 1
  %98 = call %String* @__quantum__rt__int_to_string(i64 %96)
  %99 = call %String* @__quantum__rt__string_concatenate(%String* %93, %String* %98)
  call void @__quantum__rt__string_update_reference_count(%String* %93, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %98, i32 -1)
  %100 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %101 = call %String* @__quantum__rt__string_concatenate(%String* %99, %String* %100)
  call void @__quantum__rt__string_update_reference_count(%String* %99, i32 -1)
  br i1 %97, label %condTrue__5, label %condFalse__4

condTrue__5:                                      ; preds = %exit__2
  %102 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @9, i32 0, i32 0))
  br label %condContinue__5

condFalse__4:                                     ; preds = %exit__2
  %103 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @10, i32 0, i32 0))
  br label %condContinue__5

condContinue__5:                                  ; preds = %condFalse__4, %condTrue__5
  %104 = phi %String* [ %102, %condTrue__5 ], [ %103, %condFalse__4 ]
  %105 = call %String* @__quantum__rt__string_concatenate(%String* %101, %String* %104)
  call void @__quantum__rt__string_update_reference_count(%String* %101, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %104, i32 -1)
  %106 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %107 = call %String* @__quantum__rt__string_concatenate(%String* %105, %String* %106)
  call void @__quantum__rt__string_update_reference_count(%String* %105, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %106, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %100, i32 -1)
  %108 = call %String* @__quantum__rt__string_concatenate(%String* %92, %String* %107)
  call void @__quantum__rt__string_update_reference_count(%String* %92, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %107, i32 -1)
  %109 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %110 = call %String* @__quantum__rt__string_concatenate(%String* %108, %String* %109)
  call void @__quantum__rt__string_update_reference_count(%String* %108, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %109, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %80, i32 -1)
  call void @__quantum__rt__message(%String* %110)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %33, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %44, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %46, i32 -1)
  %111 = bitcast { i64, i1 }* %48 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %111, i32 -1)
  %112 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %112, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %110, i32 -1)
  ret void
}
