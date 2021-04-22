define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate3__body(%Array* %y, %String* %b) {
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
  store %String* %b, %String** %10, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i32 1)
  store %Array* %8, %Array** %x, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i32 -1)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %8, i1 false)
  %13 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0))
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 1)
  %15 = bitcast i8* %14 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %13, i32 1)
  %16 = load %String*, %String** %15, align 8
  store %String* %13, %String** %15, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i32 1)
  store %Array* %12, %Array** %x, align 8
  %17 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  %18 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %19 = bitcast %Tuple* %18 to { i64, i64 }*
  %20 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %19, i32 0, i32 0
  %21 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %19, i32 0, i32 1
  store i64 0, i64* %20, align 4
  store i64 0, i64* %21, align 4
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %22 = phi i64 [ 0, %exit__1 ], [ %26, %exiting__2 ]
  %23 = icmp sle i64 %22, 9
  br i1 %23, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %22)
  %25 = bitcast i8* %24 to { i64, i64 }**
  store { i64, i64 }* %19, { i64, i64 }** %25, align 8
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %26 = add i64 %22, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %arr = alloca %Array*, align 8
  store %Array* %17, %Array** %arr, align 8
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %27 = phi i64 [ 0, %exit__2 ], [ %33, %exiting__3 ]
  %28 = icmp sle i64 %27, 9
  br i1 %28, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %29 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %27)
  %30 = bitcast i8* %29 to { i64, i64 }**
  %31 = load { i64, i64 }*, { i64, i64 }** %30, align 8
  %32 = bitcast { i64, i64 }* %31 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %32, i32 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %33 = add i64 %27, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %17, i32 1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %34 = phi i64 [ 0, %exit__3 ], [ %40, %exiting__4 ]
  %35 = icmp sle i64 %34, 9
  br i1 %35, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %36 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %34)
  %37 = bitcast i8* %36 to { i64, i64 }**
  %38 = load { i64, i64 }*, { i64, i64 }** %37, align 8
  %39 = bitcast { i64, i64 }* %38 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %39, i32 1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %40 = add i64 %34, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i32 1)
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %i = phi i64 [ 0, %exit__4 ], [ %53, %exiting__5 ]
  %41 = icmp sle i64 %i, 9
  br i1 %41, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %42 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %42, i32 -1)
  %43 = call %Array* @__quantum__rt__array_copy(%Array* %42, i1 false)
  %44 = add i64 %i, 1
  %45 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %46 = bitcast %Tuple* %45 to { i64, i64 }*
  %47 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %46, i32 0, i32 0
  %48 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %46, i32 0, i32 1
  store i64 %i, i64* %47, align 4
  store i64 %44, i64* %48, align 4
  %49 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %43, i64 %i)
  %50 = bitcast i8* %49 to { i64, i64 }**
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %45, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %45, i32 1)
  %51 = load { i64, i64 }*, { i64, i64 }** %50, align 8
  %52 = bitcast { i64, i64 }* %51 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %52, i32 -1)
  store { i64, i64 }* %46, { i64, i64 }** %50, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %43, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %43, i32 1)
  store %Array* %43, %Array** %arr, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %42, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %45, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %52, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %43, i32 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %53 = add i64 %i, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  %54 = load %Array*, %Array** %arr, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %12, i32 -1)
  %55 = call i64 @__quantum__rt__array_get_size_1d(%Array* %54)
  %56 = sub i64 %55, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %57 = phi i64 [ 0, %exit__5 ], [ %63, %exiting__6 ]
  %58 = icmp sle i64 %57, %56
  br i1 %58, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %59 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %54, i64 %57)
  %60 = bitcast i8* %59 to { i64, i64 }**
  %61 = load { i64, i64 }*, { i64, i64 }** %60, align 8
  %62 = bitcast { i64, i64 }* %61 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %62, i32 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %63 = add i64 %57, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_alias_count(%Array* %54, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %11, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i32 -1)
  %64 = sub i64 %55, 1
  br label %header__7

header__7:                                        ; preds = %exiting__7, %exit__6
  %65 = phi i64 [ 0, %exit__6 ], [ %71, %exiting__7 ]
  %66 = icmp sle i64 %65, %64
  br i1 %66, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %54, i64 %65)
  %68 = bitcast i8* %67 to { i64, i64 }**
  %69 = load { i64, i64 }*, { i64, i64 }** %68, align 8
  %70 = bitcast { i64, i64 }* %69 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %70, i32 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %71 = add i64 %65, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_reference_count(%Array* %54, i32 -1)
  ret %Array* %12
}
