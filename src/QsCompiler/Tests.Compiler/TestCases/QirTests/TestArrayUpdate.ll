define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 1)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  %0 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 0)
  %2 = bitcast i8* %1 to %String**
  %3 = load %String*, %String** %2
  store %String* %b, %String** %2
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 1)
  store %Array* %0, %Array** %x
  call void @__quantum__rt__array_update_access_count(%Array* %0, i64 -1)
  %4 = call %Array* @__quantum__rt__array_copy(%Array* %0, i1 false)
  %5 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @0, i32 0, i32 0))
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 1)
  %7 = bitcast i8* %6 to %String**
  %8 = load %String*, %String** %7
  store %String* %5, %String** %7
  call void @__quantum__rt__array_update_access_count(%Array* %4, i64 1)
  store %Array* %4, %Array** %x
  %9 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  %10 = call i64 @__quantum__rt__array_get_size_1d(%Array* %y)
  %11 = sub i64 %10, 1
  br label %header__2

header__1:                                        ; preds = %exiting__1, %exit__3
  %12 = phi i64 [ 0, %entry ], [ %20, %exiting__1 ]
  %13 = icmp sle i64 %12, 9
  br i1 %13, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %15 = bitcast %Tuple* %14 to { i64, i64 }*
  %16 = getelementptr { i64, i64 }, { i64, i64 }* %15, i64 0, i32 0
  %17 = getelementptr { i64, i64 }, { i64, i64 }* %15, i64 0, i32 1
  store i64 0, i64* %16
  store i64 0, i64* %17
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %12)
  %19 = bitcast i8* %18 to { i64, i64 }**
  store { i64, i64 }* %15, { i64, i64 }** %19
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %20 = add i64 %12, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %arr = alloca %Array*
  store %Array* %9, %Array** %arr
  br label %header__5

header__2:                                        ; preds = %exiting__2, %entry
  %21 = phi i64 [ 0, %entry ], [ %26, %exiting__2 ]
  %22 = icmp sle i64 %21, %11
  br i1 %22, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %23 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %y, i64 %21)
  %24 = bitcast i8* %23 to %String**
  %25 = load %String*, %String** %24
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %26 = add i64 %21, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %b, i64 1)
  %27 = call i64 @__quantum__rt__array_get_size_1d(%Array* %0)
  %28 = sub i64 %27, 1
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %29 = phi i64 [ 0, %exit__2 ], [ %34, %exiting__3 ]
  %30 = icmp sle i64 %29, %28
  br i1 %30, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %29)
  %32 = bitcast i8* %31 to %String**
  %33 = load %String*, %String** %32
  call void @__quantum__rt__string_update_reference_count(%String* %33, i64 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %34 = add i64 %29, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %5, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i64 1)
  br label %header__1

header__4:                                        ; preds = %exiting__4, %exit__5
  %35 = phi i64 [ 0, %exit__1 ], [ %41, %exiting__4 ]
  %36 = icmp sle i64 %35, 9
  br i1 %36, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %37 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %35)
  %38 = bitcast i8* %37 to { i64, i64 }**
  %39 = load { i64, i64 }*, { i64, i64 }** %38
  %40 = bitcast { i64, i64 }* %39 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %40, i64 1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %41 = add i64 %35, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_access_count(%Array* %9, i64 1)
  br label %header__6

header__5:                                        ; preds = %exiting__5, %exit__1
  %42 = phi i64 [ 0, %exit__1 ], [ %48, %exiting__5 ]
  %43 = icmp sle i64 %42, 9
  br i1 %43, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %44 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %42)
  %45 = bitcast i8* %44 to { i64, i64 }**
  %46 = load { i64, i64 }*, { i64, i64 }** %45
  %47 = bitcast { i64, i64 }* %46 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %47, i64 1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %48 = add i64 %42, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i64 1)
  br label %header__4

header__6:                                        ; preds = %exiting__6, %exit__4
  %i = phi i64 [ 0, %exit__4 ], [ %61, %exiting__6 ]
  %49 = icmp sle i64 %i, 9
  br i1 %49, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %50 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_access_count(%Array* %50, i64 -1)
  %51 = call %Array* @__quantum__rt__array_copy(%Array* %50, i1 false)
  %52 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %53 = bitcast %Tuple* %52 to { i64, i64 }*
  %54 = getelementptr { i64, i64 }, { i64, i64 }* %53, i64 0, i32 0
  %55 = getelementptr { i64, i64 }, { i64, i64 }* %53, i64 0, i32 1
  %56 = add i64 %i, 1
  store i64 %i, i64* %54
  store i64 %56, i64* %55
  %57 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %51, i64 %i)
  %58 = bitcast i8* %57 to { i64, i64 }**
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %52, i64 1)
  %59 = load { i64, i64 }*, { i64, i64 }** %58
  %60 = bitcast { i64, i64 }* %59 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %60, i64 -1)
  store { i64, i64 }* %53, { i64, i64 }** %58
  call void @__quantum__rt__array_update_access_count(%Array* %51, i64 1)
  store %Array* %51, %Array** %arr
  call void @__quantum__rt__array_update_reference_count(%Array* %50, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %60, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %61 = add i64 %i, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  %62 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_access_count(%Array* %4, i64 -1)
  %63 = call i64 @__quantum__rt__array_get_size_1d(%Array* %62)
  %64 = sub i64 %63, 1
  br label %header__7

header__7:                                        ; preds = %exiting__7, %exit__6
  %65 = phi i64 [ 0, %exit__6 ], [ %71, %exiting__7 ]
  %66 = icmp sle i64 %65, %64
  br i1 %66, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %67 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %62, i64 %65)
  %68 = bitcast i8* %67 to { i64, i64 }**
  %69 = load { i64, i64 }*, { i64, i64 }** %68
  %70 = bitcast { i64, i64 }* %69 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %70, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %71 = add i64 %65, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_access_count(%Array* %62, i64 -1)
  %72 = call i64 @__quantum__rt__array_get_size_1d(%Array* %4)
  %73 = sub i64 %72, 1
  br label %header__8

header__8:                                        ; preds = %exiting__8, %exit__7
  %74 = phi i64 [ 0, %exit__7 ], [ %79, %exiting__8 ]
  %75 = icmp sle i64 %74, %73
  br i1 %75, label %body__8, label %exit__8

body__8:                                          ; preds = %header__8
  %76 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %4, i64 %74)
  %77 = bitcast i8* %76 to %String**
  %78 = load %String*, %String** %77
  call void @__quantum__rt__string_update_reference_count(%String* %78, i64 -1)
  br label %exiting__8

exiting__8:                                       ; preds = %body__8
  %79 = add i64 %74, 1
  br label %header__8

exit__8:                                          ; preds = %header__8
  call void @__quantum__rt__array_update_reference_count(%Array* %4, i64 -1)
  %80 = sub i64 %63, 1
  br label %header__9

header__9:                                        ; preds = %exiting__9, %exit__8
  %81 = phi i64 [ 0, %exit__8 ], [ %87, %exiting__9 ]
  %82 = icmp sle i64 %81, %80
  br i1 %82, label %body__9, label %exit__9

body__9:                                          ; preds = %header__9
  %83 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %62, i64 %81)
  %84 = bitcast i8* %83 to { i64, i64 }**
  %85 = load { i64, i64 }*, { i64, i64 }** %84
  %86 = bitcast { i64, i64 }* %85 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %86, i64 -1)
  br label %exiting__9

exiting__9:                                       ; preds = %body__9
  %87 = add i64 %81, 1
  br label %header__9

exit__9:                                          ; preds = %header__9
  call void @__quantum__rt__array_update_reference_count(%Array* %62, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %3, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %0, i64 -1)
  br label %header__10

header__10:                                       ; preds = %exiting__10, %exit__9
  %88 = phi i64 [ 0, %exit__9 ], [ %94, %exiting__10 ]
  %89 = icmp sle i64 %88, 9
  br i1 %89, label %body__10, label %exit__10

body__10:                                         ; preds = %header__10
  %90 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %9, i64 %88)
  %91 = bitcast i8* %90 to { i64, i64 }**
  %92 = load { i64, i64 }*, { i64, i64 }** %91
  %93 = bitcast { i64, i64 }* %92 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %93, i64 -1)
  br label %exiting__10

exiting__10:                                      ; preds = %body__10
  %94 = add i64 %88, 1
  br label %header__10

exit__10:                                         ; preds = %header__10
  call void @__quantum__rt__array_update_reference_count(%Array* %9, i64 -1)
  ret %Array* %4
}
