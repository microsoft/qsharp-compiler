define { %String*, { double, double }*, { double, double }* }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate2__body(i1 %cond, { %String*, { double, double }*, { double, double }* }* %arg) {
entry:
  %0 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i32 0, i32 1
  %1 = load { double, double }*, { double, double }** %0
  %2 = bitcast { double, double }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 1)
  %3 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i32 0, i32 2
  %4 = load { double, double }*, { double, double }** %3
  %5 = bitcast { double, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 1)
  %6 = bitcast { %String*, { double, double }*, { double, double }* }* %arg to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 1)
  %namedValue = alloca { %String*, { double, double }*, { double, double }* }*
  store { %String*, { double, double }*, { double, double }* }* %arg, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 1)
  %7 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i32 0, i32 0
  %8 = load %String*, %String** %7
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 1)
  %9 = call %String* @__quantum__rt__string_create(i32 4, i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i32 0, i32 0))
  %10 = call i1 @__quantum__rt__string_equal(%String* %8, %String* %9)
  br i1 %10, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 -1)
  %11 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %6, i1 false)
  %12 = icmp ne %Tuple* %6, %11
  %13 = bitcast %Tuple* %11 to { %String*, { double, double }*, { double, double }* }*
  %14 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %13, i32 0, i32 1
  %15 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 0.000000e+00, double 0.000000e+00)
  %16 = bitcast { double, double }* %15 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %16, i64 1)
  %17 = load { double, double }*, { double, double }** %14
  %18 = bitcast { double, double }* %17 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i64 -1)
  br i1 %12, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %then0__1
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i64 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %then0__1
  store { double, double }* %15, { double, double }** %14
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i64 1)
  store { %String*, { double, double }*, { double, double }* }* %13, { %String*, { double, double }*, { double, double }* }** %namedValue
  br i1 %cond, label %then0__2, label %continue__2

then0__2:                                         ; preds = %condContinue__1
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i64 -1)
  %19 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %11, i1 false)
  %20 = icmp ne %Tuple* %11, %19
  %21 = bitcast %Tuple* %19 to { %String*, { double, double }*, { double, double }* }*
  %22 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %21, i32 0, i32 1
  %23 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 1.000000e+00, double 0.000000e+00)
  %24 = bitcast { double, double }* %23 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i64 1)
  %25 = load { double, double }*, { double, double }** %22
  %26 = bitcast { double, double }* %25 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %26, i64 -1)
  br i1 %20, label %condContinue__2, label %condFalse__2

condFalse__2:                                     ; preds = %then0__2
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i64 -1)
  br label %condContinue__2

condContinue__2:                                  ; preds = %condFalse__2, %then0__2
  store { double, double }* %23, { double, double }** %22
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %19, i64 1)
  store { %String*, { double, double }*, { double, double }* }* %21, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %19, i64 -1)
  br label %continue__2

continue__2:                                      ; preds = %condContinue__2, %condContinue__1
  %27 = load { %String*, { double, double }*, { double, double }* }*, { %String*, { double, double }*, { double, double }* }** %namedValue
  %28 = bitcast { %String*, { double, double }*, { double, double }* }* %27 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %28, i64 -1)
  %29 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %28, i1 false)
  %30 = icmp ne %Tuple* %28, %29
  %31 = bitcast %Tuple* %29 to { %String*, { double, double }*, { double, double }* }*
  %32 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %31, i32 0, i32 2
  %33 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %27, i32 0, i32 1
  %34 = load { double, double }*, { double, double }** %33
  %35 = bitcast { double, double }* %34 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %35, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %35, i64 1)
  %36 = load { double, double }*, { double, double }** %32
  %37 = bitcast { double, double }* %36 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %37, i64 -1)
  br i1 %30, label %condContinue__3, label %condFalse__3

condFalse__3:                                     ; preds = %continue__2
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %35, i64 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %37, i64 -1)
  br label %condContinue__3

condContinue__3:                                  ; preds = %condFalse__3, %continue__2
  store { double, double }* %34, { double, double }** %32
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %29, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %29, i64 1)
  store { %String*, { double, double }*, { double, double }* }* %31, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %16, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %28, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %37, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %29, i64 -1)
  br label %continue__1

continue__1:                                      ; preds = %condContinue__3, %entry
  %38 = load { %String*, { double, double }*, { double, double }* }*, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 -1)
  %39 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %38, i32 0, i32 1
  %40 = load { double, double }*, { double, double }** %39
  %41 = bitcast { double, double }* %40 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %41, i64 -1)
  %42 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %38, i32 0, i32 2
  %43 = load { double, double }*, { double, double }** %42
  %44 = bitcast { double, double }* %43 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %44, i64 -1)
  %45 = bitcast { %String*, { double, double }*, { double, double }* }* %38 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %45, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  ret { %String*, { double, double }*, { double, double }* }* %38
}
