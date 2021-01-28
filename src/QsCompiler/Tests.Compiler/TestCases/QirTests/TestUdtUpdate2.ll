define { %String*, { double, double }*, { double, double }* }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate2__body(i1 %cond, { %String*, { double, double }*, { double, double }* }* %arg) {
entry:
  %0 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i64 0, i32 1
  %1 = load { double, double }*, { double, double }** %0
  %2 = bitcast { double, double }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 1)
  %3 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i64 0, i32 2
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
  %7 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i64 0, i32 0
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
  %12 = bitcast %Tuple* %11 to { %String*, { double, double }*, { double, double }* }*
  %13 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %12, i64 0, i32 1
  %14 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 0.000000e+00, double 0.000000e+00)
  %15 = bitcast { double, double }* %14 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %15, i64 1)
  %16 = load { double, double }*, { double, double }** %13
  %17 = bitcast { double, double }* %16 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %17, i64 -1)
  store { double, double }* %14, { double, double }** %13
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i64 1)
  store { %String*, { double, double }*, { double, double }* }* %12, { %String*, { double, double }*, { double, double }* }** %namedValue
  br i1 %cond, label %then0__2, label %continue__2

then0__2:                                         ; preds = %then0__1
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i64 -1)
  %18 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %11, i1 false)
  %19 = bitcast %Tuple* %18 to { %String*, { double, double }*, { double, double }* }*
  %20 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %19, i64 0, i32 1
  %21 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 1.000000e+00, double 0.000000e+00)
  %22 = bitcast { double, double }* %21 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %22, i64 1)
  %23 = load { double, double }*, { double, double }** %20
  %24 = bitcast { double, double }* %23 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i64 -1)
  store { double, double }* %21, { double, double }** %20
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i64 1)
  store { %String*, { double, double }*, { double, double }* }* %19, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i64 -1)
  br label %continue__2

continue__2:                                      ; preds = %then0__2, %then0__1
  %25 = load { %String*, { double, double }*, { double, double }* }*, { %String*, { double, double }*, { double, double }* }** %namedValue
  %26 = bitcast { %String*, { double, double }*, { double, double }* }* %25 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %26, i64 -1)
  %27 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %26, i1 false)
  %28 = bitcast %Tuple* %27 to { %String*, { double, double }*, { double, double }* }*
  %29 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %28, i64 0, i32 2
  %30 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %25, i64 0, i32 1
  %31 = load { double, double }*, { double, double }** %30
  %32 = bitcast { double, double }* %31 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %32, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %32, i64 1)
  %33 = load { double, double }*, { double, double }** %29
  %34 = bitcast { double, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %34, i64 -1)
  store { double, double }* %31, { double, double }** %29
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %27, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %27, i64 1)
  store { %String*, { double, double }*, { double, double }* }* %28, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %34, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %27, i64 -1)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %entry
  %35 = load { %String*, { double, double }*, { double, double }* }*, { %String*, { double, double }*, { double, double }* }** %namedValue
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 -1)
  %36 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %35, i64 0, i32 1
  %37 = load { double, double }*, { double, double }** %36
  %38 = bitcast { double, double }* %37 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %38, i64 -1)
  %39 = getelementptr { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %35, i64 0, i32 2
  %40 = load { double, double }*, { double, double }** %39
  %41 = bitcast { double, double }* %40 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %41, i64 -1)
  %42 = bitcast { %String*, { double, double }*, { double, double }* }* %35 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %42, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  ret { %String*, { double, double }*, { double, double }* }* %35
}
