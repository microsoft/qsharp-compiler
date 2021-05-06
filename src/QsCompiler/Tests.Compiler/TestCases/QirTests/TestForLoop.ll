define internal { double, %String* }* @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
entry:
  %name = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @0, i32 0, i32 0))
  %0 = call %String* @__quantum__rt__string_create(i8* null)
  %1 = call { double, %String* }* @Microsoft__Quantum__Testing__QIR__Energy__body(double 0.000000e+00, %String* %0)
  %res = alloca { double, %String* }*, align 8
  store { double, %String* }* %1, { double, %String* }** %res, align 8
  %2 = bitcast { double, %String* }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 1)
  %3 = getelementptr inbounds { double, %String* }, { double, %String* }* %1, i32 0, i32 1
  %4 = load %String*, %String** %3, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %4, i32 1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i32 -1)
  %5 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %2, i1 false)
  %6 = icmp ne %Tuple* %2, %5
  %7 = bitcast %Tuple* %5 to { double, %String* }*
  %8 = getelementptr inbounds { double, %String* }, { double, %String* }* %7, i32 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 1)
  %9 = load %String*, %String** %8, align 8
  br i1 %6, label %condContinue__1, label %condFalse__1

condFalse__1:                                     ; preds = %entry
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i32 -1)
  br label %condContinue__1

condContinue__1:                                  ; preds = %condFalse__1, %entry
  store %String* %name, %String** %8, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 1)
  store { double, %String* }* %7, { double, %String* }** %res, align 8
  %energy = alloca double, align 8
  store double 0.000000e+00, double* %energy, align 8
  br label %header__1

header__1:                                        ; preds = %exiting__1, %condContinue__1
  %10 = phi i64 [ 0, %condContinue__1 ], [ %12, %exiting__1 ]
  %11 = icmp sle i64 %10, 10
  br i1 %11, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %12 = add i64 %10, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %13 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %5, i1 false)
  %14 = icmp ne %Tuple* %5, %13
  %15 = bitcast %Tuple* %13 to { double, %String* }*
  %16 = getelementptr inbounds { double, %String* }, { double, %String* }* %15, i32 0, i32 0
  %17 = load double, double* %energy, align 8
  store double %17, double* %16, align 8
  %18 = getelementptr inbounds { double, %String* }, { double, %String* }* %15, i32 0, i32 1
  %19 = load %String*, %String** %18, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %19, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %4, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %9, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  ret { double, %String* }* %15

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %25, %exiting__2 ]
  %20 = icmp sle i64 %j, 0
  %21 = icmp sge i64 %j, 0
  %22 = select i1 false, i1 %20, i1 %21
  br i1 %22, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %23 = load double, double* %energy, align 8
  %24 = fadd double %23, 5.000000e-01
  store double %24, double* %energy, align 8
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %25 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
