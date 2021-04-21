define { double, %String* }* @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
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
  %6 = bitcast %Tuple* %5 to { double, %String* }*
  %7 = getelementptr inbounds { double, %String* }, { double, %String* }* %6, i32 0, i32 1
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 1)
  %8 = load %String*, %String** %7, align 8
  store %String* %name, %String** %7, align 8
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 1)
  store { double, %String* }* %6, { double, %String* }** %res, align 8
  %energy = alloca double, align 8
  store double 0.000000e+00, double* %energy, align 8
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %9 = phi i64 [ 0, %entry ], [ %11, %exiting__1 ]
  %10 = icmp sle i64 %9, 10
  br i1 %10, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %11 = add i64 %9, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %12 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %5, i1 false)
  %13 = bitcast %Tuple* %12 to { double, %String* }*
  %14 = getelementptr inbounds { double, %String* }, { double, %String* }* %13, i32 0, i32 0
  %15 = load double, double* %energy, align 8
  store double %15, double* %14, align 8
  %16 = getelementptr inbounds { double, %String* }, { double, %String* }* %13, i32 0, i32 1
  %17 = load %String*, %String** %16, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %17, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %4, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1)
  ret { double, %String* }* %13

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %23, %exiting__2 ]
  %18 = icmp sle i64 %j, 0
  %19 = icmp sge i64 %j, 0
  %20 = select i1 false, i1 %18, i1 %19
  br i1 %20, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %21 = load double, double* %energy, align 8
  %22 = fadd double %21, 5.000000e-01
  store double %22, double* %energy, align 8
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %23 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
