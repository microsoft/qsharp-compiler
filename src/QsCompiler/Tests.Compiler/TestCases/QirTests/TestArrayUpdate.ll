define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 1)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 -1)
  %0 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %2 = bitcast i8* %1 to %String**
  %3 = load %String*, %String** %2
  store %String* %b, %String** %2
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i64 1)
  store %Array* %0, %Array** %x
  call void @__quantum__rt__array_update_alias_count(%Array* %0, i64 -1)
  %4 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %5 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 1)
  %7 = bitcast i8* %6 to %String**
  %8 = load %String*, %String** %7
  store %String* %5, %String** %7
  call void @__quantum__rt__array_update_alias_count(%Array* %4, i64 1)
  store %Array* %4, %Array** %x
  %9 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %10 = phi i64 [ 0, %entry ], [ %18, %exiting__1 ]
  %11 = icmp sle i64 %10, 9
  br i1 %11, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %12 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %13 = bitcast %Tuple* %12 to { i64, i64 }*
  %14 = getelementptr { i64, i64 }, { i64, i64 }* %13, i64 0, i32 0
  %15 = getelementptr { i64, i64 }, { i64, i64 }* %13, i64 0, i32 1
  store i64 0, i64* %14
  store i64 0, i64* %15
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %10)
  %17 = bitcast i8* %16 to { i64, i64 }**
  store { i64, i64 }* %13, { i64, i64 }** %17
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %18 = add i64 %10, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %arr = alloca %Array*
  store %Array* %9, %Array** %arr
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %19 = phi i64 [ 0, %exit__1 ], [ %25, %exiting__2 ]
  %20 = icmp sle i64 %19, 9
  br i1 %20, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %21 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %19)
  %22 = bitcast i8* %21 to { i64, i64 }**
  %23 = load { i64, i64 }*, { i64, i64 }** %22
  %24 = bitcast { i64, i64 }* %23 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i64 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %25 = add i64 %19, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_alias_count(%Array* %9, i64 1)
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %i = phi i64 [ 0, %exit__2 ], [ %38, %exiting__3 ]
  %26 = icmp sle i64 %i, 9
  br i1 %26, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %27 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_alias_count(%Array* %27, i64 -1)
  %28 = call %Array* @__quantum__rt__array_copy(%Array* %27, i1 false)
  %29 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %30 = bitcast %Tuple* %29 to { i64, i64 }*
  %31 = getelementptr { i64, i64 }, { i64, i64 }* %30, i64 0, i32 0
  %32 = getelementptr { i64, i64 }, { i64, i64 }* %30, i64 0, i32 1
  %33 = add i64 %i, 1
  store i64 %i, i64* %31
  store i64 %33, i64* %32
  %34 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %28, i64 %i)
  %35 = bitcast i8* %34 to { i64, i64 }**
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %29, i64 1)
  %36 = load { i64, i64 }*, { i64, i64 }** %35
  %37 = bitcast { i64, i64 }* %36 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %37, i64 -1)
  store { i64, i64 }* %30, { i64, i64 }** %35
  call void @__quantum__rt__array_update_alias_count(%Array* %28, i64 1)
  store %Array* %28, %Array** %arr
  call void @__quantum__rt__array_update_reference_count(%Array* %27, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %37, i64 -1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %38 = add i64 %i, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  %39 = load %Array*, %Array** %arr
  %40 = call i64 @__quantum__rt__array_get_size_1d(%Array* %y)
  %41 = sub i64 %40, 1
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %42 = phi i64 [ 0, %exit__3 ], [ %47, %exiting__4 ]
  %43 = icmp sle i64 %42, %41
  br i1 %43, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %44 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %y, i64 %42)
  %45 = bitcast i8* %44 to %String**
  %46 = load %String*, %String** %45
  call void @__quantum__rt__string_update_reference_count(%String* %46, i64 1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %47 = add i64 %42, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %b, i64 1)
  %48 = call i64 @__quantum__rt__array_get_size_1d(%Array* %0)
  %49 = sub i64 %48, 1
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %50 = phi i64 [ 0, %exit__4 ], [ %55, %exiting__5 ]
  %51 = icmp sle i64 %50, %49
  br i1 %51, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %52 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %50)
  %53 = bitcast i8* %52 to %String**
  %54 = load %String*, %String** %53
  call void @__quantum__rt__string_update_reference_count(%String* %54, i64 1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %55 = add i64 %50, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %4, i64 -1)
  %56 = call i64 @__quantum__rt__array_get_size_1d(%Array* %39)
  %57 = sub i64 %56, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %58 = phi i64 [ 0, %exit__5 ], [ %64, %exiting__6 ]
  %59 = icmp sle i64 %58, %57
  br i1 %59, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %60 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %39, i64 %58)
  %61 = bitcast i8* %60 to { i64, i64 }**
  %62 = load { i64, i64 }*, { i64, i64 }** %61
  %63 = bitcast { i64, i64 }* %62 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %63, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %64 = add i64 %58, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_alias_count(%Array* %39, i64 -1)
  %65 = call i64 @__quantum__rt__array_get_size_1d(%Array* %4)
  %66 = sub i64 %65, 1
  br label %header__7

header__7:                                        ; preds = %exiting__7, %exit__6
  %67 = phi i64 [ 0, %exit__6 ], [ %72, %exiting__7 ]
  %68 = icmp sle i64 %67, %66
  br i1 %68, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %69 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 %67)
  %70 = bitcast i8* %69 to %String**
  %71 = load %String*, %String** %70
  call void @__quantum__rt__string_update_reference_count(%String* %71, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %72 = add i64 %67, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i64 -1)
  %73 = sub i64 %56, 1
  br label %header__8

header__8:                                        ; preds = %exiting__8, %exit__7
  %74 = phi i64 [ 0, %exit__7 ], [ %80, %exiting__8 ]
  %75 = icmp sle i64 %74, %73
  br i1 %75, label %body__8, label %exit__8

body__8:                                          ; preds = %header__8
  %76 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %39, i64 %74)
  %77 = bitcast i8* %76 to { i64, i64 }**
  %78 = load { i64, i64 }*, { i64, i64 }** %77
  %79 = bitcast { i64, i64 }* %78 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %79, i64 -1)
  br label %exiting__8

exiting__8:                                       ; preds = %body__8
  %80 = add i64 %74, 1
  br label %header__8

exit__8:                                          ; preds = %header__8
  call void @__quantum__rt__array_update_reference_count(%Array* %39, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %3, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  ret %Array* %4
}
