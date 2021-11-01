; ModuleID = 'Temporary ModuleID'
source_filename = "Temporary ModuleID"

%Range = type { i64, i64, i64 }
%String = type opaque

@PauliI = internal constant i2 0
@PauliX = internal constant i2 1
@PauliY = internal constant i2 -1
@PauliZ = internal constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }

define internal i64 @Microsoft__Quantum__Qir__Development__InternalFunc__body() !dbg !7 {
entry:
  %var_x = alloca i64, align 8
  store i64 26, i64* %var_x, align 4
  ret i64 26
}

define internal i64 @Microsoft__Quantum__Qir__Development__RunExample__body() !dbg !11 {
entry:
  %0 = call i64 @Microsoft__Quantum__Qir__Development__InternalFunc__body(), !dbg !12
  %var_y = alloca i64, align 8, !dbg !12
  store i64 %0, i64* %var_y, align 4, !dbg !12
  ret i64 %0, !dbg !12
}

define i64 @Microsoft__Quantum__Qir__Development__RunExample__Interop() #0 {
entry:
  %0 = call i64 @Microsoft__Quantum__Qir__Development__RunExample__body()
  ret i64 %0
}

define void @Microsoft__Quantum__Qir__Development__RunExample() #1 {
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

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }

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
!7 = distinct !DISubprogram(name: "Microsoft__Quantum__Qir__Development__InternalFunc__body", linkageName: "Microsoft__Quantum__Qir__Development__InternalFunc__body", scope: null, file: !5, line: 19, type: !8, scopeLine: 19, spFlags: DISPFlagLocalToUnit | DISPFlagDefinition, unit: !4, retainedNodes: !6)
!8 = !DISubroutineType(types: !9)
!9 = !{!10, null}
!10 = !DIBasicType(name: "Int", size: 64, encoding: DW_ATE_signed)
!11 = distinct !DISubprogram(name: "Microsoft__Quantum__Qir__Development__RunExample__body", linkageName: "Microsoft__Quantum__Qir__Development__RunExample__body", scope: null, file: !5, line: 14, type: !8, scopeLine: 14, spFlags: DISPFlagLocalToUnit | DISPFlagDefinition, unit: !4, retainedNodes: !6)
!12 = !DILocation(line: 16, column: 37, scope: !11)
