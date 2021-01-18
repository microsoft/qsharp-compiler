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
  %18 = phi i64 [ 0, %exit__1 ], [ %28, %exiting__2 ]
  %19 = icmp sge i64 %18, 9
  %20 = icmp sle i64 %18, 9
  %21 = select i1 true, i1 %20, i1 %19
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %23 = bitcast %Tuple* %22 to { i64, i64 }*
  %24 = getelementptr { i64, i64 }, { i64, i64 }* %23, i64 0, i32 0
  %25 = getelementptr { i64, i64 }, { i64, i64 }* %23, i64 0, i32 1
  store i64 0, i64* %24
  store i64 0, i64* %25
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %18)
  %27 = bitcast i8* %26 to { i64, i64 }**
  store { i64, i64 }* %23, { i64, i64 }** %27
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %28 = add i64 %18, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %arr = alloca %Array*
  store %Array* %17, %Array** %arr
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %29 = phi i64 [ 0, %exit__2 ], [ %35, %exiting__3 ]
  %30 = icmp sle i64 %29, 9
  br i1 %30, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %29)
  %32 = bitcast i8* %31 to { i64, i64 }**
  %33 = load { i64, i64 }*, { i64, i64 }** %32
  %34 = bitcast { i64, i64 }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %34, i64 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %35 = add i64 %29, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_access_count(%Array* %17, i64 1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %i = phi i64 [ 0, %exit__3 ], [ %45, %exiting__4 ]
  %36 = icmp sge i64 %i, 9
  %37 = icmp sle i64 %i, 9
  %38 = select i1 true, i1 %37, i1 %36
  br i1 %38, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %39 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_reference_count(%Array* %39, i64 1)
  %40 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_access_count(%Array* %40, i64 -1)
  %41 = load %Array*, %Array** %arr
  %42 = call %Array* @__quantum__rt__array_copy(%Array* %41, i1 false)
  %43 = call i64 @__quantum__rt__array_get_size_1d(%Array* %42)
  %44 = sub i64 %43, 1
  br label %header__5

exiting__4:                                       ; preds = %exit__6
  %45 = add i64 %i, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_access_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_access_count(%Array* %12, i64 -1)
  %46 = load %Array*, %Array** %arr
  %47 = call i64 @__quantum__rt__array_get_size_1d(%Array* %46)
  %48 = sub i64 %47, 1
  br label %header__7

header__5:                                        ; preds = %exiting__5, %body__4
  %49 = phi i64 [ 0, %body__4 ], [ %55, %exiting__5 ]
  %50 = icmp sle i64 %49, %44
  br i1 %50, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %51 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %42, i64 %49)
  %52 = bitcast i8* %51 to { i64, i64 }**
  %53 = load { i64, i64 }*, { i64, i64 }** %52
  %54 = bitcast { i64, i64 }* %53 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %54, i64 1)
  br label %exiting__5

exiting__5:                                       ; preds = %body__5
  %55 = add i64 %49, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  %56 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %57 = bitcast %Tuple* %56 to { i64, i64 }*
  %58 = getelementptr { i64, i64 }, { i64, i64 }* %57, i64 0, i32 0
  %59 = getelementptr { i64, i64 }, { i64, i64 }* %57, i64 0, i32 1
  %60 = add i64 %i, 1
  store i64 %i, i64* %58
  store i64 %60, i64* %59
  %61 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %42, i64 %i)
  %62 = bitcast i8* %61 to { i64, i64 }**
  %63 = load { i64, i64 }*, { i64, i64 }** %62
  %64 = bitcast { i64, i64 }* %63 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %64, i64 -1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %56, i64 1)
  %65 = load { i64, i64 }*, { i64, i64 }** %62
  %66 = bitcast { i64, i64 }* %65 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %66, i64 -1)
  store { i64, i64 }* %57, { i64, i64 }** %62
  store %Array* %42, %Array** %arr
  call void @__quantum__rt__array_update_access_count(%Array* %42, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %42, i64 -1)
  %67 = sub i64 %43, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %68 = phi i64 [ 0, %exit__5 ], [ %74, %exiting__6 ]
  %69 = icmp sle i64 %68, %67
  br i1 %69, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %70 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %42, i64 %68)
  %71 = bitcast i8* %70 to { i64, i64 }**
  %72 = load { i64, i64 }*, { i64, i64 }** %71
  %73 = bitcast { i64, i64 }* %72 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %73, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %74 = add i64 %68, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_reference_count(%Array* %42, i64 -1)
  br label %exiting__4

header__7:                                        ; preds = %exiting__7, %exit__4
  %75 = phi i64 [ 0, %exit__4 ], [ %81, %exiting__7 ]
  %76 = icmp sle i64 %75, %48
  br i1 %76, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %77 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %46, i64 %75)
  %78 = bitcast i8* %77 to { i64, i64 }**
  %79 = load { i64, i64 }*, { i64, i64 }** %78
  %80 = bitcast { i64, i64 }* %79 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %80, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %81 = add i64 %75, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_access_count(%Array* %46, i64 -1)
  br label %header__8

header__8:                                        ; preds = %exiting__8, %exit__7
  %82 = phi i64 [ 0, %exit__7 ], [ %88, %exiting__8 ]
  %83 = icmp sle i64 %82, 9
  br i1 %83, label %body__8, label %exit__8

body__8:                                          ; preds = %header__8
  %84 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %17, i64 %82)
  %85 = bitcast i8* %84 to { i64, i64 }**
  %86 = load { i64, i64 }*, { i64, i64 }** %85
  %87 = bitcast { i64, i64 }* %86 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %87, i64 -1)
  br label %exiting__8

exiting__8:                                       ; preds = %body__8
  %88 = add i64 %82, 1
  br label %header__8

exit__8:                                          ; preds = %header__8
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i64 -1)
  ret %Array* %12
}
