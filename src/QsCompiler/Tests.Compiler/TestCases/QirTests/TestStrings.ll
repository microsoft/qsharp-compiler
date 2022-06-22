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
  %16 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @6, i32 0, i32 0))
  %17 = call %String* @__quantum__rt__string_concatenate(%String* %15, %String* %16)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  %18 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @7, i32 0, i32 0))
  %19 = call %String* @__quantum__rt__string_concatenate(%String* %17, %String* %18)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 -1)
  %20 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @8, i32 0, i32 0))
  %21 = call %String* @__quantum__rt__string_concatenate(%String* %19, %String* %20)
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %20, i32 -1)
  %22 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @9, i32 0, i32 0))
  %23 = call %String* @__quantum__rt__string_concatenate(%String* %21, %String* %22)
  call void @__quantum__rt__string_update_reference_count(%String* %21, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %22, i32 -1)
  %24 = call %String* @__quantum__rt__result_to_string(%Result* %9)
  %25 = call %String* @__quantum__rt__string_concatenate(%String* %23, %String* %24)
  call void @__quantum__rt__string_update_reference_count(%String* %23, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %24, i32 -1)
  %26 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @10, i32 0, i32 0))
  %27 = call %String* @__quantum__rt__string_concatenate(%String* %25, %String* %26)
  call void @__quantum__rt__string_update_reference_count(%String* %25, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %26, i32 -1)
  %28 = call %String* @__quantum__rt__bigint_to_string(%BigInt* %10)
  %29 = call %String* @__quantum__rt__string_concatenate(%String* %27, %String* %28)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %28, i32 -1)
  %30 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @11, i32 0, i32 0))
  %31 = call %String* @__quantum__rt__string_concatenate(%String* %29, %String* %30)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i32 -1)
  %32 = call %String* @__quantum__rt__range_to_string(%Range { i64 0, i64 1, i64 3 })
  %i = call %String* @__quantum__rt__string_concatenate(%String* %31, %String* %32)
  call void @__quantum__rt__string_update_reference_count(%String* %31, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %32, i32 -1)
  %33 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @12, i32 0, i32 0))
  %34 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @13, i32 0, i32 0))
  call void @__quantum__rt__string_update_reference_count(%String* %34, i32 1)
  %35 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %36 = sub i64 %35, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %37 = phi %String* [ %34, %entry ], [ %47, %exiting__1 ]
  %38 = phi i64 [ 0, %entry ], [ %48, %exiting__1 ]
  %39 = icmp sle i64 %38, %36
  br i1 %39, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %40 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arr, i64 %38)
  %41 = bitcast i8* %40 to i64*
  %42 = load i64, i64* %41, align 4
  %43 = icmp ne %String* %37, %34
  br i1 %43, label %condTrue__1, label %condContinue__1

condTrue__1:                                      ; preds = %body__1
  %44 = call %String* @__quantum__rt__string_concatenate(%String* %37, %String* %33)
  call void @__quantum__rt__string_update_reference_count(%String* %37, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condTrue__1, %body__1
  %45 = phi %String* [ %44, %condTrue__1 ], [ %37, %body__1 ]
  %46 = call %String* @__quantum__rt__int_to_string(i64 %42)
  %47 = call %String* @__quantum__rt__string_concatenate(%String* %45, %String* %46)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %46, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %condContinue__1
  %48 = add i64 %38, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %49 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @14, i32 0, i32 0))
  %data = call %String* @__quantum__rt__string_concatenate(%String* %37, %String* %49)
  call void @__quantum__rt__string_update_reference_count(%String* %37, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %33, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %34, i32 -1)
  %50 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @2, i32 0, i32 0))
  %51 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %52 = call %String* @__quantum__rt__string_concatenate(%String* %51, %String* %x)
  %53 = call %String* @__quantum__rt__string_concatenate(%String* %52, %String* %51)
  call void @__quantum__rt__string_update_reference_count(%String* %52, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %51, i32 -1)
  %res = call %String* @__quantum__rt__string_concatenate(%String* %50, %String* %53)
  call void @__quantum__rt__string_update_reference_count(%String* %50, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %53, i32 -1)
  %54 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @15, i32 0, i32 0))
  %defaultArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %55 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 0)
  %56 = bitcast i8* %55 to %String**
  store %String* %54, %String** %56, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %54, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %defaultArr, i32 1)
  %57 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @16, i32 0, i32 0))
  %strArr = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %58 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 0)
  %59 = bitcast i8* %58 to %String**
  store %String* %57, %String** %59, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %57, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %strArr, i32 1)
  %60 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @15, i32 0, i32 0))
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
  call void @__quantum__rt__string_update_reference_count(%String* %54, i32 -1)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %61 = phi i64 [ 0, %exit__1 ], [ %66, %exiting__2 ]
  %62 = icmp sle i64 %61, 0
  br i1 %62, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %63 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %defaultArr, i64 %61)
  %64 = bitcast i8* %63 to %String**
  %65 = load %String*, %String** %64, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %65, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %66 = add i64 %61, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %defaultArr, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %57, i32 -1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %67 = phi i64 [ 0, %exit__2 ], [ %72, %exiting__3 ]
  %68 = icmp sle i64 %67, 0
  br i1 %68, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %69 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %strArr, i64 %67)
  %70 = bitcast i8* %69 to %String**
  %71 = load %String*, %String** %70, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %71, i32 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %72 = add i64 %67, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %strArr, i32 -1)
  ret %String* %60
}
