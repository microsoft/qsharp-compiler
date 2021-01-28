define { double, %String* }* @Microsoft__Quantum__Testing__QIR__TestNestedLoops__body() {
entry:
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
  %energy = alloca double
  store double 0.000000e+00, double* %energy
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %10, %exiting__1 ]
  %9 = icmp sle i64 %i, 10
  br i1 %9, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %preheader__1

exiting__1:                                       ; preds = %exit__2
  %10 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %11 = call %Tuple* @__quantum__rt__tuple_copy(%Tuple* %5, i1 false)
  %12 = bitcast %Tuple* %11 to { double, %String* }*
  %13 = getelementptr { double, %String* }, { double, %String* }* %12, i64 0, i32 0
  %14 = load double, double* %energy
  store double %14, double* %13
  %15 = getelementptr { double, %String* }, { double, %String* }* %12, i64 0, i32 1
  %16 = load %String*, %String** %15
  call void @__quantum__rt__string_update_reference_count(%String* %16, i64 1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %4, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %8, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %name, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i64 -1)
  ret { double, %String* }* %12

preheader__1:                                     ; preds = %body__1
  br label %header__2

header__2:                                        ; preds = %exiting__2, %preheader__1
  %j = phi i64 [ 5, %preheader__1 ], [ %22, %exiting__2 ]
  %17 = icmp sle i64 %j, 0
  %18 = icmp sge i64 %j, 0
  %19 = select i1 false, i1 %17, i1 %18
  br i1 %19, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %20 = load double, double* %energy
  %21 = fadd double %20, 5.000000e-01
  store double %21, double* %energy
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %22 = add i64 %j, -1
  br label %header__2

exit__2:                                          ; preds = %header__2
  br label %exiting__1
}
