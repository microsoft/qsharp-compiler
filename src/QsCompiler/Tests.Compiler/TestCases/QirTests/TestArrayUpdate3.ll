define %Array* @Microsoft__Quantum__Testing__QIR__TestArrayUpdate3__body(%Array* %y, %String* %b) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 1)
  %x = alloca %Array*
  store %Array* %y, %Array** %x
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 1)
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
  %6 = load %String*, %String** %5
  call void @__quantum__rt__string_update_reference_count(%String* %6, i64 1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %7 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 -1)
  %8 = call %Array* @__quantum__rt__array_copy(%Array* %y, i1 false)
  %9 = icmp ne %Array* %y, %8
  %10 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %8, i64 0)
  %11 = bitcast i8* %10 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %b, i64 1)
  %12 = load %String*, %String** %11
  br i1 %9, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %exit__1
  call void @__quantum__rt__string_update_reference_count(%String* %b, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %12, i64 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %exit__1
  store %String* %b, %String** %11
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i64 1)
  store %Array* %8, %Array** %x
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i64 -1)
  %13 = call %Array* @__quantum__rt__array_copy(%Array* %8, i1 false)
  %14 = icmp ne %Array* %8, %13
  %15 = call %String* @__quantum__rt__string_create(i32 5, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0))
  %16 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %13, i64 1)
  %17 = bitcast i8* %16 to %String**
  call void @__quantum__rt__string_update_reference_count(%String* %15, i64 1)
  %18 = load %String*, %String** %17
  br i1 %14, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %condContinue__1
  call void @__quantum__rt__string_update_reference_count(%String* %15, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i64 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %condContinue__1
  store %String* %15, %String** %17
  call void @__quantum__rt__array_update_reference_count(%Array* %13, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %13, i64 1)
  store %Array* %13, %Array** %x
  %19 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 10)
  br label %header__2

header__2:                                        ; preds = %exiting__2, %condContinue__2
  %20 = phi i64 [ 0, %condContinue__2 ], [ %28, %exiting__2 ]
  %21 = icmp sle i64 %20, 9
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %23 = bitcast %Tuple* %22 to { i64, i64 }*
  %24 = getelementptr { i64, i64 }, { i64, i64 }* %23, i64 0, i32 0
  %25 = getelementptr { i64, i64 }, { i64, i64 }* %23, i64 0, i32 1
  store i64 0, i64* %24
  store i64 0, i64* %25
  %26 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 %20)
  %27 = bitcast i8* %26 to { i64, i64 }**
  store { i64, i64 }* %23, { i64, i64 }** %27
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %28 = add i64 %20, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %arr = alloca %Array*
  store %Array* %19, %Array** %arr
  br label %header__3

header__3:                                        ; preds = %exiting__3, %exit__2
  %29 = phi i64 [ 0, %exit__2 ], [ %35, %exiting__3 ]
  %30 = icmp sle i64 %29, 9
  br i1 %30, label %body__3, label %exit__3

body__3:                                          ; preds = %header__3
  %31 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 %29)
  %32 = bitcast i8* %31 to { i64, i64 }**
  %33 = load { i64, i64 }*, { i64, i64 }** %32
  %34 = bitcast { i64, i64 }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %34, i64 1)
  br label %exiting__3

exiting__3:                                       ; preds = %body__3
  %35 = add i64 %29, 1
  br label %header__3

exit__3:                                          ; preds = %header__3
  call void @__quantum__rt__array_update_alias_count(%Array* %19, i64 1)
  br label %header__4

header__4:                                        ; preds = %exiting__4, %exit__3
  %36 = phi i64 [ 0, %exit__3 ], [ %42, %exiting__4 ]
  %37 = icmp sle i64 %36, 9
  br i1 %37, label %body__4, label %exit__4

body__4:                                          ; preds = %header__4
  %38 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 %36)
  %39 = bitcast i8* %38 to { i64, i64 }**
  %40 = load { i64, i64 }*, { i64, i64 }** %39
  %41 = bitcast { i64, i64 }* %40 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %41, i64 1)
  br label %exiting__4

exiting__4:                                       ; preds = %body__4
  %42 = add i64 %36, 1
  br label %header__4

exit__4:                                          ; preds = %header__4
  call void @__quantum__rt__array_update_reference_count(%Array* %19, i64 1)
  br label %header__5

header__5:                                        ; preds = %exiting__5, %exit__4
  %i = phi i64 [ 0, %exit__4 ], [ %56, %exiting__5 ]
  %43 = icmp sle i64 %i, 9
  br i1 %43, label %body__5, label %exit__5

body__5:                                          ; preds = %header__5
  %44 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_alias_count(%Array* %44, i64 -1)
  %45 = call %Array* @__quantum__rt__array_copy(%Array* %44, i1 false)
  %46 = icmp ne %Array* %44, %45
  %47 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %48 = bitcast %Tuple* %47 to { i64, i64 }*
  %49 = getelementptr { i64, i64 }, { i64, i64 }* %48, i64 0, i32 0
  %50 = getelementptr { i64, i64 }, { i64, i64 }* %48, i64 0, i32 1
  %51 = add i64 %i, 1
  store i64 %i, i64* %49
  store i64 %51, i64* %50
  %52 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %45, i64 %i)
  %53 = bitcast i8* %52 to { i64, i64 }**
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %47, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %47, i64 1)
  %54 = load { i64, i64 }*, { i64, i64 }** %53
  %55 = bitcast { i64, i64 }* %54 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %55, i64 -1)
  br i1 %46, label %condContinue__3, label %condFalse__3

condFalse__3:                                     ; preds = %body__5
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %47, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %55, i64 -1)
  br label %condContinue__3

condContinue__3:                                  ; preds = %condFalse__3, %body__5
  store { i64, i64 }* %48, { i64, i64 }** %53
  call void @__quantum__rt__array_update_reference_count(%Array* %45, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %45, i64 1)
  store %Array* %45, %Array** %arr
  call void @__quantum__rt__array_update_reference_count(%Array* %44, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %47, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %55, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %45, i64 -1)
  br label %exiting__5

exiting__5:                                       ; preds = %condContinue__3
  %56 = add i64 %i, 1
  br label %header__5

exit__5:                                          ; preds = %header__5
  %57 = load %Array*, %Array** %arr
  call void @__quantum__rt__array_update_alias_count(%Array* %y, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %13, i64 -1)
  %58 = call i64 @__quantum__rt__array_get_size_1d(%Array* %57)
  %59 = sub i64 %58, 1
  br label %header__6

header__6:                                        ; preds = %exiting__6, %exit__5
  %60 = phi i64 [ 0, %exit__5 ], [ %66, %exiting__6 ]
  %61 = icmp sle i64 %60, %59
  br i1 %61, label %body__6, label %exit__6

body__6:                                          ; preds = %header__6
  %62 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %57, i64 %60)
  %63 = bitcast i8* %62 to { i64, i64 }**
  %64 = load { i64, i64 }*, { i64, i64 }** %63
  %65 = bitcast { i64, i64 }* %64 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %65, i64 -1)
  br label %exiting__6

exiting__6:                                       ; preds = %body__6
  %66 = add i64 %60, 1
  br label %header__6

exit__6:                                          ; preds = %header__6
  call void @__quantum__rt__array_update_alias_count(%Array* %57, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %y, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %12, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %15, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %13, i64 -1)
  br label %header__7

header__7:                                        ; preds = %exiting__7, %exit__6
  %67 = phi i64 [ 0, %exit__6 ], [ %73, %exiting__7 ]
  %68 = icmp sle i64 %67, 9
  br i1 %68, label %body__7, label %exit__7

body__7:                                          ; preds = %header__7
  %69 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %19, i64 %67)
  %70 = bitcast i8* %69 to { i64, i64 }**
  %71 = load { i64, i64 }*, { i64, i64 }** %70
  %72 = bitcast { i64, i64 }* %71 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %72, i64 -1)
  br label %exiting__7

exiting__7:                                       ; preds = %body__7
  %73 = add i64 %67, 1
  br label %header__7

exit__7:                                          ; preds = %header__7
  call void @__quantum__rt__array_update_reference_count(%Array* %19, i64 -1)
  %74 = sub i64 %58, 1
  br label %header__8

header__8:                                        ; preds = %exiting__8, %exit__7
  %75 = phi i64 [ 0, %exit__7 ], [ %81, %exiting__8 ]
  %76 = icmp sle i64 %75, %74
  br i1 %76, label %body__8, label %exit__8

body__8:                                          ; preds = %header__8
  %77 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %57, i64 %75)
  %78 = bitcast i8* %77 to { i64, i64 }**
  %79 = load { i64, i64 }*, { i64, i64 }** %78
  %80 = bitcast { i64, i64 }* %79 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %80, i64 -1)
  br label %exiting__8

exiting__8:                                       ; preds = %body__8
  %81 = add i64 %75, 1
  br label %header__8

exit__8:                                          ; preds = %header__8
  call void @__quantum__rt__array_update_reference_count(%Array* %57, i64 -1)
  ret %Array* %13
}
