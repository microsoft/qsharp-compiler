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
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %2, i64 -1)
  %3 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %4 = bitcast %Tuple* %3 to { double, %String* }*
  %5 = getelementptr { double, %String* }, { double, %String* }* %4, i64 0, i32 0
  %6 = getelementptr { double, %String* }, { double, %String* }* %4, i64 0, i32 1
  %7 = load %String*, %String** %6
  store %String* %name, %String** %6
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %3, i64 1)
  store { double, %String* }* %4, { double, %String* }** %res
  %8 = load i64, i64* %n
  %9 = add i64 %8, 1
  store i64 %9, i64* %n
  br label %until__1

until__1:                                         ; preds = %repeat__1
  %10 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %11 = load %Result*, %Result** @ResultZero
  %12 = call i1 @__quantum__rt__result_equal(%Result* %10, %Result* %11)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  br i1 %12, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %13 = icmp sgt i64 %9, 100
  br i1 %13, label %then0__1, label %continue__1

then0__1:                                         ; preds = %fixup__1
  %14 = call %String* @__quantum__rt__string_create(i32 19, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @1, i32 0, i32 0))
  %15 = load { double, %String* }*, { double, %String* }** %res
  %16 = bitcast { double, %String* }* %15 to %Tuple*
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %16, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  %17 = load %String*, %String** %6
  call void @__quantum__rt__string_update_reference_count(%String* %17, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %7, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %10, i64 -1)
  call void @__quantum__rt__fail(%String* %14)
  unreachable

continue__1:                                      ; preds = %fixup__1
  %18 = load { double, %String* }*, { double, %String* }** %res
  %19 = bitcast { double, %String* }* %18 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %19, i64 -1)
  %20 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %19, i1 false)
  %21 = bitcast %Tuple* %20 to { double, %String* }*
  %22 = getelementptr { double, %String* }, { double, %String* }* %21, i64 0, i32 0
  %23 = getelementptr { double, %String* }, { double, %String* }* %21, i64 0, i32 1
  %24 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  store %String* %24, %String** %23
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %20, i64 1)
  store { double, %String* }* %21, { double, %String* }** %res
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %20, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  %25 = load %String*, %String** %6
  call void @__quantum__rt__string_update_reference_count(%String* %25, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %7, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %10, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %24, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %20, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  br label %repeat__1

rend__1:                                          ; preds = %until__1
  call void @__quantum__rt__tuple_update_access_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %7, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %10, i64 -1)
  %26 = load i64, i64* %n
  ret i64 %26
}
