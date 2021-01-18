define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 1)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  %0 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %1 = call i64 @__quantum__rt__array_get_size_1d(%Array* %0)
  %2 = sub i64 %1, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %3 = phi i64 [ 0, %entry ], [ %8, %exiting__1 ]
  %4 = icmp sle i64 %3, %2
  br i1 %4, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %3)
  %6 = bitcast i8* %5 to %String**
  %7 = load %String*, %String** %6
  call void @__quantum__rt__string_update_reference_count(%String* %7, i64 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %8 = add i64 %3, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__string_update_reference_count(%String* %b, i64 1)
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %10 = bitcast i8* %9 to %String**
  %11 = load %String*, %String** %10
  call void @__quantum__rt__string_update_reference_count(%String* %11, i64 -1)
  store %String* %b, %String** %10
  store %Array* %0, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 -1)
  %12 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %13 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %14 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %12, i64 1)
  %15 = bitcast i8* %14 to %String**
  %16 = load %String*, %String** %15
  call void @__quantum__rt__string_update_reference_count(%String* %16, i64 -1)
  store %String* %13, %String** %15
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  store %Array* %12, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %12, i64 -1)
  %17 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %18 = phi i64 [ 0, %exit__1 ], [ %26, %exiting__2 ]
  %19 = icmp sle i64 %18, 9
  br i1 %19, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %21 = bitcast %Tuple* %20 to { i64, i64 }*
  %22 = getelementptr { i64, i64 }, { i64, i64 }* %21, i64 0, i32 0
  %23 = getelementptr { i64, i64 }, { i64, i64 }* %21, i64 0, i32 1
  store i64 0, i64* %22
  store i64 0, i64* %23
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %18)
  %25 = bitcast i8* %24 to { i64, i64 }**
  store { i64, i64 }* %21, { i64, i64 }** %25
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %26 = add i64 %18, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %arr = alloca %Array*
  store %Array* %17, %Array** %arr
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %27 = phi i64 [ 0, %exit__2 ], [ %33, %exiting__3 ]
  %28 = icmp sle i64 %27, 9
  br i1 %28, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %29 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %27)
  %30 = bitcast i8* %29 to { i64, i64 }**
  %31 = load { i64, i64 }*, { i64, i64 }** %30
  %32 = bitcast { i64, i64 }* %31 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %32, i64 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %33 = add i64 %27, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_access_count(%Array* %17, i64 1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %i = phi i64 [ 0, %exit__3 ], [ %41, %exiting__4 ]
  %34 = icmp sle i64 %i, 9
  br i1 %34, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %35 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i64 1)
  %36 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_access_count(%Array* %36, i64 -1)
  %37 = load %Array*, %Array** %arr
  %38 = call %Array* @__quantum__rt__array_copy(%Array* %37, i1 false)
  %39 = call i64 @__quantum__rt__array_get_size_1d(%Array* %38)
  %40 = sub i64 %39, 1
  br label %header__5

exiting__4:                                       ; preds = %exit__6
  %41 = add i64 %i, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 -1)
  %42 = load %Array*, %Array** %arr
  %43 = call i64 @__quantum__rt__array_get_size_1d(%Array* %42)
  %44 = sub i64 %43, 1
  br label %header__7

header__5:                                        ; preds = %exiting__5, %body__4
  %45 = phi i64 [ 0, %body__4 ], [ %51, %exiting__5 ]
  %46 = icmp sle i64 %45, %40
  br i1 %46, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %47 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %38, i64 %45)
  %48 = bitcast i8* %47 to { i64, i64 }**
  %49 = load { i64, i64 }*, { i64, i64 }** %48
  %50 = bitcast { i64, i64 }* %49 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %50, i64 1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %51 = add i64 %45, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  %52 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %53 = bitcast %Tuple* %52 to { i64, i64 }*
  %54 = getelementptr { i64, i64 }, { i64, i64 }* %53, i64 0, i32 0
  %55 = getelementptr { i64, i64 }, { i64, i64 }* %53, i64 0, i32 1
  %56 = add i64 %i, 1
  store i64 %i, i64* %54
  store i64 %56, i64* %55
  %57 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %38, i64 %i)
  %58 = bitcast i8* %57 to { i64, i64 }**
  %59 = load { i64, i64 }*, { i64, i64 }** %58
  %60 = bitcast { i64, i64 }* %59 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %60, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %52, i64 1)
  %61 = load { i64, i64 }*, { i64, i64 }** %58
  %62 = bitcast { i64, i64 }* %61 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %62, i64 -1)
  store { i64, i64 }* %53, { i64, i64 }** %58
  store %Array* %38, %Array** %arr
  call void @__quantum__rt__array_update_access_count(%Array* %38, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %38, i64 -1)
  %63 = sub i64 %39, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %64 = phi i64 [ 0, %exit__5 ], [ %70, %exiting__6 ]
  %65 = icmp sle i64 %64, %63
  br i1 %65, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %66 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %38, i64 %64)
  %67 = bitcast i8* %66 to { i64, i64 }**
  %68 = load { i64, i64 }*, { i64, i64 }** %67
  %69 = bitcast { i64, i64 }* %68 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %69, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %70 = add i64 %64, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %38, i64 -1)
  br label %exiting__4

header__7:                                        ; preds = %exiting__7, %exit__4
  %71 = phi i64 [ 0, %exit__4 ], [ %77, %exiting__7 ]
  %72 = icmp sle i64 %71, %44
  br i1 %72, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %73 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %42, i64 %71)
  %74 = bitcast i8* %73 to { i64, i64 }**
  %75 = load { i64, i64 }*, { i64, i64 }** %74
  %76 = bitcast { i64, i64 }* %75 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %76, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %77 = add i64 %71, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_access_count(%Array* %42, i64 -1)
  br label %header__8

header__8:                                        ; preds = %exiting__8, %exit__7
  %78 = phi i64 [ 0, %exit__7 ], [ %84, %exiting__8 ]
  %79 = icmp sle i64 %78, 9
  br i1 %79, label %body__8, label %exit__8

body__8:                                          ; preds = %header__8
  %80 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %78)
  %81 = bitcast i8* %80 to { i64, i64 }**
  %82 = load { i64, i64 }*, { i64, i64 }** %81
  %83 = bitcast { i64, i64 }* %82 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %83, i64 -1)
  br label %exiting__8

exiting__8:                                       ; preds = %body__8
  %84 = add i64 %78, 1
  br label %header__8

exit__8:                                          ; preds = %header__8
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i64 -1)
  ret %Array* %12
}
