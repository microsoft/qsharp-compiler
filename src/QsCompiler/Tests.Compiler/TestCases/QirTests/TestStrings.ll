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
  %9 = call %Result* @__quantum__rt__result_get_one()
  %10 = call %BigInt* @__quantum__rt__bigint_create_i64(i64 1)
  %11 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([17 x i8], [17 x i8]* @4, i32 0, i32 0))
  %12 = call %String* @__quantum__rt__double_to_string(double 1.200000e+00)
  %13 = call %String* @__quantum__rt__string_concatenate(%String* %11, %String* %12)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %12, i32 -1)
  %14 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @5, i32 0, i32 0))
  %15 = call %String* @__quantum__rt__string_concatenate(%String* %13, %String* %14)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %14, i32 -1)
  br i1 true, label %condTrue__1, label %condFalse__1

condTrue__1:                                      ; preds = %entry
  %16 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @6, i32 0, i32 0))
  br label %condContinue__1

condFalse__1:                                     ; preds = %entry
  %17 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @7, i32 0, i32 0))
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %condTrue__1
  %18 = phi %String* [ %16, %condTrue__1 ], [ %17, %condFalse__1 ]
  %19 = call %String* @__quantum__rt__string_concatenate(%String* %15, %String* %18)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 -1)
  %20 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @8, i32 0, i32 0))
  %21 = call %String* @__quantum__rt__string_concatenate(%String* %19, %String* %20)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 -1)
  br i1 true, label %condTrue__2, label %condFalse__2

condTrue__2:                                      ; preds = %condContinue__1
  %22 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @9, i32 0, i32 0))
  br label %condContinue__2

condFalse__2:                                     ; preds = %condContinue__1
  br i1 false, label %condTrue__3, label %condFalse__3

condTrue__3:                                      ; preds = %condFalse__2
  %23 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @10, i32 0, i32 0))
  br label %condContinue__3

condFalse__3:                                     ; preds = %condFalse__2
  br i1 false, label %condTrue__4, label %condFalse__4

condTrue__4:                                      ; preds = %condFalse__3
  %24 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @11, i32 0, i32 0))
  br label %condContinue__4

condFalse__4:                                     ; preds = %condFalse__3
  %25 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @12, i32 0, i32 0))
  br label %condContinue__4

condContinue__4:                                  ; preds = %condFalse__4, %condTrue__4
  %26 = phi %String* [ %24, %condTrue__4 ], [ %25, %condFalse__4 ]
  br label %condContinue__3

condContinue__3:                                  ; preds = %condContinue__4, %condTrue__3
  %27 = phi %String* [ %23, %condTrue__3 ], [ %26, %condContinue__4 ]
  br label %condContinue__2

condContinue__2:                                  ; preds = %condContinue__3, %condTrue__2
  %28 = phi %String* [ %22, %condTrue__2 ], [ %27, %condContinue__3 ]
  %29 = call %String* @__quantum__rt__string_concatenate(%String* %21, %String* %28)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %28, i32 -1)
  %30 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @13, i32 0, i32 0))
  %31 = call %String* @__quantum__rt__string_concatenate(%String* %29, %String* %30)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i32 -1)
  %32 = call %String* @__quantum__rt__result_to_string(%Result* %9)
  %33 = call %String* @__quantum__rt__string_concatenate(%String* %31, %String* %32)
  call void @__quantum__rt__string_update_reference_count(%String* %31, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %32, i32 -1)
  %34 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @14, i32 0, i32 0))
  %35 = call %String* @__quantum__rt__string_concatenate(%String* %33, %String* %34)
  call void @__quantum__rt__string_update_reference_count(%String* %33, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %34, i32 -1)
  %36 = call %String* @__quantum__rt__bigint_to_string(%BigInt* %10)
  %37 = call %String* @__quantum__rt__string_concatenate(%String* %35, %String* %36)
  call void @__quantum__rt__string_update_reference_count(%String* %35, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %36, i32 -1)
  %38 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @15, i32 0, i32 0))
  %39 = call %String* @__quantum__rt__string_concatenate(%String* %37, %String* %38)
  call void @__quantum__rt__string_update_reference_count(%String* %37, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %38, i32 -1)
  %40 = call %String* @__quantum__rt__range_to_string(%Range { i64 0, i64 1, i64 3 })
  %i = call %String* @__quantum__rt__string_concatenate(%String* %39, %String* %40)
  call void @__quantum__rt__string_update_reference_count(%String* %39, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %40, i32 -1)
  %41 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @16, i32 0, i32 0))
  %42 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @17, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %42, i32 1)
  %43 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %44 = sub i64 %43, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %condContinue__2
  %45 = phi %String* [ %42, %condContinue__2 ], [ %55, %exiting__1 ]
  %46 = phi i64 [ 0, %condContinue__2 ], [ %56, %exiting__1 ]
  %47 = icmp sle i64 %46, %44
  br i1 %47, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %48 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %46)
  %49 = bitcast i8* %48 to i64*
  %50 = load i64, i64* %49, align 4
  %51 = icmp ne %String* %45, %42
  br i1 %51, label %condTrue__5, label %condContinue__5

condTrue__5:                                      ; preds = %body__1
  %52 = call %String* @__quantum__rt__string_concatenate(%String* %45, %String* %41)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  br label %condContinue__5

condContinue__5:                                  ; preds = %condTrue__5, %body__1
  %53 = phi %String* [ %52, %condTrue__5 ], [ %45, %body__1 ]
  %54 = call %String* @__quantum__rt__int_to_string(i64 %50)
  %55 = call %String* @__quantum__rt__string_concatenate(%String* %53, %String* %54)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %54, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__5
  %56 = add i64 %46, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %57 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @18, i32 0, i32 0))
  %data = call %String* @__quantum__rt__string_concatenate(%String* %45, %String* %57)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %57, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %41, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %42, i32 -1)
  %58 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @2, i32 0, i32 0))
  %59 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %60 = call %String* @__quantum__rt__string_concatenate(%String* %59, %String* %x)
  %61 = call %String* @__quantum__rt__string_concatenate(%String* %60, %String* %59)
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %59, i32 -1)
  %res = call %String* @__quantum__rt__string_concatenate(%String* %58, %String* %61)
  call void @__quantum__rt__string_update_reference_count(%String* %58, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 -1)
  %62 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @19, i32 0, i32 0))
  %defaultArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %63 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 0)
  %64 = bitcast i8* %63 to %String**
  store %String* %62, %String** %64, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %62, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 1)
  %65 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @20, i32 0, i32 0))
  %strArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %66 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 0)
  %67 = bitcast i8* %66 to %String**
  store %String* %65, %String** %67, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %65, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 1)
  %68 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @19, i32 0, i32 0))
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %x, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %y, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %z, i32 -1)
  call void @__quantum__rt__bigint_update_reference_count(%BigInt* %10, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %i, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %data, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %res, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %62, i32 -1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %69 = phi i64 [ 0, %exit__1 ], [ %74, %exiting__2 ]
  %70 = icmp sle i64 %69, 0
  br i1 %70, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %71 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %69)
  %72 = bitcast i8* %71 to %String**
  %73 = load %String*, %String** %72, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %73, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %74 = add i64 %69, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %65, i32 -1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %75 = phi i64 [ 0, %exit__2 ], [ %80, %exiting__3 ]
  %76 = icmp sle i64 %75, 0
  br i1 %76, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %77 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %75)
  %78 = bitcast i8* %77 to %String**
  %79 = load %String*, %String** %78, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %79, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %80 = add i64 %75, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %strArr, i32 -1)
  ret %String* %68
}
