define internal i64 @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  tail call void @llvm.dbg.value(metadata i64 42, metadata !13, metadata !DIExpression()), !dbg !15
  %varY = alloca i64, align 8, !dbg !16
  store i64 43, i64* %varY, align 4, !dbg !16
  tail call void @llvm.dbg.value(metadata i64* %varY, metadata !14, metadata !DIExpression()), !dbg !16
  store i64 42, i64* %varY, align 4, !dbg !17
  ret i64 42, !dbg !18
}
