define internal i64 @Microsoft__Quantum__Testing__QIR__TestRepeat1__body(%Qubit* %q) {
entry:
  %res = alloca { double, %String* }*, align 8
  %n = alloca i64, align 8
  store i64 0, i64* %n, align 4
  br label %repeat__1

repeat__1:                                        ; preds = %continue__1, %entry
  call void @__quantum__qis__t__body(%Qubit* %q)
  call void @__quantum__qis__x__body(%Qubit* %q)
  call void @__quantum__qis__t__adj(%Qubit* %q)
  call void @__quantum__qis__h__body(%Qubit* %q)
  %name = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @0, i32 0, i32 0))
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @1, i32 0, i32 0))
  %1 = call { double, %String* }* @Microsoft__Quantum__Testing__QIR__Energy__body(double 0.000000e+00, %String* %0)
  store { double, %String* }* %1, { double, %String* }** %res, align 8
  %2 = bitcast { double, %String* }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 -1)
  %3 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %4 = bitcast %Tuple* %3 to { double, %String* }*
  %5 = getelementptr inbounds { double, %String* }, { double, %String* }* %4, i32 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 1)
  %6 = load %String*, %String** %5, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %6, i32 -1)
  store %String* %name, %String** %5, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 1)
  store { double, %String* }* %4, { double, %String* }** %res, align 8
  %7 = load i64, i64* %n, align 4
  %8 = add i64 %7, 1
  store i64 %8, i64* %n, align 4
  br label %until__1

until__1:                                         ; preds = %repeat__1
  %9 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %10 = call %Result* @__quantum__rt__result_get_zero()
  %11 = call i1 @__quantum__rt__result_equal(%Result* %9, %Result* %10)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 1)
  br i1 %11, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %12 = icmp sgt i64 %8, 100
  br i1 %12, label %then0__1, label %continue__1

then0__1:                                         ; preds = %fixup__1
  %13 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([20 x i8], [20 x i8]* @2, i32 0, i32 0))
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %9, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__fail(%String* %13)
  unreachable

continue__1:                                      ; preds = %fixup__1
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 -1)
  %14 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %3, i1 false)
  %15 = bitcast %Tuple* %14 to { double, %String* }*
  %16 = getelementptr inbounds { double, %String* }, { double, %String* }* %15, i32 0, i32 1
  %17 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([1 x i8], [1 x i8]* @1, i32 0, i32 0))
  %18 = load %String*, %String** %16, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %18, i32 -1)
  store %String* %17, %String** %16, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 1)
  store { double, %String* }* %15, { double, %String* }** %res, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %14, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 -1)
  br label %repeat__1

rend__1:                                          ; preds = %until__1
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %9, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  %19 = load i64, i64* %n, align 4
  ret i64 %19
}
