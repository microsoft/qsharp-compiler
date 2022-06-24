define internal { i64, i64 }* @Microsoft__Quantum__Testing__QIR__TestConditions__body(%String* %input, %Array* %arr) {
entry:
  %res = alloca i64, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 1)
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @4, i32 0, i32 0))
  %1 = call i1 @__quantum__rt__string_equal(%String* %input, %String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  br i1 %1, label %then0__1, label %test1__1

then0__1:                                         ; preds = %entry
  br label %continue__1

test1__1:                                         ; preds = %entry
  %2 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @5, i32 0, i32 0))
  %3 = call i1 @__quantum__rt__string_equal(%String* %input, %String* %2)
  call void @__quantum__rt__string_update_reference_count(%String* %2, i32 -1)
  br i1 %3, label %then1__1, label %test2__1

then1__1:                                         ; preds = %test1__1
  br label %continue__1

test2__1:                                         ; preds = %test1__1
  %4 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %5 = icmp sgt i64 %4, 0
  br i1 %5, label %then2__1, label %continue__1

then2__1:                                         ; preds = %test2__1
  br label %continue__1

continue__1:                                      ; preds = %then2__1, %test2__1, %then1__1, %then0__1
  store i64 0, i64* %res, align 4
  %arg = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arg, i64 0)
  %7 = bitcast i8* %6 to double*
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %arg, i64 1)
  %9 = bitcast i8* %8 to double*
  store double 5.000000e-01, double* %7, align 8
  store double 5.000000e-01, double* %9, align 8
  call void @__quantum__rt__array_update_alias_count(%Array* %arg, i32 1)
  %10 = call i64 @__quantum__qis__drawrandom__body(%Array* %arg)
  call void @__quantum__rt__array_update_alias_count(%Array* %arg, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %arg, i32 -1)
  %11 = icmp eq i64 %10, 0
  br i1 %11, label %then0__2, label %test1__2

then0__2:                                         ; preds = %continue__1
  store i64 1, i64* %res, align 4
  br label %continue__2

test1__2:                                         ; preds = %continue__1
  br label %continue__2

continue__2:                                      ; preds = %test1__2, %then0__2
  %12 = call i64 @__quantum__rt__array_get_size_1d(%Array* %arr)
  %13 = load i64, i64* %res, align 4
  %14 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, i64 }* getelementptr ({ i64, i64 }, { i64, i64 }* null, i32 1) to i64))
  %15 = bitcast %Tuple* %14 to { i64, i64 }*
  %16 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %15, i32 0, i32 0
  %17 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %15, i32 0, i32 1
  store i64 %12, i64* %16, align 4
  store i64 %13, i64* %17, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %arr, i32 -1)
  ret { i64, i64 }* %15
}
