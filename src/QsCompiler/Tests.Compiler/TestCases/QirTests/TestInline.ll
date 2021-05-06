define internal { double, double }* @Microsoft__Quantum__Testing__QIR__TestInline__body() {
entry:
  %x = alloca double, align 8
  store double 0.000000e+00, double* %x, align 8
  %y = alloca double, align 8
  store double 0.000000e+00, double* %y, align 8
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call { double, %Qubit* }* @Microsoft__Quantum__Testing__QIR__AsTuple__body(%Qubit* %q)
  %1 = getelementptr inbounds { double, %Qubit* }, { double, %Qubit* }* %0, i32 0, i32 0
  %theta = load double, double* %1, align 8
  %2 = getelementptr inbounds { double, %Qubit* }, { double, %Qubit* }* %0, i32 0, i32 1
  %qb = load %Qubit*, %Qubit** %2, align 8
  call void @__quantum__qis__k__body(double %theta, %Qubit* %qb)
  %3 = bitcast { double, %Qubit* }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  %4 = call { double, %Qubit* }* @Microsoft__Quantum__Testing__QIR__AsTuple__body(%Qubit* %q)
  %5 = getelementptr inbounds { double, %Qubit* }, { double, %Qubit* }* %4, i32 0, i32 0
  %theta__2 = load double, double* %5, align 8
  %6 = getelementptr inbounds { double, %Qubit* }, { double, %Qubit* }* %4, i32 0, i32 1
  %qb__2 = load %Qubit*, %Qubit** %6, align 8
  call void @__quantum__qis__k__body(double %theta__2, %Qubit* %qb__2)
  %7 = bitcast { double, %Qubit* }* %4 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %7, i32 -1)
  %8 = call { double, %Qubit* }* @Microsoft__Quantum__Testing__QIR__AsTuple__body(%Qubit* %q)
  %9 = getelementptr inbounds { double, %Qubit* }, { double, %Qubit* }* %8, i32 0, i32 0
  %theta__4 = load double, double* %9, align 8
  %10 = getelementptr inbounds { double, %Qubit* }, { double, %Qubit* }* %8, i32 0, i32 1
  %qb__4 = load %Qubit*, %Qubit** %10, align 8
  call void @__quantum__qis__k__body(double %theta__4, %Qubit* %qb__4)
  %11 = bitcast { double, %Qubit* }* %8 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %11, i32 -1)
  %12 = call %Result* @__quantum__qis__mz(%Qubit* %q)
  %13 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %14 = bitcast %Tuple* %13 to { double, double }*
  %15 = getelementptr inbounds { double, double }, { double, double }* %14, i32 0, i32 0
  %16 = getelementptr inbounds { double, double }, { double, double }* %14, i32 0, i32 1
  store double 0.000000e+00, double* %15, align 8
  store double 0.000000e+00, double* %16, align 8
  %17 = call { double, double }* @Microsoft__Quantum__Testing__QIR__UpdatedValues__body(%Result* %12, { double, double }* %14)
  %18 = getelementptr inbounds { double, double }, { double, double }* %17, i32 0, i32 0
  %19 = load double, double* %18, align 8
  store double %19, double* %x, align 8
  %20 = getelementptr inbounds { double, double }, { double, double }* %17, i32 0, i32 1
  %21 = load double, double* %20, align 8
  store double %21, double* %y, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %12, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %13, i32 -1)
  %22 = bitcast { double, double }* %17 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  %23 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (double* getelementptr (double, double* null, i32 1) to i64), i64 2))
  %24 = bitcast %Tuple* %23 to { double, double }*
  %25 = getelementptr inbounds { double, double }, { double, double }* %24, i32 0, i32 0
  %26 = getelementptr inbounds { double, double }, { double, double }* %24, i32 0, i32 1
  store double %19, double* %25, align 8
  store double %21, double* %26, align 8
  ret { double, double }* %24
}
