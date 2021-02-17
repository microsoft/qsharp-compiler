define void @Microsoft__Quantum__Testing__QIR__ReturnTuple__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { %String* }*
  %1 = getelementptr inbounds { %String* }, { %String* }* %0, i32 0, i32 0
  %2 = load %String*, %String** %1
  %3 = call { %String*, { i64, double }* }* @Microsoft__Quantum__Testing__QIR__ReturnTuple__body(%String* %2)
  %4 = bitcast %Tuple* %result-tuple to { %String*, { i64, double }* }*
  %5 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %4, i32 0, i32 0
  %6 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %3, i32 0, i32 0
  %7 = load %String*, %String** %6
  store %String* %7, %String** %5
  %8 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %4, i32 0, i32 1
  %9 = getelementptr inbounds { %String*, { i64, double }* }, { %String*, { i64, double }* }* %3, i32 0, i32 1
  %10 = load { i64, double }*, { i64, double }** %9
  store { i64, double }* %10, { i64, double }** %8
  ret void
}
