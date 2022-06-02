define internal { double, %String* }* @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
entry:
  %energy = alloca double, align 8
  %res = alloca { double, %String* }*, align 8
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
  store double 0.000000e+00, double* %energy, align 8
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %7 = phi i64 [ 0, %entry ], [ %9, %exiting__1 ]
  %8 = icmp sle i64 %7, 10
  br i1 %8, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %9 = add i64 %7, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %10 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %3, i1 false)
  %11 = bitcast %Tuple* %10 to { double, %String* }*
  %12 = getelementptr inbounds { double, %String* }, { double, %String* }* %11, i32 0, i32 0
  %13 = load double, double* %energy, align 8
  store double %13, double* %12, align 8
  %14 = getelementptr inbounds { double, %String* }, { double, %String* }* %11, i32 0, i32 1
  %15 = load %String*, %String** %14, align 8
  call void @__quantum__rt__string_update_reference_count(%String* %15, i32 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %3, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  ret { double, %String* }* %11

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %21, %exiting__2 ]
  %16 = icmp sle i64 %j, 0
  %17 = icmp sge i64 %j, 0
  %18 = select i1 false, i1 %16, i1 %17
  br i1 %18, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %19 = load double, double* %energy, align 8
  %20 = fadd double %19, 5.000000e-01
  store double %20, double* %energy, align 8
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %21 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
