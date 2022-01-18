define internal i1 @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  tail call void @llvm.dbg.value(metadata i1 true, metadata !13, metadata !DIExpression()), !dbg !15
  %varY = alloca i1, align 1, !dbg !16
  store i1 false, i1* %varY, align 1, !dbg !16
  tail call void @llvm.dbg.value(metadata i1* %varY, metadata !14, metadata !DIExpression()), !dbg !16
  store i1 true, i1* %varY, align 1, !dbg !17
  ret i1 true, !dbg !18
}
