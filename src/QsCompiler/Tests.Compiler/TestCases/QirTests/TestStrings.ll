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
  %12 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([17 x i8], [17 x i8]* @4, i32 0, i32 0))
  %13 = call %String* @__quantum__rt__double_to_string(double 1.200000e+00)
  %14 = call %String* @__quantum__rt__string_concatenate(%String* %12, %String* %13)
  call void @__quantum__rt__string_update_reference_count(%String* %12, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i32 -1)
  %15 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  %16 = call %String* @__quantum__rt__string_concatenate(%String* %14, %String* %15)
  call void @__quantum__rt__string_update_reference_count(%String* %14, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  br i1 true, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %17 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @6, i32 0, i32 0))
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %18 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @7, i32 0, i32 0))
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %19 = phi %String* [ %17, %condTrue__1 ], [ %18, %condFalse__1 ]
  %20 = call %String* @__quantum__rt__string_concatenate(%String* %16, %String* %19)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 -1)
  %21 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @8, i32 0, i32 0))
  %22 = call %String* @__quantum__rt__string_concatenate(%String* %20, %String* %21)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i32 -1)
  %23 = load i2, i2* @PauliX, align 1
  %24 = icmp eq i2 %23, %9
  br i1 %24, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condContinue__1
  %25 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @9, i32 0, i32 0))
  br label %condContinue__2

condFalse__2:                                     ; preds = %condContinue__1
  %26 = load i2, i2* @PauliY, align 1
  %27 = icmp eq i2 %26, %9
  br i1 %27, label %condTrue__3, label %condFalse__3

condTrue__3:                                      ; preds = %condFalse__2
  %28 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @10, i32 0, i32 0))
  br label %condContinue__3

condFalse__3:                                     ; preds = %condFalse__2
  %29 = load i2, i2* @PauliZ, align 1
  %30 = icmp eq i2 %29, %9
  br i1 %30, label %condTrue__4, label %condFalse__4

condTrue__4:                                      ; preds = %condFalse__3
  %31 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @11, i32 0, i32 0))
  br label %condContinue__4

condFalse__4:                                     ; preds = %condFalse__3
  %32 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @12, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__4, %condTrue__4
  %33 = phi %String* [ %31, %condTrue__4 ], [ %32, %condFalse__4 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %34 = phi %String* [ %28, %condTrue__3 ], [ %33, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %35 = phi %String* [ %25, %condTrue__2 ], [ %34, %condContinue__3 ]
  %36 = call %String* @__quantum__rt__string_concatenate(%String* %22, %String* %35)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %35, i32 -1)
  %37 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @13, i32 0, i32 0))
  %38 = call %String* @__quantum__rt__string_concatenate(%String* %36, %String* %37)
  call void @__quantum__rt__string_update_reference_count(%String* %36, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %37, i32 -1)
  %39 = call %String* @__quantum__rt__result_to_string(%Result* %10)
  %40 = call %String* @__quantum__rt__string_concatenate(%String* %38, %String* %39)
  call void @__quantum__rt__string_update_reference_count(%String* %38, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %39, i32 -1)
  %41 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @14, i32 0, i32 0))
  %42 = call %String* @__quantum__rt__string_concatenate(%String* %40, %String* %41)
  call void @__quantum__rt__string_update_reference_count(%String* %40, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %41, i32 -1)
  %43 = call %String* @__quantum__rt__bigint_to_string(%BigInt* %11)
  %44 = call %String* @__quantum__rt__string_concatenate(%String* %42, %String* %43)
  call void @__quantum__rt__string_update_reference_count(%String* %42, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %43, i32 -1)
  %45 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @15, i32 0, i32 0))
  %46 = call %String* @__quantum__rt__string_concatenate(%String* %44, %String* %45)
  call void @__quantum__rt__string_update_reference_count(%String* %44, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  %47 = call %String* @__quantum__rt__range_to_string(%Range { i64 0, i64 1, i64 3 })
  %i = call %String* @__quantum__rt__string_concatenate(%String* %46, %String* %47)
  call void @__quantum__rt__string_update_reference_count(%String* %46, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %47, i32 -1)
  %48 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @16, i32 0, i32 0))
  %49 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @17, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 1)
  %50 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %51 = sub i64 %50, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %condContinue__2
  %52 = phi %String* [ %49, %condContinue__2 ], [ %62, %exiting__1 ]
  %53 = phi i64 [ 0, %condContinue__2 ], [ %63, %exiting__1 ]
  %54 = icmp sle i64 %53, %51
  br i1 %54, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %55 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %53)
  %56 = bitcast i8* %55 to i64*
  %57 = load i64, i64* %56, align 4
  %58 = icmp ne %String* %52, %49
  br i1 %58, label %condTrue__5, label %condContinue__5

condTrue__5:                                      ; preds = %body__1
  %59 = call %String* @__quantum__rt__string_concatenate(%String* %52, %String* %48)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  br label %condContinue__5

condContinue__5:                                  ; preds = %condTrue__5, %body__1
  %60 = phi %String* [ %59, %condTrue__5 ], [ %52, %body__1 ]
  %61 = call %String* @__quantum__rt__int_to_string(i64 %57)
  %62 = call %String* @__quantum__rt__string_concatenate(%String* %60, %String* %61)
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__5
  %63 = add i64 %53, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %64 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @18, i32 0, i32 0))
  %data = call %String* @__quantum__rt__string_concatenate(%String* %52, %String* %64)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %64, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %48, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 -1)
  %65 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @2, i32 0, i32 0))
  %66 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %67 = call %String* @__quantum__rt__string_concatenate(%String* %66, %String* %x)
  %68 = call %String* @__quantum__rt__string_concatenate(%String* %67, %String* %66)
  call void @__quantum__rt__string_update_reference_count(%String* %67, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %66, i32 -1)
  %res = call %String* @__quantum__rt__string_concatenate(%String* %65, %String* %68)
  call void @__quantum__rt__string_update_reference_count(%String* %65, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %68, i32 -1)
  %69 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @19, i32 0, i32 0))
  %defaultArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %70 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 0)
  %71 = bitcast i8* %70 to %String**
  store %String* %69, %String** %71, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %69, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 1)
  %72 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @20, i32 0, i32 0))
  %strArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %73 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 0)
  %74 = bitcast i8* %73 to %String**
  store %String* %72, %String** %74, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %72, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 1)
  %75 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @19, i32 0, i32 0))
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
  call void @__quantum__rt__string_update_reference_count(%String* %69, i32 -1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %76 = phi i64 [ 0, %exit__1 ], [ %81, %exiting__2 ]
  %77 = icmp sle i64 %76, 0
  br i1 %77, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %78 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %76)
  %79 = bitcast i8* %78 to %String**
  %80 = load %String*, %String** %79, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %80, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %81 = add i64 %76, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %72, i32 -1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %82 = phi i64 [ 0, %exit__2 ], [ %87, %exiting__3 ]
  %83 = icmp sle i64 %82, 0
  br i1 %83, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %84 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %82)
  %85 = bitcast i8* %84 to %String**
  %86 = load %String*, %String** %85, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %87 = add i64 %82, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %strArr, i32 -1)
  ret %String* %75
}
