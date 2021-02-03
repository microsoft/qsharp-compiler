define i64 @Microsoft__Quantum__Testing__QIR__TestScoping__body(%Array* %a) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i64 1)
  %sum = alloca i64
  store i64 0, i64* %sum
  %0 = call i64 @__quantum__rt__array_get_size_1d(%Array* %a)
  %1 = sub i64 %0, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %2 = phi i64 [ 0, %entry ], [ %11, %exiting__1 ]
  %3 = icmp sle i64 %2, %1
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %2)
  %5 = bitcast i8* %4 to i64*
  %i = load i64, i64* %5
  %6 = icmp sgt i64 %i, 5
  br i1 %6, label %then0__1, label %else__1

then0__1:                                         ; preds = %body__1
  %x = add i64 %i, 3
  %7 = load i64, i64* %sum
  %8 = add i64 %7, %x
  store i64 %8, i64* %sum
  br label %continue__1

else__1:                                          ; preds = %body__1
  %x__1 = mul i64 %i, 2
  %9 = load i64, i64* %sum
  %10 = add i64 %9, %x__1
  store i64 %10, i64* %sum
  br label %continue__1

continue__1:                                      ; preds = %else__1, %then0__1
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %11 = add i64 %2, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %12 = sub i64 %0, 1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %exit__1
  %13 = phi i64 [ 0, %exit__1 ], [ %19, %exiting__2 ]
  %14 = icmp sle i64 %13, %12
  br i1 %14, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 %13)
  %16 = bitcast i8* %15 to i64*
  %i__1 = load i64, i64* %16
  %17 = load i64, i64* %sum
  %18 = add i64 %17, %i__1
  store i64 %18, i64* %sum
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %19 = add i64 %13, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  %20 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64))
  %21 = bitcast %Tuple* %20 to { %Callable*, i64 }*
  %22 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %21, i64 0, i32 0
  %23 = getelementptr { %Callable*, i64 }, { %Callable*, i64 }* %21, i64 0, i32 1
  %24 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Testing__QIR__Foo, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  store %Callable* %24, %Callable** %22
  store i64 1, i64* %23
  %25 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__3, [2 x void (%Tuple*, i64)*]* @MemoryManagement__3, %Tuple* %20)
  %26 = load i64, i64* %sum
  call void @__quantum__rt__array_update_alias_count(%Array* %a, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %25, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %25, i64 -1)
  ret i64 %26
}
