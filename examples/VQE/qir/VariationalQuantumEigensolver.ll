
%Result = type opaque
%Range = type { i64, i64, i64 }
%Tuple = type opaque
%String = type opaque
%Array = type opaque
%Callable = type opaque

@ResultZero = external global %Result*
@ResultOne = external global %Result*
@PauliI = constant i2 0
@PauliX = constant i2 1
@PauliY = constant i2 -1
@PauliZ = constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }
@Microsoft__Quantum__Samples__Chemistry__SimpleVQE__Dummy = constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__Dummy__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__Dummy__adj__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__Dummy__ctl__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__Dummy__ctladj__wrapper]
@0 = internal constant [9 x i8] c"Success!\00"
@Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorFunction = constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorFunction__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null]
@Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorGeneratorSystemImpl = constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorGeneratorSystemImpl__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null]

@Microsoft__Quantum__Samples__Chemistry__SimpleVQE__GetEnergyHydrogenVQE = alias %String* (), %String* ()* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__GetEnergyHydrogenVQE__body

define { %Callable* }* @Microsoft__Quantum__Simulation__EvolutionUnitary__body(%Callable* %__Item1__) {
entry:
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %__Item1__, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %__Item1__, i64 1)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { %Callable* }*
  %2 = getelementptr inbounds { %Callable* }, { %Callable* }* %1, i32 0, i32 0
  store %Callable* %__Item1__, %Callable** %2
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %__Item1__, i64 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %__Item1__, i64 1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %__Item1__, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %__Item1__, i64 -1)
  ret { %Callable* }* %1
}

define %String* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__GetEnergyHydrogenVQE__body() #0 {
entry:
  %evolutionSet = call { %Callable* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__JordanWignerClusterOperatorEvolutionSet__body()
  %0 = getelementptr inbounds { %Callable* }, { %Callable* }* %evolutionSet, i32 0, i32 0
  %1 = load %Callable*, %Callable** %0
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %1, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %1, i64 1)
  %2 = bitcast { %Callable* }* %evolutionSet to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 1)
  %generatorSystem = call { i64, %Callable* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__JordanWignerClusterOperatorGeneratorSystem__body()
  %3 = getelementptr inbounds { i64, %Callable* }, { i64, %Callable* }* %generatorSystem, i32 0, i32 1
  %generatorSystemFunction = load %Callable*, %Callable** %3
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %generatorSystemFunction, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %generatorSystemFunction, i64 1)
  %4 = bitcast { i64, %Callable* }* %generatorSystem to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %4, i64 1)
  %5 = getelementptr inbounds { i64, %Callable* }, { i64, %Callable* }* %generatorSystem, i32 0, i32 0
  %nTerms = load i64, i64* %5
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %generatorSystemFunction, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %generatorSystemFunction, i64 1)
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %7 = bitcast %Tuple* %6 to { i64 }*
  %8 = getelementptr inbounds { i64 }, { i64 }* %7, i32 0, i32 0
  store i64 0, i64* %8
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  call void @__quantum__rt__callable_invoke(%Callable* %generatorSystemFunction, %Tuple* %6, %Tuple* %9)
  %10 = bitcast %Tuple* %9 to { { { %Array*, %Array* }*, %Array* }* }*
  %11 = getelementptr inbounds { { { %Array*, %Array* }*, %Array* }* }, { { { %Array*, %Array* }*, %Array* }* }* %10, i32 0, i32 0
  %generatorIndex = load { { %Array*, %Array* }*, %Array* }*, { { %Array*, %Array* }*, %Array* }** %11
  %12 = getelementptr inbounds { { %Array*, %Array* }*, %Array* }, { { %Array*, %Array* }*, %Array* }* %generatorIndex, i32 0, i32 0
  %13 = load { %Array*, %Array* }*, { %Array*, %Array* }** %12
  %14 = getelementptr inbounds { %Array*, %Array* }, { %Array*, %Array* }* %13, i32 0, i32 0
  %15 = load %Array*, %Array** %14
  call void @__quantum__rt__array_update_alias_count(%Array* %15, i64 1)
  %16 = getelementptr inbounds { %Array*, %Array* }, { %Array*, %Array* }* %13, i32 0, i32 1
  %17 = load %Array*, %Array** %16
  call void @__quantum__rt__array_update_alias_count(%Array* %17, i64 1)
  %18 = bitcast { %Array*, %Array* }* %13 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i64 1)
  %19 = getelementptr inbounds { { %Array*, %Array* }*, %Array* }, { { %Array*, %Array* }*, %Array* }* %generatorIndex, i32 0, i32 1
  %20 = load %Array*, %Array** %19
  call void @__quantum__rt__array_update_alias_count(%Array* %20, i64 1)
  %21 = bitcast { { %Array*, %Array* }*, %Array* }* %generatorIndex to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %21, i64 1)
  %22 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64))
  call void @__quantum__rt__callable_invoke(%Callable* %1, %Tuple* %21, %Tuple* %22)
  %23 = bitcast %Tuple* %22 to { { %Callable* }* }*
  %24 = getelementptr inbounds { { %Callable* }* }, { { %Callable* }* }* %23, i32 0, i32 0
  %eSet = load { %Callable* }*, { %Callable* }** %24
  %25 = getelementptr inbounds { %Callable* }, { %Callable* }* %eSet, i32 0, i32 0
  %26 = load %Callable*, %Callable** %25
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %26, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %26, i64 1)
  %27 = bitcast { %Callable* }* %eSet to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %27, i64 1)
  %28 = call %String* @__quantum__rt__string_create(i32 8, i8* getelementptr inbounds ([9 x i8], [9 x i8]* @0, i32 0, i32 0))
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %1, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %1, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %generatorSystemFunction, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %generatorSystemFunction, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %4, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %generatorSystemFunction, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %generatorSystemFunction, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %15, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %17, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %18, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %20, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %21, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %26, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %26, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %27, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %1, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %1, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %2, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %generatorSystemFunction, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %generatorSystemFunction, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %4, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %15, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %17, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %18, i64 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %20, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %21, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %26, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %26, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %27, i64 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %22, i64 -1)
  ret %String* %28
}

define { %Callable* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__JordanWignerClusterOperatorEvolutionSet__body() {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorFunction, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %1 = call { %Callable* }* @Microsoft__Quantum__Simulation__EvolutionSet__body(%Callable* %0)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %0, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i64 -1)
  ret { %Callable* }* %1
}

define { i64, %Callable* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__JordanWignerClusterOperatorGeneratorSystem__body() {
entry:
  %0 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorGeneratorSystemImpl, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %1 = call { i64, %Callable* }* @Microsoft__Quantum__Simulation__GeneratorSystem__body(i64 1, %Callable* %0)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %0, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %0, i64 -1)
  ret { i64, %Callable* }* %1
}

; FIXME: IT IS NOT CLEAR THAT THE ARG TUPLE HERE SHOULD HAVE THE ADDITIONAL WRAPPING? 
; OR RATHER, WHY IS IT WRAPPED ONCE MORE HERE THAN IT IS FOR JordanWignerClusterOperatorFunction__body
define void @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorFunction__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { { { %Array*, %Array* }*, %Array* }* }*
  %1 = getelementptr inbounds { { { %Array*, %Array* }*, %Array* }* }, { { { %Array*, %Array* }*, %Array* }* }* %0, i32 0, i32 0
  %2 = load { { %Array*, %Array* }*, %Array* }*, { { %Array*, %Array* }*, %Array* }** %1
  %3 = call { %Callable* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorFunction__body({ { %Array*, %Array* }*, %Array* }* %2)
  %4 = bitcast %Tuple* %result-tuple to { { %Callable* }* }*
  %5 = getelementptr inbounds { { %Callable* }* }, { { %Callable* }* }* %4, i32 0, i32 0
  store { %Callable* }* %3, { %Callable* }** %5
  ret void
}

define { %Callable* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorFunction__body({ { %Array*, %Array* }*, %Array* }* %generatorIndex) {
entry:
  %0 = getelementptr inbounds { { %Array*, %Array* }*, %Array* }, { { %Array*, %Array* }*, %Array* }* %generatorIndex, i32 0, i32 0
  %1 = load { %Array*, %Array* }*, { %Array*, %Array* }** %0
  %2 = getelementptr inbounds { %Array*, %Array* }, { %Array*, %Array* }* %1, i32 0, i32 0
  %3 = load %Array*, %Array** %2
  call void @__quantum__rt__array_update_alias_count(%Array* %3, i64 1)
  %4 = getelementptr inbounds { %Array*, %Array* }, { %Array*, %Array* }* %1, i32 0, i32 1
  %5 = load %Array*, %Array** %4
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i64 1)
  %6 = bitcast { %Array*, %Array* }* %1 to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 1)
  %7 = getelementptr inbounds { { %Array*, %Array* }*, %Array* }, { { %Array*, %Array* }*, %Array* }* %generatorIndex, i32 0, i32 1
  %8 = load %Array*, %Array** %7
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i64 1)
  %9 = bitcast { { %Array*, %Array* }*, %Array* }* %generatorIndex to %Tuple*
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i64 1)
  %10 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE__Dummy, [2 x void (%Tuple*, i64)*]* null, %Tuple* null)
  %11 = call { %Callable* }* @Microsoft__Quantum__Simulation__EvolutionUnitary__body(%Callable* %10)
  call void @__quantum__rt__array_update_alias_count(%Array* %3, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %5, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %6, i64 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %8, i64 -1)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %9, i64 -1)
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %10, i64 -1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %10, i64 -1)
  ret { %Callable* }* %11
}

define { { %Array*, %Array* }*, %Array* }* @Microsoft__Quantum__Simulation__GeneratorIndex__body({ %Array*, %Array* }* %0, %Array* %__Item3__) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %__Item3__, i64 1)
  %1 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i1** getelementptr (i1*, i1** null, i32 1) to i64), i64 2))
  %2 = bitcast %Tuple* %1 to { { %Array*, %Array* }*, %Array* }*
  %3 = getelementptr inbounds { { %Array*, %Array* }*, %Array* }, { { %Array*, %Array* }*, %Array* }* %2, i32 0, i32 0
  %4 = getelementptr inbounds { { %Array*, %Array* }*, %Array* }, { { %Array*, %Array* }*, %Array* }* %2, i32 0, i32 1
  store { %Array*, %Array* }* %0, { %Array*, %Array* }** %3
  store %Array* %__Item3__, %Array** %4
  %5 = getelementptr inbounds { %Array*, %Array* }, { %Array*, %Array* }* %0, i32 0, i32 0
  %6 = load %Array*, %Array** %5
  %7 = getelementptr inbounds { %Array*, %Array* }, { %Array*, %Array* }* %0, i32 0, i32 1
  %8 = load %Array*, %Array** %7
  call void @__quantum__rt__array_update_reference_count(%Array* %6, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %8, i64 1)
  %9 = bitcast { %Array*, %Array* }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %9, i64 1)
  call void @__quantum__rt__array_update_reference_count(%Array* %__Item3__, i64 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %__Item3__, i64 -1)
  ret { { %Array*, %Array* }*, %Array* }* %2
}

define { i64, %Callable* }* @Microsoft__Quantum__Simulation__GeneratorSystem__body(i64 %__Item1__, %Callable* %__Item2__) {
entry:
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %__Item2__, i64 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %__Item2__, i64 1)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ i64, %Callable* }* getelementptr ({ i64, %Callable* }, { i64, %Callable* }* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { i64, %Callable* }*
  %2 = getelementptr inbounds { i64, %Callable* }, { i64, %Callable* }* %1, i32 0, i32 0
  %3 = getelementptr inbounds { i64, %Callable* }, { i64, %Callable* }* %1, i32 0, i32 1
  store i64 %__Item1__, i64* %2
  store %Callable* %__Item2__, %Callable** %3
  call void @__quantum__rt__callable_memory_management(i32 0, %Callable* %__Item2__, i64 1)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %__Item2__, i64 1)
  call void @__quantum__rt__callable_memory_management(i32 1, %Callable* %__Item2__, i64 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %__Item2__, i64 -1)
  ret { i64, %Callable* }* %1
}

define void @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorGeneratorSystemImpl__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { i64 }*
  %1 = getelementptr inbounds { i64 }, { i64 }* %0, i32 0, i32 0
  %2 = load i64, i64* %1
  %3 = call { { %Array*, %Array* }*, %Array* }* @Microsoft__Quantum__Samples__Chemistry__SimpleVQE___JordanWignerClusterOperatorGeneratorSystemImpl__body(i64 %2)
  %4 = bitcast %Tuple* %result-tuple to { { { %Array*, %Array* }*, %Array* }* }*
  %5 = getelementptr inbounds { { { %Array*, %Array* }*, %Array* }* }, { { { %Array*, %Array* }*, %Array* }* }* %4, i32 0, i32 0
  store { { %Array*, %Array* }*, %Array* }* %3, { { %Array*, %Array* }*, %Array* }** %5
  ret void
}

attributes #0 = { "EntryPoint" }
