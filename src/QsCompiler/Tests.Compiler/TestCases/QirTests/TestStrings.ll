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
  %27 = call %String* @__quantum__rt__pauli_to_string(i2 %9)
  %28 = call %String* @__quantum__rt__string_concatenate(%String* %26, %String* %27)
  call void @__quantum__rt__string_update_reference_count(%String* %26, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i32 -1)
  %29 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @9, i32 0, i32 0))
  %30 = call %String* @__quantum__rt__string_concatenate(%String* %28, %String* %29)
  call void @__quantum__rt__string_update_reference_count(%String* %28, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i32 -1)
  %31 = call %String* @__quantum__rt__result_to_string(%Result* %10)
  %32 = call %String* @__quantum__rt__string_concatenate(%String* %30, %String* %31)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %31, i32 -1)
  %33 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @10, i32 0, i32 0))
  %34 = call %String* @__quantum__rt__string_concatenate(%String* %32, %String* %33)
  call void @__quantum__rt__string_update_reference_count(%String* %32, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %33, i32 -1)
  %35 = call %String* @__quantum__rt__bigint_to_string(%BigInt* %11)
  %36 = call %String* @__quantum__rt__string_concatenate(%String* %34, %String* %35)
  call void @__quantum__rt__string_update_reference_count(%String* %34, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %35, i32 -1)
  %37 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @11, i32 0, i32 0))
  %38 = call %String* @__quantum__rt__string_concatenate(%String* %36, %String* %37)
  call void @__quantum__rt__string_update_reference_count(%String* %36, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %37, i32 -1)
  %39 = call %String* @__quantum__rt__range_to_string(%Range %15)
  %i = call %String* @__quantum__rt__string_concatenate(%String* %38, %String* %39)
  call void @__quantum__rt__string_update_reference_count(%String* %38, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %39, i32 -1)
  %40 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @12, i32 0, i32 0))
  %41 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @13, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %41, i32 1)
  %42 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %43 = sub i64 %42, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %condContinue__1
  %44 = phi %String* [ %41, %condContinue__1 ], [ %54, %exiting__1 ]
  %45 = phi i64 [ 0, %condContinue__1 ], [ %55, %exiting__1 ]
  %46 = icmp sle i64 %45, %43
  br i1 %46, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %47 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %45)
  %48 = bitcast i8* %47 to i64*
  %49 = load i64, i64* %48, align 4
  %50 = icmp ne %String* %44, %41
  br i1 %50, label %condTrue__2, label %condContinue__2

condTrue__2:                                      ; preds = %body__1
  %51 = call %String* @__quantum__rt__string_concatenate(%String* %44, %String* %40)
  call void @__quantum__rt__string_update_reference_count(%String* %44, i32 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condTrue__2, %body__1
  %52 = phi %String* [ %51, %condTrue__2 ], [ %44, %body__1 ]
  %53 = call %String* @__quantum__rt__int_to_string(i64 %49)
  %54 = call %String* @__quantum__rt__string_concatenate(%String* %52, %String* %53)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__2
  %55 = add i64 %45, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %56 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @14, i32 0, i32 0))
  %data = call %String* @__quantum__rt__string_concatenate(%String* %44, %String* %56)
  call void @__quantum__rt__string_update_reference_count(%String* %44, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %56, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %40, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %41, i32 -1)
  %57 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @15, i32 0, i32 0))
  %58 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @16, i32 0, i32 0))
  %59 = call %String* @__quantum__rt__string_concatenate(%String* %58, %String* %x)
  %60 = call %String* @__quantum__rt__string_concatenate(%String* %59, %String* %58)
  call void @__quantum__rt__string_update_reference_count(%String* %59, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %58, i32 -1)
  %res = call %String* @__quantum__rt__string_concatenate(%String* %57, %String* %60)
  call void @__quantum__rt__string_update_reference_count(%String* %57, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 -1)
  %61 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @17, i32 0, i32 0))
  %defaultArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %62 = phi i64 [ 0, %exit__1 ], [ %66, %exiting__2 ]
  %63 = icmp sle i64 %62, 0
  br i1 %63, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %64 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %62)
  %65 = bitcast i8* %64 to %String**
  store %String* %61, %String** %65, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %66 = add i64 %62, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 1)
  %67 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @18, i32 0, i32 0))
  %strArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %68 = phi i64 [ 0, %exit__2 ], [ %72, %exiting__3 ]
  %69 = icmp sle i64 %68, 0
  br i1 %69, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %70 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %68)
  %71 = bitcast i8* %70 to %String**
  store %String* %67, %String** %71, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %67, i32 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %72 = add i64 %68, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 1)
  %73 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @19, i32 0, i32 0))
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
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 -1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %74 = phi i64 [ 0, %exit__3 ], [ %79, %exiting__4 ]
  %75 = icmp sle i64 %74, 0
  br i1 %75, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %76 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %74)
  %77 = bitcast i8* %76 to %String**
  %78 = load %String*, %String** %77, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %78, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %79 = add i64 %74, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %67, i32 -1)
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %80 = phi i64 [ 0, %exit__4 ], [ %85, %exiting__5 ]
  %81 = icmp sle i64 %80, 0
  br i1 %81, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %82 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %80)
  %83 = bitcast i8* %82 to %String**
  %84 = load %String*, %String** %83, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %85 = add i64 %80, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_reference_count(%Array* %strArr, i32 -1)
  ret %String* %73
}
