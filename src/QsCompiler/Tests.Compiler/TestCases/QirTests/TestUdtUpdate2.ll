define { %String*, { double, double }*, { double, double }* }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate2__body(i1 %cond, { %String*, { double, double }*, { double, double }* }* %arg) {
entry:
  %0 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i32 0, i32 1
  %1 = load { double, double }*, { double, double }** %0, align 8
  %2 = bitcast { double, double }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 1)
  %3 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i32 0, i32 2
  %4 = load { double, double }*, { double, double }** %3, align 8
  %5 = bitcast { double, double }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 1)
  %6 = bitcast { %String*, { double, double }*, { double, double }* }* %arg to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 1)
  %namedValue = alloca { %String*, { double, double }*, { double, double }* }*, align 8
  store { %String*, { double, double }*, { double, double }* }* %arg, { %String*, { double, double }*, { double, double }* }** %namedValue, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 1)
  %7 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %arg, i32 0, i32 0
  %8 = load %String*, %String** %7, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %8, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 1)
  %9 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i32 0, i32 0))
  %10 = call i1 @__quantum__rt__string_equal(%String* %8, %String* %9)
  br i1 %10, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  %11 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %6, i1 false)
  %12 = bitcast %Tuple* %11 to { %String*, { double, double }*, { double, double }* }*
  %13 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %12, i32 0, i32 1
  %14 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 0.000000e+00, double 0.000000e+00)
  %15 = bitcast { double, double }* %14 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %15, i32 1)
  %16 = load { double, double }*, { double, double }** %13, align 8
  %17 = bitcast { double, double }* %16 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %17, i32 -1)
  store { double, double }* %14, { double, double }** %13, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i32 1)
  store { %String*, { double, double }*, { double, double }* }* %12, { %String*, { double, double }*, { double, double }* }** %namedValue, align 8
  br i1 %cond, label %then0__2, label %continue__2

then0__2:                                         ; preds = %then0__1
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %11, i32 -1)
  %18 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %11, i1 false)
  %19 = bitcast %Tuple* %18 to { %String*, { double, double }*, { double, double }* }*
  %20 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %19, i32 0, i32 1
  %21 = call { double, double }* @Microsoft__Quantum__Testing__QIR__Complex__body(double 1.000000e+00, double 0.000000e+00)
  %22 = bitcast { double, double }* %21 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %22, i32 1)
  %23 = load { double, double }*, { double, double }** %20, align 8
  %24 = bitcast { double, double }* %23 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %24, i32 -1)
  store { double, double }* %21, { double, double }** %20, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i32 1)
  store { %String*, { double, double }*, { double, double }* }* %19, { %String*, { double, double }*, { double, double }* }** %namedValue, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %24, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i32 -1)
  br label %continue__2

continue__2:                                      ; preds = %then0__2, %then0__1
  %25 = load { %String*, { double, double }*, { double, double }* }*, { %String*, { double, double }*, { double, double }* }** %namedValue, align 8
  %26 = bitcast { %String*, { double, double }*, { double, double }* }* %25 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %26, i32 -1)
  %27 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %26, i1 false)
  %28 = bitcast %Tuple* %27 to { %String*, { double, double }*, { double, double }* }*
  %29 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %28, i32 0, i32 2
  %30 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %25, i32 0, i32 1
  %31 = load { double, double }*, { double, double }** %30, align 8
  %32 = bitcast { double, double }* %31 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %32, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %32, i32 1)
  %33 = load { double, double }*, { double, double }** %29, align 8
  %34 = bitcast { double, double }* %33 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %34, i32 -1)
  store { double, double }* %31, { double, double }** %29, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %27, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %27, i32 1)
  store { %String*, { double, double }*, { double, double }* }* %28, { %String*, { double, double }*, { double, double }* }** %namedValue, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %15, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %17, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %26, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %34, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %27, i32 -1)
  br label %continue__1

continue__1:                                      ; preds = %continue__2, %entry
  %35 = load { %String*, { double, double }*, { double, double }* }*, { %String*, { double, double }*, { double, double }* }** %namedValue, align 8
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i32 -1)
  %36 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %35, i32 0, i32 1
  %37 = load { double, double }*, { double, double }** %36, align 8
  %38 = bitcast { double, double }* %37 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %38, i32 -1)
  %39 = getelementptr inbounds { %String*, { double, double }*, { double, double }* }, { %String*, { double, double }*, { double, double }* }* %35, i32 0, i32 2
  %40 = load { double, double }*, { double, double }** %39, align 8
  %41 = bitcast { double, double }* %40 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %41, i32 -1)
  %42 = bitcast { %String*, { double, double }*, { double, double }* }* %35 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %42, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i32 -1)
  ret { %String*, { double, double }*, { double, double }* }* %35
}
