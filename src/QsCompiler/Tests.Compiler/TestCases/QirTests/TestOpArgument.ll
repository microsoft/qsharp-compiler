
%Result = type opaque
%Range = type { i64, i64, i64 }
%TupleHeader = type { i32, i32 }
%Qubit = type opaque
%Array = type opaque
%Callable = type opaque
%String = type opaque

@ResultZero = external global %Result*
@ResultOne = external global %Result*
@PauliI = constant i2 0
@PauliX = constant i2 1
@PauliY = constant i2 -1
@PauliZ = constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }
@Microsoft__Quantum__Intrinsic__CNOT = constant [4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*] [void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* @Microsoft__Quantum__Intrinsic__CNOT__body__wrapper, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null]
@PartialApplication__1 = constant [4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*] [void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* @Lifted__PartialApplication__1__body__wrapper, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* @Lifted__PartialApplication__1__adj__wrapper, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null]

@Microsoft_Quantum_Testing_QIR_TestOpArgument = alias void (), void ()* @Microsoft__Quantum__Testing__QIR__TestOpArgument__body

declare void @Microsoft__Quantum__Intrinsic__H____body(%Qubit*)

define void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %qb) {
entry:
  call void @__quantum__qis__h__(%Qubit* %qb)
  ret void
}

declare void @__quantum__qis__h__(%Qubit*)

declare void @Microsoft__Quantum__Intrinsic__CNOT____body(%Qubit*, %Qubit*)

define void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %control, %Qubit* %target) {
entry:
  call void @__quantum__qis__cnot__(%Qubit* %control, %Qubit* %target)
  ret void
}

declare void @__quantum__qis__cnot__(%Qubit*, %Qubit*)

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

declare i64 @Microsoft__Quantum__Core__Length__body(%Array*)

declare %Range @Microsoft__Quantum__Core__RangeReverse__body(%Range)

define void @Microsoft__Quantum__Testing__QIR__TestOpArgument__body() #0 {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Callable*, %Qubit* }* getelementptr ({ %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %Callable*, %Qubit* }*
  %2 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %1, i64 0, i32 1
  %3 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Intrinsic__CNOT, %TupleHeader* null)
  store %Callable* %3, %Callable** %2
  %4 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %1, i64 0, i32 2
  store %Qubit* %q, %Qubit** %4
  %5 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @PartialApplication__1, %TupleHeader* %0)
  call void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %5)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  call void @__quantum__rt__callable_unreference(%Callable* %5)
  ret void
}

declare %Qubit* @__quantum__rt__qubit_allocate()

declare %Array* @__quantum__rt__qubit_allocate_array(i64)

define void @Microsoft__Quantum__Testing__QIR__Apply__body(%Callable* %op) {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__(%Qubit* %q)
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit* }* getelementptr ({ %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %Qubit* }*
  %2 = getelementptr { %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* %1, i64 0, i32 1
  store %Qubit* %q, %Qubit** %2
  call void @__quantum__rt__callable_invoke(%Callable* %op, %TupleHeader* %0, %TupleHeader* null)
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret void
}

declare %TupleHeader* @__quantum__rt__tuple_create(i64)

define void @Microsoft__Quantum__Intrinsic__CNOT__body__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Qubit*, %Qubit* }*
  %1 = getelementptr { %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* %0, i64 0, i32 1
  %2 = load %Qubit*, %Qubit** %1
  %3 = getelementptr { %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* %0, i64 0, i32 2
  %4 = load %Qubit*, %Qubit** %3
  call void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %2, %Qubit* %4)
  ret void
}

declare %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]*, %TupleHeader*)

define void @Lifted__PartialApplication__1__body__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %capture-tuple to { %TupleHeader, %Callable*, %Qubit* }*
  %1 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Qubit* }*
  %2 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit*, %Qubit* }* getelementptr ({ %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %3 = bitcast %TupleHeader* %2 to { %TupleHeader, %Qubit*, %Qubit* }*
  %4 = getelementptr { %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* %3, i64 0, i32 1
  %5 = getelementptr { %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* %1, i64 0, i32 1
  %6 = load %Qubit*, %Qubit** %5
  store %Qubit* %6, %Qubit** %4
  %7 = getelementptr { %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* %3, i64 0, i32 2
  %8 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %0, i64 0, i32 2
  %9 = load %Qubit*, %Qubit** %8
  store %Qubit* %9, %Qubit** %7
  %10 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %0, i64 0, i32 1
  %11 = load %Callable*, %Callable** %10
  call void @__quantum__rt__callable_invoke(%Callable* %11, %TupleHeader* %2, %TupleHeader* %result-tuple)
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %2)
  ret void
}

declare void @__quantum__rt__callable_invoke(%Callable*, %TupleHeader*, %TupleHeader*)

declare void @__quantum__rt__tuple_unreference(%TupleHeader*)

define void @Lifted__PartialApplication__1__adj__wrapper(%TupleHeader* %capture-tuple, %TupleHeader* %arg-tuple, %TupleHeader* %result-tuple) {
entry:
  %0 = bitcast %TupleHeader* %capture-tuple to { %TupleHeader, %Callable*, %Qubit* }*
  %1 = bitcast %TupleHeader* %arg-tuple to { %TupleHeader, %Qubit* }*
  %2 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %Qubit*, %Qubit* }* getelementptr ({ %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* null, i32 1) to i64))
  %3 = bitcast %TupleHeader* %2 to { %TupleHeader, %Qubit*, %Qubit* }*
  %4 = getelementptr { %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* %3, i64 0, i32 1
  %5 = getelementptr { %TupleHeader, %Qubit* }, { %TupleHeader, %Qubit* }* %1, i64 0, i32 1
  %6 = load %Qubit*, %Qubit** %5
  store %Qubit* %6, %Qubit** %4
  %7 = getelementptr { %TupleHeader, %Qubit*, %Qubit* }, { %TupleHeader, %Qubit*, %Qubit* }* %3, i64 0, i32 2
  %8 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %0, i64 0, i32 2
  %9 = load %Qubit*, %Qubit** %8
  store %Qubit* %9, %Qubit** %7
  %10 = getelementptr { %TupleHeader, %Callable*, %Qubit* }, { %TupleHeader, %Callable*, %Qubit* }* %0, i64 0, i32 1
  %11 = load %Callable*, %Callable** %10
  %12 = call %Callable* @__quantum__rt__callable_copy(%Callable* %11)
  call void @__quantum__rt__callable_make_adjoint(%Callable* %12)
  call void @__quantum__rt__callable_invoke(%Callable* %12, %TupleHeader* %2, %TupleHeader* %result-tuple)
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %2)
  call void @__quantum__rt__callable_unreference(%Callable* %12)
  ret void
}

declare %Callable* @__quantum__rt__callable_copy(%Callable*)

declare void @__quantum__rt__callable_make_adjoint(%Callable*)

declare void @__quantum__rt__callable_unreference(%Callable*)

declare void @__quantum__rt__qubit_release(%Qubit*)

define { %TupleHeader, %String* }* @Microsoft__Quantum__Targeting__TargetInstruction__body(%String* %arg0) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, %String* }* getelementptr ({ %TupleHeader, %String* }, { %TupleHeader, %String* }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, %String* }*
  %2 = getelementptr { %TupleHeader, %String* }, { %TupleHeader, %String* }* %1, i64 0, i32 1
  store %String* %arg0, %String** %2
  call void @__quantum__rt__string_reference(%String* %arg0)
  ret { %TupleHeader, %String* }* %1
}

declare void @__quantum__rt__string_reference(%String*)

attributes #0 = { "EntryPoint" }
