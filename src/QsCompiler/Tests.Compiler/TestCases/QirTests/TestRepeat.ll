define i64 @Microsoft__Quantum__Testing__QIR__TestRepeat__body(%Qubit* %q) {
entry:
  %n = alloca i64
  store i64 0, i64* %n
  br label %repeat__1

repeat__1:                                        ; preds = %continue__1, %entry
  call void @__quantum__qis__t__body(%Qubit* %q)
  call void @__quantum__qis__x__body(%Qubit* %q)
  call void @__quantum__qis__t__adj(%Qubit* %q)
  call void @__quantum__qis__h__body(%Qubit* %q)
  %name = call %String* @__quantum__rt__string_create(i32 6, i8* getelementptr inbounds ([7 x i8], [7 x i8]* @0, i32 0, i32 0))
  %0 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %1 = call { double, %String* }* @Microsoft__Quantum__Testing__QIR__Energy__body(double 0.000000e+00, %String* %0)
  %res = alloca { double, %String* }*
  store { double, %String* }* %1, { double, %String* }** %res
  %2 = bitcast { double, %String* }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %2, i64 -1)
  %3 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %4 = bitcast %Tuple* %3 to { double, %String* }*
  %5 = getelementptr { double, %String* }, { double, %String* }* %4, i64 0, i32 1
  %6 = load %String*, %String** %5
  store %String* %name, %String** %5
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %3, i64 1)
  store { double, %String* }* %4, { double, %String* }** %res
  %7 = load i64, i64* %n
  %8 = add i64 %7, 1
  store i64 %8, i64* %n
  br label %until__1

until__1:                                         ; preds = %repeat__1
  %9 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %10 = load %Result*, %Result** @ResultZero
  %11 = call i1 @__quantum__rt__result_equal(%Result* %9, %Result* %10)
  %12 = getelementptr { double, %String* }, { double, %String* }* %1, i64 0, i32 1
  %13 = load %String*, %String** %12
  call void @__quantum__rt__string_update_reference_count(%String* %13, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 1)
  br i1 %11, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %14 = icmp sgt i64 %8, 100
  br i1 %14, label %then0__1, label %continue__1

then0__1:                                         ; preds = %fixup__1
  %15 = call %String* @__quantum__rt__string_create(i32 19, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @1, i32 0, i32 0))
  %16 = load { double, %String* }*, { double, %String* }** %res
  %17 = load i64, i64* %n
  %18 = bitcast { double, %String* }* %16 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %18, i64 -1)
  %19 = getelementptr { double, %String* }, { double, %String* }* %16, i64 0, i32 1
  %20 = load %String*, %String** %19
  call void @__quantum__rt__string_update_reference_count(%String* %20, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  %21 = load %String*, %String** %5
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  %22 = load %String*, %String** %12
  call void @__quantum__rt__string_update_reference_count(%String* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %9, i64 -1)
  call void @__quantum__rt__fail(%String* %15)
  unreachable

continue__1:                                      ; preds = %fixup__1
  %23 = load { double, %String* }*, { double, %String* }** %res
  %24 = bitcast { double, %String* }* %23 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %24, i64 -1)
  %25 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %24, i1 false)
  %26 = bitcast %Tuple* %25 to { double, %String* }*
  %27 = getelementptr { double, %String* }, { double, %String* }* %26, i64 0, i32 1
  %28 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  %29 = load %String*, %String** %27
  store %String* %28, %String** %27
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %25, i64 1)
  store { double, %String* }* %26, { double, %String* }** %res
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %25, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  %30 = load %String*, %String** %5
  call void @__quantum__rt__string_update_reference_count(%String* %30, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  %31 = load %String*, %String** %12
  call void @__quantum__rt__string_update_reference_count(%String* %31, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %9, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %28, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %25, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 -1)
  br label %repeat__1

rend__1:                                          ; preds = %until__1
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %13, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %9, i64 -1)
  %32 = load i64, i64* %n
  ret i64 %32
}
