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
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 1)
  %3 = getelementptr { double, %String* }, { double, %String* }* %1, i64 0, i32 1
  %4 = load %String*, %String** %3
  call void @__quantum__rt__string_update_reference_count(%String* %4, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 -1)
  %5 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %6 = bitcast %Tuple* %5 to { double, %String* }*
  %7 = getelementptr { double, %String* }, { double, %String* }* %6, i64 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  %8 = load %String*, %String** %7
  store %String* %name, %String** %7
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 1)
  store { double, %String* }* %6, { double, %String* }** %res
  %9 = load i64, i64* %n
  %10 = add i64 %9, 1
  store i64 %10, i64* %n
  br label %until__1

until__1:                                         ; preds = %repeat__1
  %11 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %12 = load %Result*, %Result** @ResultZero
  %13 = call i1 @__quantum__rt__result_equal(%Result* %11, %Result* %12)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  br i1 %13, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %14 = icmp sgt i64 %10, 100
  br i1 %14, label %then0__1, label %continue__1

then0__1:                                         ; preds = %fixup__1
  %15 = call %String* @__quantum__rt__string_create(i32 19, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @1, i32 0, i32 0))
  %16 = load %String*, %String** %3
  %17 = load %String*, %String** %7
  %18 = load { double, %String* }*, { double, %String* }** %res
  %19 = bitcast { double, %String* }* %18 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %16, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %11, i64 -1)
  %20 = getelementptr { double, %String* }, { double, %String* }* %18, i64 0, i32 1
  %21 = load %String*, %String** %20
  call void @__quantum__rt__string_update_reference_count(%String* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  call void @__quantum__rt__fail(%String* %15)
  unreachable

continue__1:                                      ; preds = %fixup__1
  %22 = load { double, %String* }*, { double, %String* }** %res
  %23 = bitcast { double, %String* }* %22 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %23, i64 -1)
  %24 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %23, i1 false)
  %25 = bitcast %Tuple* %24 to { double, %String* }*
  %26 = getelementptr { double, %String* }, { double, %String* }* %25, i64 0, i32 1
  %27 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i64 1)
  %28 = load %String*, %String** %26
  store %String* %27, %String** %26
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i64 1)
  store { double, %String* }* %25, { double, %String* }** %res
  %29 = load %String*, %String** %3
  %30 = load %String*, %String** %7
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %23, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %28, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %27, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  br label %repeat__1

rend__1:                                          ; preds = %until__1
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %4, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %11, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  %31 = load i64, i64* %n
  ret i64 %31
}
