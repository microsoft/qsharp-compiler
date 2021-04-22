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
  %49 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i32 0, i32 0))
  %50 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i32 0, i32 0))
  %51 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @3, i32 0, i32 0))
  %52 = call i64 @__quantum__rt__array_get_size_1d(%Array* %44)
  %53 = sub i64 %52, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %54 = phi i64 [ 0, %exit__1 ], [ %62, %exiting__2 ]
  %55 = icmp sle i64 %54, %53
  br i1 %55, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %56 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %44, i64 %54)
  %57 = bitcast i8* %56 to i2*
  %58 = load i2, i2* %57, align 1
  %59 = call %String* @__quantum__rt__pauli_to_string(i2 %58)
  %60 = call %String* @__quantum__rt__string_concatenate(%String* %49, %String* %59)
  call void @__quantum__rt__string_update_reference_count(%String* %49, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %59, i32 -1)
  %61 = call %String* @__quantum__rt__string_concatenate(%String* %60, %String* %50)
  call void @__quantum__rt__string_update_reference_count(%String* %60, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %62 = add i64 %54, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %63 = call %String* @__quantum__rt__string_concatenate(%String* %61, %String* %51)
  call void @__quantum__rt__string_update_reference_count(%String* %61, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %51, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %50, i32 -1)
  %64 = call %String* @__quantum__rt__string_concatenate(%String* %38, %String* %63)
  call void @__quantum__rt__string_update_reference_count(%String* %38, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %63, i32 -1)
  %65 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @4, i32 0, i32 0))
  %66 = call %String* @__quantum__rt__string_concatenate(%String* %64, %String* %65)
  call void @__quantum__rt__string_update_reference_count(%String* %64, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 1)
  %67 = call %String* @__quantum__rt__string_concatenate(%String* %66, %String* %45)
  call void @__quantum__rt__string_update_reference_count(%String* %66, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  %68 = call %String* @__quantum__rt__string_concatenate(%String* %67, %String* %65)
  call void @__quantum__rt__string_update_reference_count(%String* %67, i32 -1)
  %69 = call %String* @__quantum__rt__result_to_string(%Result* %46)
  %70 = call %String* @__quantum__rt__string_concatenate(%String* %68, %String* %69)
  call void @__quantum__rt__string_update_reference_count(%String* %68, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %69, i32 -1)
  %71 = call %String* @__quantum__rt__string_concatenate(%String* %70, %String* %65)
  call void @__quantum__rt__string_update_reference_count(%String* %70, i32 -1)
  %72 = call %String* @__quantum__rt__range_to_string(%Range %47)
  %73 = call %String* @__quantum__rt__string_concatenate(%String* %71, %String* %72)
  call void @__quantum__rt__string_update_reference_count(%String* %71, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %72, i32 -1)
  %74 = call %String* @__quantum__rt__string_concatenate(%String* %73, %String* %65)
  call void @__quantum__rt__string_update_reference_count(%String* %73, i32 -1)
  %75 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @5, i32 0, i32 0))
  %76 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %48, i32 0, i32 0
  %77 = getelementptr inbounds { i64, i1 }, { i64, i1 }* %48, i32 0, i32 1
  %78 = load i64, i64* %76, align 4
  %79 = load i1, i1* %77, align 1
  %80 = call %String* @__quantum__rt__int_to_string(i64 %78)
  %81 = call %String* @__quantum__rt__string_concatenate(%String* %75, %String* %80)
  call void @__quantum__rt__string_update_reference_count(%String* %75, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %80, i32 -1)
  %82 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @6, i32 0, i32 0))
  %83 = call %String* @__quantum__rt__string_concatenate(%String* %81, %String* %82)
  call void @__quantum__rt__string_update_reference_count(%String* %81, i32 -1)
  %84 = call %String* @__quantum__rt__bool_to_string(i1 %79)
  %85 = call %String* @__quantum__rt__string_concatenate(%String* %83, %String* %84)
  call void @__quantum__rt__string_update_reference_count(%String* %83, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %84, i32 -1)
  %86 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @7, i32 0, i32 0))
  %87 = call %String* @__quantum__rt__string_concatenate(%String* %85, %String* %86)
  call void @__quantum__rt__string_update_reference_count(%String* %85, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %86, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %82, i32 -1)
  %88 = call %String* @__quantum__rt__string_concatenate(%String* %74, %String* %87)
  call void @__quantum__rt__string_update_reference_count(%String* %74, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %87, i32 -1)
  %89 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @8, i32 0, i32 0))
  %90 = call %String* @__quantum__rt__string_concatenate(%String* %88, %String* %89)
  call void @__quantum__rt__string_update_reference_count(%String* %88, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %89, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %65, i32 -1)
  call void @__quantum__rt__message(%String* %90)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %33, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %44, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %45, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %46, i32 -1)
  %91 = bitcast { i64, i1 }* %48 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %91, i32 -1)
  %92 = bitcast { %Array*, %String*, %Result*, %Range, { i64, i1 }* }* %37 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %92, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %90, i32 -1)
  ret void
}
