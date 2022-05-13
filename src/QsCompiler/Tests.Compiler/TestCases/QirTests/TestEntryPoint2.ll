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
  %37 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %38 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 0
  %39 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 1
  %40 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 2
  %41 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 3
  %42 = getelementptr inbounds { %Array*, %String*, %Result*, %Range, { i64, i1 }* }, { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36, i32 0, i32 4
  %43 = load %Array*, %Array** %38, align 8
  %44 = load %String*, %String** %39, align 8
  %45 = load %Result*, %Result** %40, align 8
  %46 = load %Range, %Range* %41, align 4
  %47 = load { i64, i1 }*, { i64, i1 }** %42, align 8
  %48 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %49 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 1)
  %50 = call i64 @__quantum__rt__array_get_size_1d(%Array* %43)
  %51 = sub i64 %50, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %52 = phi %String* [ %49, %exit__1 ], [ %71, %exiting__2 ]
  %53 = phi i64 [ 0, %exit__1 ], [ %72, %exiting__2 ]
  %54 = icmp sle i64 %53, %51
  br i1 %54, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %55 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %43, i64 %53)
  %56 = bitcast i8* %55 to i2*
  %57 = load i2, i2* %56, align 1
  %58 = icmp ne %String* %52, %49
  br i1 %58, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %body__2
  %59 = call %String* @__quantum__rt__string_concatenate(%String* %52, %String* %48)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__2
  %60 = phi %String* [ %59, %condTrue__1 ], [ %52, %body__2 ]
  %61 = icmp eq i2 1, %57
  br i1 %61, label %condTrue__2, label %condFalse__1

condTrue__2:                                      ; preds = %condContinue__1
  %62 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @3, i32 0, i32 0))
  br label %condContinue__2

condFalse__1:                                     ; preds = %condContinue__1
  %63 = icmp eq i2 -1, %57
  br i1 %63, label %condTrue__3, label %condFalse__2

condTrue__3:                                      ; preds = %condFalse__1
  %64 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @4, i32 0, i32 0))
  br label %condContinue__3

condFalse__2:                                     ; preds = %condFalse__1
  %65 = icmp eq i2 -2, %57
  br i1 %65, label %condTrue__4, label %condFalse__3

condTrue__4:                                      ; preds = %condFalse__2
  %66 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  br label %condContinue__4

condFalse__3:                                     ; preds = %condFalse__2
  %67 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @6, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__3, %condTrue__4
  %68 = phi %String* [ %66, %condTrue__4 ], [ %67, %condFalse__3 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %69 = phi %String* [ %64, %condTrue__3 ], [ %68, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %70 = phi %String* [ %62, %condTrue__2 ], [ %69, %condContinue__3 ]
  %71 = call %String* @__quantum__rt__string_concatenate(%String* %60, %String* %70)
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %70, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %condContinue__2
  %72 = add i64 %53, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %73 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %74 = call %String* @__quantum__rt__string_concatenate(%String* %52, %String* %73)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %73, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %48, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 -1)
  %75 = call %String* @__quantum__rt__string_concatenate(%String* %37, %String* %74)
  call void @__quantum__rt__string_update_reference_count(%String* %37, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %74, i32 -1)
  %76 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %77 = call %String* @__quantum__rt__string_concatenate(%String* %75, %String* %76)
  call void @__quantum__rt__string_update_reference_count(%String* %75, i32 -1)
  %78 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @8, i32 0, i32 0))
  %79 = call %String* @__quantum__rt__string_concatenate(%String* %78, %String* %44)
  %80 = call %String* @__quantum__rt__string_concatenate(%String* %79, %String* %78)
  call void @__quantum__rt__string_update_reference_count(%String* %79, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %78, i32 -1)
  %81 = call %String* @__quantum__rt__string_concatenate(%String* %77, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %77, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %80, i32 -1)
  %82 = call %String* @__quantum__rt__string_concatenate(%String* %81, %String* %76)
  call void @__quantum__rt__string_update_reference_count(%String* %81, i32 -1)
  %83 = call %String* @__quantum__rt__result_to_string(%Result* %45)
  %84 = call %String* @__quantum__rt__string_concatenate(%String* %82, %String* %83)
  call void @__quantum__rt__string_update_reference_count(%String* %82, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %83, i32 -1)
  %85 = call %String* @__quantum__rt__string_concatenate(%String* %84, %String* %76)
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  %86 = call %String* @__quantum__rt__range_to_string(%Range %46)
  %87 = call %String* @__quantum__rt__string_concatenate(%String* %85, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %85, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  %88 = call %String* @__quantum__rt__string_concatenate(%String* %87, %String* %76)
  call void @__quantum__rt__string_update_reference_count(%String* %87, i32 -1)
  %89 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0))
  %90 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %47, i32 0, i32 0
  %91 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %47, i32 0, i32 1
  %92 = load i64, i64* %90, align 4
  %93 = load i1, i1* %91, align 1
  %94 = call %String* @__quantum__rt__int_to_string(i64 %92)
  %95 = call %String* @__quantum__rt__string_concatenate(%String* %89, %String* %94)
  call void @__quantum__rt__string_update_reference_count(%String* %89, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %94, i32 -1)
  %96 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i32 0, i32 0))
  %97 = call %String* @__quantum__rt__string_concatenate(%String* %95, %String* %96)
  call void @__quantum__rt__string_update_reference_count(%String* %95, i32 -1)
  br i1 %93, label %condTrue__5, label %condFalse__4

condTrue__5:                                      ; preds = %exit__2
  %98 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @9, i32 0, i32 0))
  br label %condContinue__5

condFalse__4:                                     ; preds = %exit__2
  %99 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @10, i32 0, i32 0))
  br label %condContinue__5

condContinue__5:                                  ; preds = %condFalse__4, %condTrue__5
  %100 = phi %String* [ %98, %condTrue__5 ], [ %99, %condFalse__4 ]
  %101 = call %String* @__quantum__rt__string_concatenate(%String* %97, %String* %100)
  call void @__quantum__rt__string_update_reference_count(%String* %97, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %100, i32 -1)
  %102 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %103 = call %String* @__quantum__rt__string_concatenate(%String* %101, %String* %102)
  call void @__quantum__rt__string_update_reference_count(%String* %101, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %102, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %96, i32 -1)
  %104 = call %String* @__quantum__rt__string_concatenate(%String* %88, %String* %103)
  call void @__quantum__rt__string_update_reference_count(%String* %88, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %103, i32 -1)
  %105 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @11, i32 0, i32 0))
  %106 = call %String* @__quantum__rt__string_concatenate(%String* %104, %String* %105)
  call void @__quantum__rt__string_update_reference_count(%String* %104, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %105, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %76, i32 -1)
  call void @__quantum__rt__message(%String* %106)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %32, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %43, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %44, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %45, i32 -1)
  %107 = bitcast { i64, i1 }* %47 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %107, i32 -1)
  %108 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %36 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %108, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %106, i32 -1)
  ret void
}
