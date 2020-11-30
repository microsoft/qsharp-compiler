
%Result = type opaque
%Range = type { i64, i64, i64 }
%Qubit = type opaque
%Array = type opaque
%TupleHeader = type { i32 }
%String = type opaque
%"struct.quantum::Array" = type opaque

@ResultZero = external global %Result*
@ResultOne = external global %Result*
@PauliI = constant i2 0
@PauliX = constant i2 1
@PauliY = constant i2 -1
@PauliZ = constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }

declare double @__quantum__qis__intAsDouble(i64)

declare void @__quantum__qis__cnot(%Qubit*, %Qubit*)

declare void @__quantum__qis__h(%Qubit*)

declare %Result* @__quantum__qis__mz(%Qubit*)

declare %Result* @__quantum__qis__measure(%Array*, %Array*)

declare void @__quantum__qis__rx(double, %Qubit*)

declare void @__quantum__qis__rz(double, %Qubit*)

declare void @__quantum__qis__s(%Qubit*)

declare void @__quantum__qis__x(%Qubit*)

declare void @__quantum__qis__z(%Qubit*)

define %TupleHeader* @Microsoft__Quantum__Core__Attribute__body() {
entry:
  ret %TupleHeader* null
}

define %TupleHeader* @Microsoft__Quantum__Core__EntryPoint__body() {
entry:
  ret %TupleHeader* null
}

define %TupleHeader* @Microsoft__Quantum__Core__Inline__body() {
entry:
  ret %TupleHeader* null
}

define { %TupleHeader, %String* }* @Microsoft__Quantum__Core__Intrinsic__body(%String* %arg0) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %String* }* getelementptr ({ %TupleHeader, %String* }, { %TupleHeader, %String* }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %String* }*
  %2 = getelementptr inbounds { %TupleHeader, %String* }, { %TupleHeader, %String* }* %1, i32 0, i32 1
  store %String* %arg0, %String** %2
  call void @__quantum__rt__string_reference(%String* %arg0)
  ret { %TupleHeader, %String* }* %1
}

declare %TupleHeader* @__quantum__rt__tuple_create(i64)

declare void @__quantum__rt__string_reference(%String*)

declare i64 @Microsoft__Quantum__Core__Length__body(%Array*)

declare i64 @Microsoft__Quantum__Core__RangeEnd__body(%Range)

declare %Range @Microsoft__Quantum__Core__RangeReverse__body(%Range)

declare i64 @Microsoft__Quantum__Core__RangeStart__body(%Range)

declare i64 @Microsoft__Quantum__Core__RangeStep__body(%Range)

define void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %control, %Qubit* %target) {
entry:
  call void @__quantum__qis__cnot(%Qubit* %control, %Qubit* %target)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %qb) {
entry:
  call void @__quantum__qis__h(%Qubit* %qb)
  ret void
}

define double @Microsoft__Quantum__Intrinsic__IntAsDouble__body(i64 %i) {
entry:
  %0 = call double @__quantum__qis__intAsDouble(i64 %i)
  ret double %0
}

define %Result* @Microsoft__Quantum__Intrinsic__Measure__body(%Array* %bases, %Array* %qubits) {
entry:
  %0 = call %Result* @__quantum__qis__measure(%Array* %bases, %Array* %qubits)
  ret %Result* %0
}

define %Result* @Microsoft__Quantum__Intrinsic__Mz__body(%Qubit* %qb) {
entry:
  %0 = call %Result* @__quantum__qis__mz(%Qubit* %qb)
  ret %Result* %0
}

define void @Microsoft__Quantum__Intrinsic__Rx__body(double %theta, %Qubit* %qb) {
entry:
  call void @__quantum__qis__rx(double %theta, %Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rx__adj(double %theta, %Qubit* %qb) {
entry:
  %0 = fsub double -0.000000e+00, %theta
  call void @__quantum__qis__rx(double %0, %Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__body(double %theta, %Qubit* %qb) {
entry:
  call void @__quantum__qis__rz(double %theta, %Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Rz__adj(double %theta, %Qubit* %qb) {
entry:
  %0 = fsub double -0.000000e+00, %theta
  call void @__quantum__qis__rz(double %0, %Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__S__body(%Qubit* %qb) {
entry:
  call void @__quantum__qis__s(%Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__S__adj(%Qubit* %qb) {
entry:
  call void @__quantum__qis__s(%Qubit* %qb)
  call void @__quantum__qis__z(%Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__X__body(%Qubit* %qb) {
entry:
  call void @__quantum__qis__x(%Qubit* %qb)
  ret void
}

define void @Microsoft__Quantum__Intrinsic__Z__body(%Qubit* %qb) {
entry:
  call void @__quantum__qis__z(%Qubit* %qb)
  ret void
}

define %Result* @Qrng__RandomBit__body() {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %.bases = call %Array* @__quantum__rt__array_create_1d(i32 1, i64 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %.bases, i64 0)
  %1 = load i2, i2* @PauliX
  %2 = bitcast i8* %0 to i2*
  store i2 %1, i2* %2
  %.qubits = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %.qubits, i64 0)
  %4 = bitcast i8* %3 to %Qubit**
  store %Qubit* %q, %Qubit** %4
  %rslt = call %Result* @__quantum__qis__measure(%Array* %.bases, %Array* %.qubits)
  %.bases1 = call %Array* @__quantum__rt__array_create_1d(i32 1, i64 1)
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %.bases1, i64 0)
  %6 = load i2, i2* @PauliZ
  %7 = bitcast i8* %5 to i2*
  store i2 %6, i2* %7
  %.qubits2 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 1)
  %8 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %.qubits2, i64 0)
  %9 = bitcast i8* %8 to %Qubit**
  store %Qubit* %q, %Qubit** %9
  %10 = call %Result* @__quantum__qis__measure(%Array* %.bases1, %Array* %.qubits2)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__array_unreference(%Array* %.bases)
  call void @__quantum__rt__array_unreference(%Array* %.qubits)
  call void @__quantum__rt__array_unreference(%Array* %.bases1)
  call void @__quantum__rt__array_unreference(%Array* %.qubits2)
  ret %Result* %10
}

declare %Qubit* @__quantum__rt__qubit_allocate()

declare %Array* @__quantum__rt__qubit_allocate_array(i64)

declare %Array* @__quantum__rt__array_create_1d(i32, i64)

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64)

declare void @__quantum__rt__qubit_release(%Qubit*)

declare void @__quantum__rt__array_unreference(%Array*)

define i64 @Qrng__RandomInt__body() {
entry:
  %rslt = alloca i64
  store i64 0, i64* %rslt
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %8, %exiting__1 ]
  %0 = icmp sge i64 %i, 31
  %1 = icmp sle i64 %i, 31
  %2 = select i1 true, i1 %1, i1 %0
  br i1 %2, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %oneBit = call %Result* @Qrng__RandomBit__body()
  %3 = load %Result*, %Result** @ResultOne
  %4 = call i1 @__quantum__rt__result_equal(%Result* %oneBit, %Result* %3)
  br i1 %4, label %then0__1, label %continue__1

then0__1:                                         ; preds = %body__1
  %5 = load i64, i64* %rslt
  %6 = shl i64 1, %i
  %7 = add i64 %5, %6
  store i64 %7, i64* %rslt
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %body__1
  call void @__quantum__rt__result_unreference(%Result* %oneBit)
  br label %exiting__1

exiting__1:                                       ; preds = %continue__1
  %8 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %9 = load i64, i64* %rslt
  ret i64 %9
}

declare i1 @__quantum__rt__result_equal(%Result*, %Result*)

declare void @__quantum__rt__result_unreference(%Result*)

define %Array* @Qrng__RandomInts__body() {
entry:
  %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 32)
  %rslts = alloca %Array*
  store %Array* %0, %Array** %rslts
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %i = phi i64 [ 0, %preheader__1 ], [ %9, %exiting__1 ]
  %1 = icmp sge i64 %i, 31
  %2 = icmp sle i64 %i, 31
  %3 = select i1 true, i1 %2, i1 %1
  br i1 %3, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %4 = load %Array*, %Array** %rslts
  %5 = call %Array* @__quantum__rt__array_copy(%Array* %4)
  %6 = call i64 @Qrng__RandomInt__body()
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %5, i64 %i)
  %8 = bitcast i8* %7 to i64*
  store i64 %6, i64* %8
  store %Array* %5, %Array** %rslts
  call void @__quantum__rt__array_reference(%Array* %5)
  call void @__quantum__rt__array_unreference(%Array* %5)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %9 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %10 = load %Array*, %Array** %rslts
  call void @__quantum__rt__array_unreference(%Array* %0)
  ret %Array* %10
}

declare %Array* @__quantum__rt__array_copy(%Array*)

declare void @__quantum__rt__array_reference(%Array*)

declare double @Microsoft__Quantum__Instructions__IntAsDoubleImpl__body(i64)

declare void @Microsoft__Quantum__Instructions__PhysCNOT__body(%Qubit*, %Qubit*)

declare void @Microsoft__Quantum__Instructions__PhysH__body(%Qubit*)

declare %Result* @Microsoft__Quantum__Instructions__PhysM__body(%Qubit*)

declare %Result* @Microsoft__Quantum__Instructions__PhysMeasure__body(%Array*, %Array*)

declare void @Microsoft__Quantum__Instructions__PhysRx__body(double, %Qubit*)

declare void @Microsoft__Quantum__Instructions__PhysRz__body(double, %Qubit*)

declare void @Microsoft__Quantum__Instructions__PhysS__body(%Qubit*)

declare void @Microsoft__Quantum__Instructions__PhysX__body(%Qubit*)

declare void @Microsoft__Quantum__Instructions__PhysZ__body(%Qubit*)

define %"struct.quantum::Array"* @Qrng_RandomInts() #0 {
entry:
  %0 = call %Array* @Qrng__RandomInts__body()
  %1 = bitcast %Array* %0 to %"struct.quantum::Array"*
  ret %"struct.quantum::Array"* %1
}

attributes #0 = { "EntryPoint" }
