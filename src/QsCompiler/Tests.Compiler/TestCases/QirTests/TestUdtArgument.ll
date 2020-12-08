
%Result = type opaque
%Range = type { i64, i64, i64 }
%TupleHeader = type { i32, i32 }
%Callable = type opaque
%Array = type opaque
%String = type opaque

@ResultZero = external global %Result*
@ResultOne = external global %Result*
@PauliI = constant i2 0
@PauliX = constant i2 1
@PauliY = constant i2 -1
@PauliZ = constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }
@Microsoft__Quantum__Testing__QIR__TestType1 = constant [4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*] [void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* @Microsoft__Quantum__Testing__QIR__TestType1__body__wrapper, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null, void (%TupleHeader*, %TupleHeader*, %TupleHeader*)* null]

@Microsoft_Quantum_Testing_QIR_TestUdtArgument = alias { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* (), { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* ()* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body

define { %TupleHeader, i64 }* @Microsoft__Quantum__Testing__QIR__TestType1__body(i64 %arg0) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i64 }* getelementptr ({ %TupleHeader, i64 }, { %TupleHeader, i64 }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, i64 }*
  %2 = getelementptr { %TupleHeader, i64 }, { %TupleHeader, i64 }* %1, i64 0, i32 1
  store i64 %arg0, i64* %2
  ret { %TupleHeader, i64 }* %1
}

declare %TupleHeader* @__quantum__rt__tuple_create(i64)

define { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType2__body({ %TupleHeader, i2, i64 }* %arg0, double %arg1) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, { %TupleHeader, i2, i64 }*, double }* getelementptr ({ %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, { %TupleHeader, i2, i64 }*, double }*
  %2 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1, i64 0, i32 1
  store { %TupleHeader, i2, i64 }* %arg0, { %TupleHeader, i2, i64 }** %2
  %3 = bitcast { %TupleHeader, i2, i64 }* %arg0 to %TupleHeader*
  call void @__quantum__rt__tuple_reference(%TupleHeader* %3)
  %4 = getelementptr { %TupleHeader, { %TupleHeader, i2, i64 }*, double }, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1, i64 0, i32 2
  store double %arg1, double* %4
  ret { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %1
}

declare void @__quantum__rt__tuple_reference(%TupleHeader*)

define { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* @Microsoft__Quantum__Testing__QIR__TestUdtArgument__body() #0 {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]* @Microsoft__Quantum__Testing__QIR__TestType1, %TupleHeader* null)
  %udt1 = call i64 @Microsoft__Quantum__Testing__QIR__Build__body(%Callable* %0)
  %1 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i2, i64 }* getelementptr ({ %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* null, i32 1) to i64))
  %2 = bitcast %TupleHeader* %1 to { %TupleHeader, i2, i64 }*
  %3 = load i2, i2* @PauliX
  %4 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %2, i64 0, i32 1
  store i2 %3, i2* %4
  %5 = getelementptr { %TupleHeader, i2, i64 }, { %TupleHeader, i2, i64 }* %2, i64 0, i32 2
  store i64 1, i64* %5
  %udt2 = call { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* @Microsoft__Quantum__Testing__QIR__TestType2__body({ %TupleHeader, i2, i64 }* %2, double 2.000000e+00)
  %6 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* getelementptr ({ %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }, { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* null, i32 1) to i64))
  %7 = bitcast %TupleHeader* %6 to { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }*
  %8 = getelementptr { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }, { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* %7, i64 0, i32 1
  store i64 %udt1, i64* %8
  %9 = getelementptr { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }, { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* %7, i64 0, i32 2
  store { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* %udt2, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }** %9
  call void @__quantum__rt__callable_unreference(%Callable* %0)
  %10 = bitcast { %TupleHeader, i2, i64 }* %2 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %10)
  ret { %TupleHeader, i64, { %TupleHeader, { %TupleHeader, i2, i64 }*, double }* }* %7
}

define i64 @Microsoft__Quantum__Testing__QIR__Build__body(%Callable* %build) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i64 }* getelementptr ({ %TupleHeader, i64 }, { %TupleHeader, i64 }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, i64 }*
  %2 = getelementptr { %TupleHeader, i64 }, { %TupleHeader, i64 }* %1, i64 0, i32 1
  store i64 1, i64* %2
  %3 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, i64 }* getelementptr ({ %TupleHeader, i64 }, { %TupleHeader, i64 }* null, i32 1) to i64))
  %4 = bitcast %TupleHeader* %3 to { %TupleHeader, i64 }*
  call void @__quantum__rt__callable_invoke(%Callable* %build, %TupleHeader* %0, %TupleHeader* %3)
  %5 = getelementptr { %TupleHeader, i64 }, { %TupleHeader, i64 }* %4, i64 0, i32 1
  %6 = load i64, i64* %5
  ret i64 %6
}

declare void @Microsoft__Quantum__Testing__QIR__TestType1__body__wrapper(%TupleHeader*, %TupleHeader*, %TupleHeader*)

declare %Callable* @__quantum__rt__callable_create([4 x void (%TupleHeader*, %TupleHeader*, %TupleHeader*)*]*, %TupleHeader*)

declare void @__quantum__rt__callable_unreference(%Callable*)

declare void @__quantum__rt__tuple_unreference(%TupleHeader*)

declare void @__quantum__rt__callable_invoke(%Callable*, %TupleHeader*, %TupleHeader*)

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
