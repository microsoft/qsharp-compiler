define internal %String* @Microsoft__Quantum__Testing__QIR__TestStrings__body(i64 %a, i64 %b, %Array* %arr) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %1 = call %String* @__quantum__rt__int_to_string(i64 %a)
  %2 = call %String* @__quantum__rt__string_concatenate(%String* %0, %String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  %3 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i32 0, i32 0))
  %x = call %String* @__quantum__rt__string_concatenate(%String* %2, %String* %3)
  call void @__quantum__rt__string_update_reference_count(%String* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %3, i32 -1)
  %4 = add i64 %a, %b
  %5 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @2, i32 0, i32 0))
  %6 = call %String* @__quantum__rt__int_to_string(i64 %4)
  %y = call %String* @__quantum__rt__string_concatenate(%String* %5, %String* %6)
  call void @__quantum__rt__string_update_reference_count(%String* %5, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %6, i32 -1)
  %7 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %8 = call %String* @__quantum__rt__string_concatenate(%String* %7, %String* %y)
  %z = call %String* @__quantum__rt__string_concatenate(%String* %8, %String* %7)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %7, i32 -1)
  %9 = load i2, i2* @PauliX, align 1
  %10 = call %Result* @__quantum__rt__result_get_one()
  %11 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 1)
  %12 = load %Range, %Range* @EmptyRange, align 4
  %13 = insertvalue %Range %12, i64 0, 0
  %14 = insertvalue %Range %13, i64 1, 1
  %15 = insertvalue %Range %14, i64 3, 2
  %16 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([17 x i8], [17 x i8]* @4, i32 0, i32 0))
  %17 = call %String* @__quantum__rt__double_to_string(double 1.200000e+00)
  %18 = call %String* @__quantum__rt__string_concatenate(%String* %16, %String* %17)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  %19 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  %20 = call %String* @__quantum__rt__string_concatenate(%String* %18, %String* %19)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 -1)
  br i1 true, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %21 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @6, i32 0, i32 0))
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %22 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @7, i32 0, i32 0))
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %23 = phi %String* [ %21, %condTrue__1 ], [ %22, %condFalse__1 ]
  %24 = call %String* @__quantum__rt__string_concatenate(%String* %20, %String* %23)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %23, i32 -1)
  %25 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @8, i32 0, i32 0))
  %26 = call %String* @__quantum__rt__string_concatenate(%String* %24, %String* %25)
  call void @__quantum__rt__string_update_reference_count(%String* %24, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %25, i32 -1)
  %27 = load i2, i2* @PauliX, align 1
  %28 = icmp eq i2 %27, %9
  br i1 %28, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condContinue__1
  %29 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @9, i32 0, i32 0))
  br label %condContinue__2

condFalse__2:                                     ; preds = %condContinue__1
  %30 = load i2, i2* @PauliY, align 1
  %31 = icmp eq i2 %30, %9
  br i1 %31, label %condTrue__3, label %condFalse__3

condTrue__3:                                      ; preds = %condFalse__2
  %32 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @10, i32 0, i32 0))
  br label %condContinue__3

condFalse__3:                                     ; preds = %condFalse__2
  %33 = load i2, i2* @PauliZ, align 1
  %34 = icmp eq i2 %33, %9
  br i1 %34, label %condTrue__4, label %condFalse__4

condTrue__4:                                      ; preds = %condFalse__3
  %35 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @11, i32 0, i32 0))
  br label %condContinue__4

condFalse__4:                                     ; preds = %condFalse__3
  %36 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @12, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__4, %condTrue__4
  %37 = phi %String* [ %35, %condTrue__4 ], [ %36, %condFalse__4 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %38 = phi %String* [ %32, %condTrue__3 ], [ %37, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %39 = phi %String* [ %29, %condTrue__2 ], [ %38, %condContinue__3 ]
  %40 = call %String* @__quantum__rt__string_concatenate(%String* %26, %String* %39)
  call void @__quantum__rt__string_update_reference_count(%String* %26, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %39, i32 -1)
  %41 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @13, i32 0, i32 0))
  %42 = call %String* @__quantum__rt__string_concatenate(%String* %40, %String* %41)
  call void @__quantum__rt__string_update_reference_count(%String* %40, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %41, i32 -1)
  %43 = call %String* @__quantum__rt__result_to_string(%Result* %10)
  %44 = call %String* @__quantum__rt__string_concatenate(%String* %42, %String* %43)
  call void @__quantum__rt__string_update_reference_count(%String* %42, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %43, i32 -1)
  %45 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @14, i32 0, i32 0))
  %46 = call %String* @__quantum__rt__string_concatenate(%String* %44, %String* %45)
  call void @__quantum__rt__string_update_reference_count(%String* %44, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  %47 = call %String* @__quantum__rt__bigint_to_string(%BigInt* %11)
  %48 = call %String* @__quantum__rt__string_concatenate(%String* %46, %String* %47)
  call void @__quantum__rt__string_update_reference_count(%String* %46, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %47, i32 -1)
  %49 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @15, i32 0, i32 0))
  %50 = call %String* @__quantum__rt__string_concatenate(%String* %48, %String* %49)
  call void @__quantum__rt__string_update_reference_count(%String* %48, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 -1)
  %51 = call %String* @__quantum__rt__range_to_string(%Range %15)
  %i = call %String* @__quantum__rt__string_concatenate(%String* %50, %String* %51)
  call void @__quantum__rt__string_update_reference_count(%String* %50, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %51, i32 -1)
  %52 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @16, i32 0, i32 0))
  %53 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @17, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 1)
  %54 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %55 = sub i64 %54, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %condContinue__2
  %56 = phi %String* [ %53, %condContinue__2 ], [ %66, %exiting__1 ]
  %57 = phi i64 [ 0, %condContinue__2 ], [ %67, %exiting__1 ]
  %58 = icmp sle i64 %57, %55
  br i1 %58, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %59 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %57)
  %60 = bitcast i8* %59 to i64*
  %61 = load i64, i64* %60, align 4
  %62 = icmp ne %String* %56, %53
  br i1 %62, label %condTrue__5, label %condContinue__5

condTrue__5:                                      ; preds = %body__1
  %63 = call %String* @__quantum__rt__string_concatenate(%String* %56, %String* %52)
  call void @__quantum__rt__string_update_reference_count(%String* %56, i32 -1)
  br label %condContinue__5

condContinue__5:                                  ; preds = %condTrue__5, %body__1
  %64 = phi %String* [ %63, %condTrue__5 ], [ %56, %body__1 ]
  %65 = call %String* @__quantum__rt__int_to_string(i64 %61)
  %66 = call %String* @__quantum__rt__string_concatenate(%String* %64, %String* %65)
  call void @__quantum__rt__string_update_reference_count(%String* %64, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %65, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__5
  %67 = add i64 %57, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %68 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @18, i32 0, i32 0))
  %data = call %String* @__quantum__rt__string_concatenate(%String* %56, %String* %68)
  call void @__quantum__rt__string_update_reference_count(%String* %56, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %68, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  %69 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @19, i32 0, i32 0))
  %70 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @20, i32 0, i32 0))
  %71 = call %String* @__quantum__rt__string_concatenate(%String* %70, %String* %x)
  %72 = call %String* @__quantum__rt__string_concatenate(%String* %71, %String* %70)
  call void @__quantum__rt__string_update_reference_count(%String* %71, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %70, i32 -1)
  %res = call %String* @__quantum__rt__string_concatenate(%String* %69, %String* %72)
  call void @__quantum__rt__string_update_reference_count(%String* %69, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %72, i32 -1)
  %73 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @21, i32 0, i32 0))
  %defaultArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %74 = phi i64 [ 0, %exit__1 ], [ %78, %exiting__2 ]
  %75 = icmp sle i64 %74, 0
  br i1 %75, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %76 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %74)
  %77 = bitcast i8* %76 to %String**
  store %String* %73, %String** %77, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %73, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %78 = add i64 %74, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 1)
  %79 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @22, i32 0, i32 0))
  %strArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %80 = phi i64 [ 0, %exit__2 ], [ %84, %exiting__3 ]
  %81 = icmp sle i64 %80, 0
  br i1 %81, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %82 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %80)
  %83 = bitcast i8* %82 to %String**
  store %String* %79, %String** %83, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %79, i32 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %84 = add i64 %80, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 1)
  %85 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @23, i32 0, i32 0))
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %x, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %y, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %z, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %11, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %i, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %data, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %res, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %73, i32 -1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %86 = phi i64 [ 0, %exit__3 ], [ %91, %exiting__4 ]
  %87 = icmp sle i64 %86, 0
  br i1 %87, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %88 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %86)
  %89 = bitcast i8* %88 to %String**
  %90 = load %String*, %String** %89, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %90, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %91 = add i64 %86, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %79, i32 -1)
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %92 = phi i64 [ 0, %exit__4 ], [ %97, %exiting__5 ]
  %93 = icmp sle i64 %92, 0
  br i1 %93, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %94 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %92)
  %95 = bitcast i8* %94 to %String**
  %96 = load %String*, %String** %95, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %96, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %97 = add i64 %92, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_reference_count(%Array* %strArr, i32 -1)
  ret %String* %85
}
