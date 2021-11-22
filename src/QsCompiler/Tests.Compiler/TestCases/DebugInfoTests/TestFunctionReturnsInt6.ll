define internal i64 @Microsoft__Quantum__Testing__QirDebugInfo__IntToInt__body(i64 %var_x) !dbg !7 {
entry:
  call void @llvm.dbg.declare(metadata i64 %var_x, metadata !12, metadata !DIExpression()), !dbg !13
  %0 = add i64 %var_x, 1, !dbg !14
  ret i64 %0, !dbg !14
}