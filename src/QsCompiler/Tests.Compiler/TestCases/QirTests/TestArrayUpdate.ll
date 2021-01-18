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
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 -1)
  store %Array* %0, %Array** %x
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
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  store %Array* %12, %Array** %x
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
  %i = phi i64 [ 0, %exit__3 ], [ %39, %exiting__4 ]
  %34 = icmp sle i64 %i, 9
  br i1 %34, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %35 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i64 1)
  call void @__quantum__rt__array_update_access_count(%Array* %35, i64 -1)
  %36 = call %Array* @__quantum__rt__array_copy(%Array* %35, i1 false)
  %37 = call i64 @__quantum__rt__array_get_size_1d(%Array* %36)
  %38 = sub i64 %37, 1
  br label %header__5

exiting__4:                                       ; preds = %exit__6
  %39 = add i64 %i, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 -1)
  %40 = load %Array*, %Array** %arr
  %41 = call i64 @__quantum__rt__array_get_size_1d(%Array* %40)
  %42 = sub i64 %41, 1
  br label %header__7

header__5:                                        ; preds = %exiting__5, %body__4
  %43 = phi i64 [ 0, %body__4 ], [ %49, %exiting__5 ]
  %44 = icmp sle i64 %43, %38
  br i1 %44, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %45 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %36, i64 %43)
  %46 = bitcast i8* %45 to { i64, i64 }**
  %47 = load { i64, i64 }*, { i64, i64 }** %46
  %48 = bitcast { i64, i64 }* %47 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %48, i64 1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %49 = add i64 %43, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  %50 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %51 = bitcast %Tuple* %50 to { i64, i64 }*
  %52 = getelementptr { i64, i64 }, { i64, i64 }* %51, i64 0, i32 0
  %53 = getelementptr { i64, i64 }, { i64, i64 }* %51, i64 0, i32 1
  %54 = add i64 %i, 1
  store i64 %i, i64* %52
  store i64 %54, i64* %53
  %55 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %36, i64 %i)
  %56 = bitcast i8* %55 to { i64, i64 }**
  %57 = load { i64, i64 }*, { i64, i64 }** %56
  %58 = bitcast { i64, i64 }* %57 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %58, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %50, i64 1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %58, i64 -1)
  store { i64, i64 }* %51, { i64, i64 }** %56
  call void @__quantum__rt__array_update_access_count(%Array* %36, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %35, i64 -1)
  store %Array* %36, %Array** %arr
  %59 = sub i64 %37, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %60 = phi i64 [ 0, %exit__5 ], [ %66, %exiting__6 ]
  %61 = icmp sle i64 %60, %59
  br i1 %61, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %62 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %36, i64 %60)
  %63 = bitcast i8* %62 to { i64, i64 }**
  %64 = load { i64, i64 }*, { i64, i64 }** %63
  %65 = bitcast { i64, i64 }* %64 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %65, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %66 = add i64 %60, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %36, i64 -1)
  br label %exiting__4

header__7:                                        ; preds = %exiting__7, %exit__4
  %67 = phi i64 [ 0, %exit__4 ], [ %73, %exiting__7 ]
  %68 = icmp sle i64 %67, %42
  br i1 %68, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %69 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %40, i64 %67)
  %70 = bitcast i8* %69 to { i64, i64 }**
  %71 = load { i64, i64 }*, { i64, i64 }** %70
  %72 = bitcast { i64, i64 }* %71 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %72, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %73 = add i64 %67, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_access_count(%Array* %40, i64 -1)
  br label %header__8

header__8:                                        ; preds = %exiting__8, %exit__7
  %74 = phi i64 [ 0, %exit__7 ], [ %80, %exiting__8 ]
  %75 = icmp sle i64 %74, 9
  br i1 %75, label %body__8, label %exit__8

body__8:                                          ; preds = %header__8
  %76 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %74)
  %77 = bitcast i8* %76 to { i64, i64 }**
  %78 = load { i64, i64 }*, { i64, i64 }** %77
  %79 = bitcast { i64, i64 }* %78 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %79, i64 -1)
  br label %exiting__8

exiting__8:                                       ; preds = %body__8
  %80 = add i64 %74, 1
  br label %header__8

exit__8:                                          ; preds = %header__8
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i64 -1)
  ret %Array* %12
}
