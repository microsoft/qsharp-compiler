define internal void @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !15 {
entry:
  tail call void @llvm.dbg.value(metadata i64 42, metadata !19, metadata !DIExpression()), !dbg !20
  call void @Microsoft__Quantum__Testing__QirDebugInfo__IntToUnit__body(i64 42), !dbg !21
  call void @Microsoft__Quantum__Testing__QirDebugInfo__ToUnit__body(), !dbg !22
  ret void, !dbg !22
}
