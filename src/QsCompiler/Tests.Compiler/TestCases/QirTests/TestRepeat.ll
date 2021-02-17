define i64 @Microsoft__Quantum__Testing__QIR__TestRepeat__body(%Qubit* %q) {
entry:
  %n = alloca i64
  store i64 0, i64* %n
  br label %repeat__1

repeat__1:                                        ; preds = %condContinue__2, %entry
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
  %3 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 1
  %4 = load %String*, %String** %3
  call void @__quantum__rt__string_update_reference_count(%String* %4, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 -1)
  %5 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %6 = icmp ne %Tuple* %2, %5
  %7 = bitcast %Tuple* %5 to { double, %String* }*
  %8 = getelementptr inbounds { double, %String* }, { double, %String* }* %7, i32 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  %9 = load %String*, %String** %8
  br i1 %6, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %repeat__1
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %repeat__1
  store %String* %name, %String** %8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 1)
  store { double, %String* }* %7, { double, %String* }** %res
  %10 = load i64, i64* %n
  %11 = add i64 %10, 1
  store i64 %11, i64* %n
  br label %until__1

until__1:                                         ; preds = %condContinue__1
  %12 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %13 = load %Result*, %Result** @ResultZero
  %14 = call i1 @__quantum__rt__result_equal(%Result* %12, %Result* %13)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  br i1 %14, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  %15 = icmp sgt i64 %11, 100
  br i1 %15, label %then0__1, label %continue__1

then0__1:                                         ; preds = %fixup__1
  %16 = call %String* @__quantum__rt__string_create(i32 19, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @1, i32 0, i32 0))
  %17 = load %String*, %String** %3
  %18 = load %String*, %String** %8
  %19 = load { double, %String* }*, { double, %String* }** %res
  %20 = bitcast { double, %String* }* %19 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %20, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %17, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %18, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i64 -1)
  %21 = getelementptr inbounds { double, %String* }, { double, %String* }* %19, i32 0, i32 1
  %22 = load %String*, %String** %21
  call void @__quantum__rt__string_update_reference_count(%String* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %20, i64 -1)
  call void @__quantum__rt__fail(%String* %16)
  unreachable

continue__1:                                      ; preds = %fixup__1
  %23 = load { double, %String* }*, { double, %String* }** %res
  %24 = bitcast { double, %String* }* %23 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i64 -1)
  %25 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %24, i1 false)
  %26 = icmp ne %Tuple* %24, %25
  %27 = bitcast %Tuple* %25 to { double, %String* }*
  %28 = getelementptr inbounds { double, %String* }, { double, %String* }* %27, i32 0, i32 1
  %29 = call %String* @__quantum__rt__string_create(i32 0, i8* null)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 1)
  %30 = load %String*, %String** %28
  br i1 %26, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %continue__1
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i64 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %continue__1
  store %String* %29, %String** %28
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %25, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %25, i64 1)
  store { double, %String* }* %27, { double, %String* }** %res
  %31 = load %String*, %String** %3
  %32 = load %String*, %String** %8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %25, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %31, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %32, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %30, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %25, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %29, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %25, i64 -1)
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
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  %33 = load i64, i64* %n
  ret i64 %33
}
