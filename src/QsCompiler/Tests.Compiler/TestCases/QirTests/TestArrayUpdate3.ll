define internal %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate3__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i32 1)
  %x = alloca %Array*, align 8
  store %Array* %y, %Array** %x, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i32 1)
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %y)
  %1 = sub i64 %0, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %7, %exiting__1 ]
  %3 = icmp sle i64 %2, %1
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %y, i64 %2)
  %5 = bitcast i8* %4 to %String**
  %6 = load %String*, %String** %5, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %6, i32 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %7 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i32 -1)
  %8 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %8, i64 0)
  %10 = bitcast i8* %9 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %b, i32 1)
  %11 = load %String*, %String** %10, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  store %String* %b, %String** %10, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i32 1)
  store %Array* %8, %Array** %x, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i32 -1)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %8, i1 false)
  %13 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0))
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 1)
  %15 = bitcast i8* %14 to %String**
  %16 = load %String*, %String** %15, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  store %String* %13, %String** %15, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i32 1)
  store %Array* %12, %Array** %x, align 8
  %17 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i64 }* getelementptr ({ i64, i64 }, { i64, i64 }* null, i32 1) to i64))
  %18 = bitcast %Tuple* %17 to { i64, i64 }*
  %19 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %18, i32 0, i32 0
  %20 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %18, i32 0, i32 1
  store i64 0, i64* %19, align 4
  store i64 0, i64* %20, align 4
  %21 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %22 = phi i64 [ 0, %exit__1 ], [ %26, %exiting__2 ]
  %23 = icmp sle i64 %22, 9
  br i1 %23, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %21, i64 %22)
  %25 = bitcast i8* %24 to { i64, i64 }**
  store { i64, i64 }* %18, { i64, i64 }** %25, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i32 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %26 = add i64 %22, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %arr = alloca %Array*, align 8
  store %Array* %21, %Array** %arr, align 8
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %27 = phi i64 [ 0, %exit__2 ], [ %33, %exiting__3 ]
  %28 = icmp sle i64 %27, 9
  br i1 %28, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %29 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %21, i64 %27)
  %30 = bitcast i8* %29 to { i64, i64 }**
  %31 = load { i64, i64 }*, { i64, i64 }** %30, align 8
  %32 = bitcast { i64, i64 }* %31 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %32, i32 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %33 = add i64 %27, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %21, i32 1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %i = phi i64 [ 0, %exit__3 ], [ %46, %exiting__4 ]
  %34 = icmp sle i64 %i, 9
  br i1 %34, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %35 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %35, i32 -1)
  %36 = call %Array* @__quantum__rt__array_copy(%Array* %35, i1 false)
  %37 = add i64 %i, 1
  %38 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i64 }* getelementptr ({ i64, i64 }, { i64, i64 }* null, i32 1) to i64))
  %39 = bitcast %Tuple* %38 to { i64, i64 }*
  %40 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %39, i32 0, i32 0
  %41 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %39, i32 0, i32 1
  store i64 %i, i64* %40, align 4
  store i64 %37, i64* %41, align 4
  %42 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %36, i64 %i)
  %43 = bitcast i8* %42 to { i64, i64 }**
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %38, i32 1)
  %44 = load { i64, i64 }*, { i64, i64 }** %43, align 8
  %45 = bitcast { i64, i64 }* %44 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %45, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %45, i32 -1)
  store { i64, i64 }* %39, { i64, i64 }** %43, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %36, i32 1)
  store %Array* %36, %Array** %arr, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i32 -1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %46 = add i64 %i, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  %47 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i32 -1)
  %48 = call i64 @__quantum__rt__array_get_size_1d(%Array* %47)
  %49 = sub i64 %48, 1
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %50 = phi i64 [ 0, %exit__4 ], [ %56, %exiting__5 ]
  %51 = icmp sle i64 %50, %49
  br i1 %51, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %52 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %47, i64 %50)
  %53 = bitcast i8* %52 to { i64, i64 }**
  %54 = load { i64, i64 }*, { i64, i64 }** %53, align 8
  %55 = bitcast { i64, i64 }* %54 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %55, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %56 = add i64 %50, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_alias_count(%Array* %47, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i32 -1)
  %57 = sub i64 %48, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %58 = phi i64 [ 0, %exit__5 ], [ %64, %exiting__6 ]
  %59 = icmp sle i64 %58, %57
  br i1 %59, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %60 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %47, i64 %58)
  %61 = bitcast i8* %60 to { i64, i64 }**
  %62 = load { i64, i64 }*, { i64, i64 }** %61, align 8
  %63 = bitcast { i64, i64 }* %62 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %63, i32 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %64 = add i64 %58, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %47, i32 -1)
  ret %Array* %12
}
