; ModuleID = 'Temporary ModuleID'
source_filename = "Temporary ModuleID"

%Range = type { i64, i64, i64 }
%Tuple = type opaque
%Callable = type opaque
%String = type opaque

@PauliI = internal constant i2 0
@PauliX = internal constant i2 1
@PauliY = internal constant i2 -1
@PauliZ = internal constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }
@PartialApplication__1__FunctionTable = internal constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Lifted__PartialApplication__1__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null]
@Microsoft__Quantum__Qir__Development__Add__FunctionTable = internal constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Microsoft__Quantum__Qir__Development__Add__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null]
@MemoryManagement__1__FunctionTable = internal constant [2 x void (%Tuple*, i32)*] [void (%Tuple*, i32)* @MemoryManagement__1__RefCount, void (%Tuple*, i32)* @MemoryManagement__1__AliasCount]
@PartialApplication__2__FunctionTable = internal constant [4 x void (%Tuple*, %Tuple*, %Tuple*)*] [void (%Tuple*, %Tuple*, %Tuple*)* @Lifted__PartialApplication__2__body__wrapper, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null, void (%Tuple*, %Tuple*, %Tuple*)* null]

define internal i64 @Microsoft__Quantum__Qir__Development__Add__body(i64 %var_left, i64 %var_right) !dbg !7 {
entry:
  call void @llvm.dbg.declare(metadata i64 %var_left, metadata !12, metadata !DIExpression()), !dbg !14
  call void @llvm.dbg.declare(metadata i64 %var_right, metadata !13, metadata !DIExpression()), !dbg !14
  %0 = add i64 %var_left, %var_right, !dbg !15
  ret i64 %0, !dbg !15
}

; Function Attrs: nounwind readnone speculatable willreturn
declare void @llvm.dbg.declare(metadata, metadata, metadata) #0

define internal i64 @Microsoft__Quantum__Qir__Development__GetTwentySix__body() !dbg !16 {
entry:
  ret i64 26, !dbg !19
}

define internal i64 @Microsoft__Quantum__Qir__Development__MainFunc__body() !dbg !20 {
entry:
  %var_x = call i64 @Microsoft__Quantum__Qir__Development__GetTwentySix__body(), !dbg !26
  call void @llvm.dbg.declare(metadata i64 %var_x, metadata !22, metadata !DIExpression()), !dbg !27
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64)), !dbg !28
  %1 = bitcast %Tuple* %0 to { %Callable*, i64 }*, !dbg !28
  %2 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %1, i32 0, i32 0, !dbg !28
  %3 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %1, i32 0, i32 1, !dbg !28
  %4 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Qir__Development__Add__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null), !dbg !28
  store %Callable* %4, %Callable** %2, align 8, !dbg !29
  store i64 4, i64* %3, align 4, !dbg !29
  %add_four = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__1__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %0), !dbg !29
  call void @__quantum__rt__capture_update_alias_count(%Callable* %add_four, i32 1), !dbg !29
  call void @__quantum__rt__callable_update_alias_count(%Callable* %add_four, i32 1), !dbg !29
  %5 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64)), !dbg !30
  %6 = bitcast %Tuple* %5 to { i64 }*, !dbg !30
  %7 = getelementptr inbounds { i64 }, { i64 }* %6, i32 0, i32 0, !dbg !30
  store i64 %var_x, i64* %7, align 4, !dbg !30
  %8 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64)), !dbg !30
  call void @__quantum__rt__callable_invoke(%Callable* %add_four, %Tuple* %5, %Tuple* %8), !dbg !30
  %9 = bitcast %Tuple* %8 to { i64 }*, !dbg !30
  %10 = getelementptr inbounds { i64 }, { i64 }* %9, i32 0, i32 0, !dbg !30
  %var_y = load i64, i64* %10, align 4, !dbg !30
  call void @llvm.dbg.declare(metadata i64 %var_y, metadata !24, metadata !DIExpression()), !dbg !30
  %11 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint ({ %Callable*, i64 }* getelementptr ({ %Callable*, i64 }, { %Callable*, i64 }* null, i32 1) to i64)), !dbg !31
  %12 = bitcast %Tuple* %11 to { %Callable*, i64 }*, !dbg !31
  %13 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %12, i32 0, i32 0, !dbg !31
  %14 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %12, i32 0, i32 1, !dbg !31
  %15 = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @Microsoft__Quantum__Qir__Development__Add__FunctionTable, [2 x void (%Tuple*, i32)*]* null, %Tuple* null), !dbg !31
  store %Callable* %15, %Callable** %13, align 8, !dbg !32
  store i64 3, i64* %14, align 4, !dbg !32
  %add_three = call %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]* @PartialApplication__2__FunctionTable, [2 x void (%Tuple*, i32)*]* @MemoryManagement__1__FunctionTable, %Tuple* %11), !dbg !32
  call void @__quantum__rt__capture_update_alias_count(%Callable* %add_three, i32 1), !dbg !32
  call void @__quantum__rt__callable_update_alias_count(%Callable* %add_three, i32 1), !dbg !32
  %16 = call i64 @Microsoft__Quantum__Qir__Development__TakesCallable__body(i64 %var_y, %Callable* %add_three), !dbg !33
  call void @__quantum__rt__capture_update_alias_count(%Callable* %add_four, i32 -1), !dbg !33
  call void @__quantum__rt__callable_update_alias_count(%Callable* %add_four, i32 -1), !dbg !33
  call void @__quantum__rt__capture_update_alias_count(%Callable* %add_three, i32 -1), !dbg !33
  call void @__quantum__rt__callable_update_alias_count(%Callable* %add_three, i32 -1), !dbg !33
  call void @__quantum__rt__capture_update_reference_count(%Callable* %add_four, i32 -1), !dbg !33
  call void @__quantum__rt__callable_update_reference_count(%Callable* %add_four, i32 -1), !dbg !33
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %5, i32 -1), !dbg !33
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %8, i32 -1), !dbg !33
  call void @__quantum__rt__capture_update_reference_count(%Callable* %add_three, i32 -1), !dbg !33
  call void @__quantum__rt__callable_update_reference_count(%Callable* %add_three, i32 -1), !dbg !33
  ret i64 %16, !dbg !33
}

define internal void @Lifted__PartialApplication__1__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %0, i32 0, i32 1
  %2 = load i64, i64* %1, align 4
  %3 = bitcast %Tuple* %arg-tuple to { i64 }*
  %4 = getelementptr inbounds { i64 }, { i64 }* %3, i32 0, i32 0
  %5 = load i64, i64* %4, align 4
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { i64, i64 }*
  %8 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %7, i32 0, i32 0
  %9 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %7, i32 0, i32 1
  store i64 %2, i64* %8, align 4
  store i64 %5, i64* %9, align 4
  %10 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %0, i32 0, i32 0
  %11 = load %Callable*, %Callable** %10, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %11, %Tuple* %6, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  ret void
}

declare %Callable* @__quantum__rt__callable_create([4 x void (%Tuple*, %Tuple*, %Tuple*)*]*, [2 x void (%Tuple*, i32)*]*, %Tuple*)

declare %Tuple* @__quantum__rt__tuple_create(i64)

define internal void @Microsoft__Quantum__Qir__Development__Add__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %arg-tuple to { i64, i64 }*
  %1 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %0, i32 0, i32 0
  %2 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %0, i32 0, i32 1
  %3 = load i64, i64* %1, align 4
  %4 = load i64, i64* %2, align 4
  %5 = call i64 @Microsoft__Quantum__Qir__Development__Add__body(i64 %3, i64 %4)
  %6 = bitcast %Tuple* %result-tuple to { i64 }*
  %7 = getelementptr inbounds { i64 }, { i64 }* %6, i32 0, i32 0
  store i64 %5, i64* %7, align 4
  ret void
}

define internal void @MemoryManagement__1__RefCount(%Tuple* %capture-tuple, i32 %count-change) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %0, i32 0, i32 0
  %2 = load %Callable*, %Callable** %1, align 8
  call void @__quantum__rt__capture_update_reference_count(%Callable* %2, i32 %count-change)
  call void @__quantum__rt__callable_update_reference_count(%Callable* %2, i32 %count-change)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %capture-tuple, i32 %count-change)
  ret void
}

define internal void @MemoryManagement__1__AliasCount(%Tuple* %capture-tuple, i32 %count-change) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %0, i32 0, i32 0
  %2 = load %Callable*, %Callable** %1, align 8
  call void @__quantum__rt__capture_update_alias_count(%Callable* %2, i32 %count-change)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %2, i32 %count-change)
  call void @__quantum__rt__tuple_update_alias_count(%Tuple* %capture-tuple, i32 %count-change)
  ret void
}

declare void @__quantum__rt__capture_update_alias_count(%Callable*, i32)

declare void @__quantum__rt__callable_update_alias_count(%Callable*, i32)

declare void @__quantum__rt__callable_invoke(%Callable*, %Tuple*, %Tuple*)

define internal void @Lifted__PartialApplication__2__body__wrapper(%Tuple* %capture-tuple, %Tuple* %arg-tuple, %Tuple* %result-tuple) {
entry:
  %0 = bitcast %Tuple* %capture-tuple to { %Callable*, i64 }*
  %1 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %0, i32 0, i32 1
  %2 = load i64, i64* %1, align 4
  %3 = bitcast %Tuple* %arg-tuple to { i64 }*
  %4 = getelementptr inbounds { i64 }, { i64 }* %3, i32 0, i32 0
  %5 = load i64, i64* %4, align 4
  %6 = call %Tuple* @__quantum__rt__tuple_create(i64 mul nuw (i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64), i64 2))
  %7 = bitcast %Tuple* %6 to { i64, i64 }*
  %8 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %7, i32 0, i32 0
  %9 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %7, i32 0, i32 1
  store i64 %2, i64* %8, align 4
  store i64 %5, i64* %9, align 4
  %10 = getelementptr inbounds { %Callable*, i64 }, { %Callable*, i64 }* %0, i32 0, i32 0
  %11 = load %Callable*, %Callable** %10, align 8
  call void @__quantum__rt__callable_invoke(%Callable* %11, %Tuple* %6, %Tuple* %result-tuple)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %6, i32 -1)
  ret void
}

define internal i64 @Microsoft__Quantum__Qir__Development__TakesCallable__body(i64 %given_int, %Callable* %callable) {
entry:
  call void @__quantum__rt__capture_update_alias_count(%Callable* %callable, i32 1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %callable, i32 1)
  %0 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  %1 = bitcast %Tuple* %0 to { i64 }*
  %2 = getelementptr inbounds { i64 }, { i64 }* %1, i32 0, i32 0
  store i64 %given_int, i64* %2, align 4
  %3 = call %Tuple* @__quantum__rt__tuple_create(i64 ptrtoint (i64* getelementptr (i64, i64* null, i32 1) to i64))
  call void @__quantum__rt__callable_invoke(%Callable* %callable, %Tuple* %0, %Tuple* %3)
  %4 = bitcast %Tuple* %3 to { i64 }*
  %5 = getelementptr inbounds { i64 }, { i64 }* %4, i32 0, i32 0
  %6 = load i64, i64* %5, align 4
  call void @__quantum__rt__capture_update_alias_count(%Callable* %callable, i32 -1)
  call void @__quantum__rt__callable_update_alias_count(%Callable* %callable, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %0, i32 -1)
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %3, i32 -1)
  ret i64 %6
}

declare void @__quantum__rt__capture_update_reference_count(%Callable*, i32)

declare void @__quantum__rt__callable_update_reference_count(%Callable*, i32)

declare void @__quantum__rt__tuple_update_reference_count(%Tuple*, i32)

declare void @__quantum__rt__tuple_update_alias_count(%Tuple*, i32)

define internal i64 @Microsoft__Quantum__Qir__Development__RunExample__body() !dbg !34 {
entry:
  br i1 true, label %then0__1, label %continue__1, !dbg !35

then0__1:                                         ; preds = %entry
  %0 = call i64 @Microsoft__Quantum__Qir__Development__MainFunc__body(), !dbg !36
  ret i64 %0, !dbg !36

continue__1:                                      ; preds = %entry
  ret i64 -1, !dbg !37
}

define i64 @Microsoft__Quantum__Qir__Development__RunExample__Interop() #1 {
entry:
  %0 = call i64 @Microsoft__Quantum__Qir__Development__RunExample__body()
  ret i64 %0
}

define void @Microsoft__Quantum__Qir__Development__RunExample() #2 {
entry:
  %0 = call i64 @Microsoft__Quantum__Qir__Development__RunExample__body()
  %1 = call %String* @__quantum__rt__int_to_string(i64 %0)
  call void @__quantum__rt__message(%String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*)

declare %String* @__quantum__rt__int_to_string(i64)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

attributes #0 = { nounwind readnone speculatable willreturn }
attributes #1 = { "InteropFriendly" }
attributes #2 = { "EntryPoint" }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2, !3}
!llvm.dbg.cu = !{!4}

!0 = !{!"Microsoft.Quantum.QsCompiler with Microsoft.Quantum.QirGeneration V 1.0.0.0"}
!1 = !{i32 2, !"Dwarf Version", i32 4}
!2 = !{i32 2, !"Debug Info Version", i32 3}
!3 = !{i32 2, !"CodeView", i32 1}
!4 = distinct !DICompileUnit(language: DW_LANG_C99, file: !5, producer: "Microsoft.Quantum.QsCompiler with Microsoft.Quantum.QirGeneration V 1.0.0.0", isOptimized: false, runtimeVersion: 0, emissionKind: FullDebug, enums: !6, splitDebugInlining: false)
!5 = !DIFile(filename: "Program.c", directory: "C:\\Users\\t-ryanmoreno\\source\\repos\\qsharp-compiler-debug\\examples\\QIR\\Development")
!6 = !{}
!7 = distinct !DISubprogram(name: "Add", linkageName: "Microsoft__Quantum__Qir__Development__Add__body", scope: null, file: !5, line: 25, type: !8, scopeLine: 25, spFlags: DISPFlagLocalToUnit | DISPFlagDefinition, unit: !4, retainedNodes: !11)
!8 = !DISubroutineType(types: !9)
!9 = !{!10, !10, !10}
!10 = !DIBasicType(name: "Int", size: 64, encoding: DW_ATE_signed)
!11 = !{!12, !13}
!12 = !DILocalVariable(name: "var_left", arg: 1, scope: !7, file: !5, line: 25, type: !10)
!13 = !DILocalVariable(name: "var_right", arg: 2, scope: !7, file: !5, line: 25, type: !10)
!14 = !DILocation(line: 25, column: 1, scope: !7)
!15 = !DILocation(line: 26, column: 9, scope: !7)
!16 = distinct !DISubprogram(name: "GetTwentySix", linkageName: "Microsoft__Quantum__Qir__Development__GetTwentySix__body", scope: null, file: !5, line: 33, type: !17, scopeLine: 33, spFlags: DISPFlagLocalToUnit | DISPFlagDefinition, unit: !4, retainedNodes: !6)
!17 = !DISubroutineType(types: !18)
!18 = !{!10, null}
!19 = !DILocation(line: 34, column: 9, scope: !16)
!20 = distinct !DISubprogram(name: "MainFunc", linkageName: "Microsoft__Quantum__Qir__Development__MainFunc__body", scope: null, file: !5, line: 16, type: !17, scopeLine: 16, spFlags: DISPFlagLocalToUnit | DISPFlagDefinition, unit: !4, retainedNodes: !21)
!21 = !{!22, !23, !24, !25}
!22 = !DILocalVariable(name: "var_x", scope: !20, file: !5, line: 17, type: !10)
!23 = !DILocalVariable(name: "add_four", scope: !20, file: !5, line: 18)
!24 = !DILocalVariable(name: "var_y", scope: !20, file: !5, line: 19, type: !10)
!25 = !DILocalVariable(name: "add_three", scope: !20, file: !5, line: 21)
!26 = !DILocation(line: 17, column: 21, scope: !20)
!27 = !DILocation(line: 17, column: 9, scope: !20)
!28 = !DILocation(line: 18, column: 24, scope: !20)
!29 = !DILocation(line: 18, column: 9, scope: !20)
!30 = !DILocation(line: 19, column: 9, scope: !20)
!31 = !DILocation(line: 21, column: 25, scope: !20)
!32 = !DILocation(line: 21, column: 9, scope: !20)
!33 = !DILocation(line: 22, column: 9, scope: !20)
!34 = distinct !DISubprogram(name: "RunExample", linkageName: "Microsoft__Quantum__Qir__Development__RunExample__body", scope: null, file: !5, line: 8, type: !17, scopeLine: 8, spFlags: DISPFlagLocalToUnit | DISPFlagDefinition, unit: !4, retainedNodes: !6)
!35 = !DILocation(line: 9, column: 9, scope: !34)
!36 = !DILocation(line: 11, column: 20, scope: !34)
!37 = !DILocation(line: 13, column: 9, scope: !34)
