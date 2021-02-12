define { double, %String* }* @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
entry:
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

condFalse__1:                                     ; preds = %entry
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %entry
  store %String* %name, %String** %8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 1)
  store { double, %String* }* %7, { double, %String* }** %res
  %energy = alloca double
  store double 0.000000e+00, double* %energy
  br label %header__1

header__1:                                        ; preds = %exiting__1, %condContinue__1
  %i = phi i64 [ 0, %condContinue__1 ], [ %11, %exiting__1 ]
  %10 = icmp sle i64 %i, 10
  br i1 %10, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %11 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %12 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %5, i1 false)
  %13 = icmp ne %Tuple* %5, %12
  %14 = bitcast %Tuple* %12 to { double, %String* }*
  %15 = getelementptr inbounds { double, %String* }, { double, %String* }* %14, i32 0, i32 0
  %16 = load double, double* %energy
  store double %16, double* %15
  %17 = getelementptr inbounds { double, %String* }, { double, %String* }* %14, i32 0, i32 1
  %18 = load %String*, %String** %17
  call void @__quantum__rt__string_update_reference_count(%String* %18, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %4, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  ret { double, %String* }* %14

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %24, %exiting__2 ]
  %19 = icmp sle i64 %j, 0
  %20 = icmp sge i64 %j, 0
  %21 = select i1 false, i1 %19, i1 %20
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = load double, double* %energy
  %23 = fadd double %22, 5.000000e-01
  store double %23, double* %energy
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %24 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
